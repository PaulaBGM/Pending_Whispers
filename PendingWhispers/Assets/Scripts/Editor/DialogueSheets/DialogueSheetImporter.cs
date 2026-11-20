using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Inventory.Model;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace PendingWhispers.EditorTools
{
    /// <summary>
    /// Tools > Dialogue > Importar desde CSV
    /// Lee los CSV descargados de la Google Sheet (Nodos, Opciones, Pruebas) y
    /// genera o actualiza los DialogueData.
    ///
    /// Funciona "todo o nada": primero lo valida todo y, si hay un solo error,
    /// no escribe ningún asset. Los errores salen en la Console con pestaña y fila.
    ///
    /// Columnas: se buscan por el nombre de la cabecera, así que el orden da igual
    /// y se pueden añadir columnas propias (p. ej. "notas"): se ignoran.
    /// </summary>
    public static class DialogueSheetImporter
    {
        private const string Title = "Importar diálogos";
        private const string TestFolder = "Assets/Scripts/Systems/Dialogo/SOs/_ImportTest";
        private const string NewDialoguesFolder = "Assets/Scripts/Systems/Dialogo/SOs/Imported";
        private const string DefaultWrongFeedback = "Eso no parece relevante ahora mismo.";
        private const string StartNodeId = "start";

        [MenuItem("Tools/Dialogue/Importar desde CSV")]
        public static void ImportMenu()
        {
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            string defaultFolder = Path.Combine(projectRoot, "DialogueSheets");

            string csvFolder = EditorUtility.OpenFolderPanel("Carpeta con los CSV (Nodos, Opciones, Pruebas)", defaultFolder, "");
            if (string.IsNullOrEmpty(csvFolder))
                return;

            int mode = EditorUtility.DisplayDialogComplex(Title,
                "¿Dónde se escriben los diálogos?\n\n" +
                "· Carpeta de prueba: crea copias en SOs/_ImportTest. Los originales no se tocan.\n\n" +
                "· Actualizar originales: sobrescribe los nodos de los DialogueData con el mismo nombre. " +
                "Los diálogos nuevos se crean en SOs/Imported.",
                "Carpeta de prueba", "Cancelar", "Actualizar originales");

            if (mode == 1)
                return;

            bool updateOriginals = mode == 2;
            if (updateOriginals && !EditorUtility.DisplayDialog(Title,
                    "Se van a sobrescribir los nodos de los DialogueData originales.\n\n" +
                    "Se puede deshacer con Ctrl+Z (o con Git). ¿Continuar?",
                    "Sí, actualizar", "Cancelar"))
                return;

            Import(csvFolder, updateOriginals);
        }

        // ==================================================================
        // Flujo principal
        // ==================================================================

        private static void Import(string csvFolder, bool updateOriginals)
        {
            var report = new Report();

            Table nodesTable = Table.Load(csvFolder, "Nodos", DialogueSheetCsv.NodeHeaders, report);
            Table choicesTable = Table.Load(csvFolder, "Opciones", DialogueSheetCsv.ChoiceHeaders, report);
            Table evidenceTable = Table.Load(csvFolder, "Pruebas", DialogueSheetCsv.EvidenceHeaders, report);

            if (report.HasErrors)
            {
                Finish(report, 0, 0, updateOriginals);
                return;
            }

            var flags = new AssetIndex<FlagSO>("FlagSO");
            var events = new AssetIndex<GameEventSO>("GameEventSO");
            var items = new AssetIndex<ItemSO>("ItemSO (pista/testimonio)");
            var testimonies = new AssetIndex<TestimonyItemSO>("TestimonyItemSO");

            // 1. Construir nodos en memoria (sin tocar assets).
            var dialogues = new Dictionary<string, List<DialogueNode>>();
            var dialogueOrder = new List<string>();

            foreach (Row row in nodesTable.Rows)
            {
                string dialogueName = row.Get("dialogue");
                string id = row.Get("id");

                if (string.IsNullOrEmpty(dialogueName))
                {
                    report.Error(row.Where, "falta el nombre del diálogo (columna dialogue).");
                    continue;
                }

                if (dialogueName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                {
                    report.Error(row.Where, $"'{dialogueName}' tiene caracteres que no valen para un nombre de archivo.");
                    continue;
                }

                if (!dialogues.TryGetValue(dialogueName, out List<DialogueNode> nodes))
                {
                    nodes = new List<DialogueNode>();
                    dialogues[dialogueName] = nodes;
                    dialogueOrder.Add(dialogueName);
                }

                if (string.IsNullOrEmpty(id))
                {
                    report.Error(row.Where, "nodo sin id.");
                    continue;
                }

                if (nodes.Any(n => n.id == id))
                {
                    report.Error(row.Where, $"id de nodo repetido '{id}' en {dialogueName}.");
                    continue;
                }

                string feedback = row.GetRaw("wrongEvidenceFeedback");

                nodes.Add(new DialogueNode
                {
                    id = id,
                    speakerID = row.Get("speaker"),
                    text = row.GetRaw("text"),
                    nextNodeID = row.Get("next"),
                    expression = ParseExpression(row, report),
                    testimony = testimonies.Resolve(row.Get("testimony"), row.Where, report),
                    requiredFlags = flags.ResolveList(row.Get("requiredFlags"), row.Where, report),
                    onEnterFlags = flags.ResolveList(row.Get("onEnterFlags"), row.Where, report),
                    onEnterEvents = events.ResolveList(row.Get("onEnterEvents"), row.Where, report),
                    allowsEvidencePresentation = ParseBool(row, "allowsEvidence", report),
                    wrongEvidenceNodeID = row.Get("wrongEvidenceNode"),
                    wrongEvidenceFeedback = string.IsNullOrWhiteSpace(feedback) ? DefaultWrongFeedback : feedback,
                    choices = new List<DialogueChoice>(),
                    evidenceOptions = new List<DialogueEvidenceOption>()
                });
            }

            foreach (Row row in choicesTable.Rows)
            {
                DialogueNode node = FindOwnerNode(row, dialogues, report);
                if (node == null)
                    continue;

                node.choices.Add(new DialogueChoice
                {
                    text = row.GetRaw("text"),
                    nextNodeID = row.Get("next"),
                    requiredFlags = flags.ResolveList(row.Get("requiredFlags"), row.Where, report),
                    addFlags = flags.ResolveList(row.Get("addFlags"), row.Where, report),
                    onSelectedEvent = events.Resolve(row.Get("onSelectedEvent"), row.Where, report),
                    endsDialogue = ParseBool(row, "endsDialogue", report),
                    requiredReputation = ParseInt(row, "requiredReputation", report),
                    reputationChange = ParseInt(row, "reputationChange", report)
                });
            }

            foreach (Row row in evidenceTable.Rows)
            {
                DialogueNode node = FindOwnerNode(row, dialogues, report);
                if (node == null)
                    continue;

                ItemSO item = items.Resolve(row.Get("item"), row.Where, report);
                if (item == null && string.IsNullOrEmpty(row.Get("item")))
                    report.Error(row.Where, "opción de prueba sin item.");

                if (item != null && node.evidenceOptions.Any(o => o.item == item))
                    report.Warning(row.Where, $"'{item.name}' ya estaba como prueba en el nodo '{node.id}'; solo contará la primera fila.");

                node.evidenceOptions.Add(new DialogueEvidenceOption
                {
                    item = item,
                    nextNodeID = row.Get("next"),
                    addFlags = flags.ResolveList(row.Get("addFlags"), row.Where, report),
                    onSelectedEvent = events.Resolve(row.Get("onSelectedEvent"), row.Where, report)
                });
            }

            // 2. Validar la estructura de cada diálogo.
            foreach (string dialogueName in dialogueOrder)
                ValidateDialogue(dialogueName, dialogues[dialogueName], report);

            // 3. Decidir qué asset se escribe para cada diálogo.
            var originals = FindOriginalDialogues(report);
            var characterPool = BuildCharacterPool(originals);
            var plans = new List<WritePlan>();

            foreach (string dialogueName in dialogueOrder)
            {
                originals.TryGetValue(dialogueName, out DialogueData original);
                string path;
                DialogueData target;

                if (updateOriginals && original != null)
                {
                    target = original;
                    path = AssetDatabase.GetAssetPath(original);
                }
                else
                {
                    string folder = updateOriginals ? NewDialoguesFolder : TestFolder;
                    path = $"{folder}/{dialogueName}.asset";
                    target = AssetDatabase.LoadAssetAtPath<DialogueData>(path);
                }

                plans.Add(new WritePlan
                {
                    Name = dialogueName,
                    Path = path,
                    Target = target,
                    Original = original,
                    Nodes = dialogues[dialogueName]
                });

                CheckSpeakers(plans[plans.Count - 1], characterPool, report);
            }

            if (report.HasErrors)
            {
                Finish(report, 0, 0, updateOriginals);
                return;
            }

            // 4. Escribir (solo si no hay ningún error).
            int created = 0, updated = 0;
            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName("Importar diálogos desde CSV");
            int undoGroup = Undo.GetCurrentGroup();

            foreach (WritePlan plan in plans)
            {
                if (plan.Target == null)
                {
                    EnsureFolder(Path.GetDirectoryName(plan.Path)?.Replace('\\', '/'));

                    var asset = ScriptableObject.CreateInstance<DialogueData>();
                    asset.characters = plan.Original != null && plan.Original.characters != null
                        ? plan.Original.characters.Select(CloneCharacter).ToList()
                        : new List<DialogueCharacter>();
                    asset.nodes = plan.Nodes;
                    AddMissingCharacters(asset, plan.Nodes, characterPool);

                    AssetDatabase.CreateAsset(asset, plan.Path);
                    created++;
                }
                else
                {
                    Undo.RecordObject(plan.Target, "Importar diálogos desde CSV");
                    plan.Target.characters ??= new List<DialogueCharacter>();
                    plan.Target.nodes = plan.Nodes;
                    AddMissingCharacters(plan.Target, plan.Nodes, characterPool);
                    EditorUtility.SetDirty(plan.Target);
                    updated++;
                }
            }

            Undo.CollapseUndoOperations(undoGroup);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Finish(report, created, updated, updateOriginals);
        }

        // ==================================================================
        // Validación
        // ==================================================================

        private static DialogueNode FindOwnerNode(Row row, Dictionary<string, List<DialogueNode>> dialogues, Report report)
        {
            string dialogueName = row.Get("dialogue");
            string nodeId = row.Get("nodeId");

            if (!dialogues.TryGetValue(dialogueName, out List<DialogueNode> nodes))
            {
                report.Error(row.Where, $"el diálogo '{dialogueName}' no aparece en la pestaña Nodos.");
                return null;
            }

            DialogueNode node = nodes.Find(n => n.id == nodeId);
            if (node == null)
                report.Error(row.Where, $"no existe el nodo '{nodeId}' en {dialogueName}.");

            return node;
        }

        private static void ValidateDialogue(string dialogueName, List<DialogueNode> nodes, Report report)
        {
            var ids = new HashSet<string>(nodes.Select(n => n.id));

            if (!ids.Contains(StartNodeId))
                report.Error(dialogueName, $"no tiene nodo '{StartNodeId}' (el diálogo siempre empieza por ahí).");

            foreach (DialogueNode node in nodes)
            {
                string where = $"{dialogueName} / nodo '{node.id}'";

                if (!string.IsNullOrEmpty(node.nextNodeID) && !ids.Contains(node.nextNodeID))
                    report.Error(where, $"next apunta a '{node.nextNodeID}', que no existe.");

                if (!string.IsNullOrEmpty(node.wrongEvidenceNodeID) && !ids.Contains(node.wrongEvidenceNodeID))
                    report.Error(where, $"wrongEvidenceNode apunta a '{node.wrongEvidenceNodeID}', que no existe.");

                foreach (DialogueChoice choice in node.choices)
                {
                    string choiceWhere = $"{where} / opción '{choice.text}'";

                    if (string.IsNullOrEmpty(choice.nextNodeID))
                    {
                        if (!choice.endsDialogue)
                            report.Error(choiceWhere, "no tiene next ni endsDialogue: no llevaría a ningún sitio.");
                    }
                    else if (!ids.Contains(choice.nextNodeID))
                    {
                        report.Error(choiceWhere, $"next apunta a '{choice.nextNodeID}', que no existe.");
                    }
                }

                foreach (DialogueEvidenceOption option in node.evidenceOptions)
                {
                    string itemName = option.item != null ? option.item.name : "?";
                    string optionWhere = $"{where} / prueba '{itemName}'";

                    if (string.IsNullOrEmpty(option.nextNodeID))
                        report.Error(optionWhere, "no tiene next: al presentarla el diálogo se rompería.");
                    else if (!ids.Contains(option.nextNodeID))
                        report.Error(optionWhere, $"next apunta a '{option.nextNodeID}', que no existe.");
                }

                if (node.evidenceOptions.Count > 0 && !node.allowsEvidencePresentation)
                    report.Warning(where, "tiene pruebas en la pestaña Pruebas pero allowsEvidence es FALSE: no se podrán presentar.");

                if (node.allowsEvidencePresentation && node.evidenceOptions.Count == 0)
                    report.Warning(where, "allowsEvidence es TRUE pero no tiene ninguna prueba correcta en la pestaña Pruebas.");
            }
        }

        private static void CheckSpeakers(WritePlan plan, Dictionary<string, DialogueCharacter> pool, Report report)
        {
            var known = new HashSet<string>();
            DialogueData source = plan.Target ?? plan.Original;
            if (source?.characters != null)
                foreach (DialogueCharacter c in source.characters)
                    if (c != null && !string.IsNullOrEmpty(c.id))
                        known.Add(c.id);

            foreach (string speaker in plan.Nodes.Select(n => n.speakerID).Where(s => !string.IsNullOrEmpty(s)).Distinct())
            {
                if (known.Contains(speaker))
                    continue;

                if (pool.ContainsKey(speaker))
                    report.Info($"{plan.Name}: se añade el personaje '{speaker}' copiándolo de otro diálogo.");
                else
                    report.Warning(plan.Name, $"el personaje '{speaker}' no existe en ningún DialogueData: saldrá como '???'. Añádelo a mano en el asset.");
            }
        }

        // ==================================================================
        // Personajes
        // ==================================================================

        private static Dictionary<string, DialogueCharacter> BuildCharacterPool(Dictionary<string, DialogueData> originals)
        {
            var pool = new Dictionary<string, DialogueCharacter>();
            foreach (DialogueData data in originals.Values)
            {
                if (data.characters == null)
                    continue;

                foreach (DialogueCharacter c in data.characters)
                    if (c != null && !string.IsNullOrEmpty(c.id) && !pool.ContainsKey(c.id))
                        pool[c.id] = c;
            }
            return pool;
        }

        private static void AddMissingCharacters(DialogueData asset, List<DialogueNode> nodes, Dictionary<string, DialogueCharacter> pool)
        {
            foreach (string speaker in nodes.Select(n => n.speakerID).Where(s => !string.IsNullOrEmpty(s)).Distinct())
            {
                if (asset.characters.Any(c => c != null && c.id == speaker))
                    continue;

                if (pool.TryGetValue(speaker, out DialogueCharacter source))
                    asset.characters.Add(CloneCharacter(source));
            }
        }

        /// <summary>Copia por valor (incluye las referencias a sprites) para no compartir la misma instancia entre assets.</summary>
        private static DialogueCharacter CloneCharacter(DialogueCharacter source)
        {
            if (source == null)
                return null;

            var copy = new DialogueCharacter();
            EditorJsonUtility.FromJsonOverwrite(EditorJsonUtility.ToJson(source), copy);
            return copy;
        }

        // ==================================================================
        // Assets
        // ==================================================================

        /// <summary>DialogueData del proyecto por nombre, sin contar las copias de _ImportTest.</summary>
        private static Dictionary<string, DialogueData> FindOriginalDialogues(Report report)
        {
            var result = new Dictionary<string, DialogueData>();
            foreach (string guid in AssetDatabase.FindAssets("t:DialogueData"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.StartsWith(TestFolder + "/"))
                    continue;

                var data = AssetDatabase.LoadAssetAtPath<DialogueData>(path);
                if (data == null)
                    continue;

                if (result.TryGetValue(data.name, out DialogueData other))
                {
                    report.Error(data.name,
                        $"hay dos DialogueData con ese nombre ({AssetDatabase.GetAssetPath(other)} y {path}). Renombra uno.");
                    continue;
                }

                result[data.name] = data;
            }
            return result;
        }

        private static void EnsureFolder(string folder)
        {
            if (string.IsNullOrEmpty(folder) || AssetDatabase.IsValidFolder(folder))
                return;

            string parent = Path.GetDirectoryName(folder)?.Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
        }

        private class AssetIndex<T> where T : Object
        {
            private readonly string typeLabel;
            private readonly Dictionary<string, List<T>> byName = new();

            public AssetIndex(string typeLabel)
            {
                this.typeLabel = typeLabel;
                foreach (string guid in AssetDatabase.FindAssets($"t:{typeof(T).Name}"))
                {
                    T asset = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
                    if (asset == null)
                        continue;

                    if (!byName.TryGetValue(asset.name, out List<T> list))
                        byName[asset.name] = list = new List<T>();

                    if (!list.Contains(asset))
                        list.Add(asset);
                }
            }

            public T Resolve(string name, string where, Report report)
            {
                if (string.IsNullOrEmpty(name))
                    return null;

                if (!byName.TryGetValue(name, out List<T> found))
                {
                    report.Error(where, $"no existe ningún {typeLabel} llamado '{name}'.");
                    return null;
                }

                if (found.Count > 1)
                {
                    string paths = string.Join(", ", found.Select(a => AssetDatabase.GetAssetPath(a)));
                    report.Error(where, $"hay varios {typeLabel} llamados '{name}' ({paths}). Renombra uno.");
                    return null;
                }

                return found[0];
            }

            public List<T> ResolveList(string cell, string where, Report report)
            {
                var result = new List<T>();
                if (string.IsNullOrEmpty(cell))
                    return result;

                foreach (string part in cell.Split(DialogueSheetCsv.ListSeparator))
                {
                    string name = part.Trim();
                    if (name.Length == 0)
                        continue;

                    T asset = Resolve(name, where, report);
                    if (asset != null)
                        result.Add(asset);
                }
                return result;
            }
        }

        // ==================================================================
        // Lectura de celdas
        // ==================================================================

        private static DialogueExpression ParseExpression(Row row, Report report)
        {
            string value = row.Get("expression");
            if (string.IsNullOrEmpty(value))
                return DialogueExpression.Neutral;

            if (Enum.TryParse(value, true, out DialogueExpression expression))
                return expression;

            report.Error(row.Where,
                $"expresión '{value}' no válida. Opciones: {string.Join(", ", Enum.GetNames(typeof(DialogueExpression)))}.");
            return DialogueExpression.Neutral;
        }

        private static bool ParseBool(Row row, string column, Report report)
        {
            string value = row.Get(column).ToUpperInvariant();
            switch (value)
            {
                case "":
                case "FALSE":
                case "FALSO":
                case "0":
                case "NO":
                    return false;
                case "TRUE":
                case "VERDADERO":
                case "1":
                case "SI":
                case "SÍ":
                case "X":
                    return true;
                default:
                    report.Error(row.Where, $"{column}: '{row.Get(column)}' no es TRUE/FALSE.");
                    return false;
            }
        }

        private static int ParseInt(Row row, string column, Report report)
        {
            string value = row.Get(column);
            if (string.IsNullOrEmpty(value))
                return 0;

            if (int.TryParse(value, out int result))
                return result;

            report.Error(row.Where, $"{column}: '{value}' no es un número entero.");
            return 0;
        }

        // ==================================================================
        // Tablas CSV
        // ==================================================================

        private class Row
        {
            public string Where;
            private readonly Dictionary<string, int> columns;
            private readonly List<string> cells;

            public Row(string where, Dictionary<string, int> columns, List<string> cells)
            {
                Where = where;
                this.columns = columns;
                this.cells = cells;
            }

            /// <summary>Valor recortado (para ids, nombres de assets, etc.).</summary>
            public string Get(string column) => GetRaw(column).Trim();

            /// <summary>Valor tal cual (para textos que se ven en pantalla).</summary>
            public string GetRaw(string column)
            {
                if (!columns.TryGetValue(column, out int index) || index >= cells.Count)
                    return string.Empty;
                return cells[index] ?? string.Empty;
            }
        }

        private class Table
        {
            public readonly List<Row> Rows = new();

            /// <summary>
            /// Busca el CSV cuyo nombre termina en "tabName" (Google Sheets lo descarga como
            /// "NombreHoja - Nodos.csv") y comprueba que tiene todas las columnas.
            /// </summary>
            public static Table Load(string folder, string tabName, string[] requiredHeaders, Report report)
            {
                var table = new Table();

                string[] matches = Directory.GetFiles(folder, "*.csv")
                    .Where(f => Path.GetFileNameWithoutExtension(f).Trim()
                        .EndsWith(tabName, StringComparison.OrdinalIgnoreCase))
                    .ToArray();

                if (matches.Length == 0)
                {
                    report.Error(tabName, $"no hay ningún CSV que termine en '{tabName}' en {folder}.");
                    return table;
                }

                if (matches.Length > 1)
                {
                    report.Error(tabName, $"hay varios CSV para '{tabName}': {string.Join(", ", matches.Select(f => Path.GetFileName(f)))}. Deja solo uno.");
                    return table;
                }

                string fileName = Path.GetFileName(matches[0]);
                List<List<string>> rows = DialogueSheetCsv.Parse(File.ReadAllText(matches[0]));

                if (rows.Count == 0)
                {
                    report.Error(fileName, "el archivo está vacío (ni siquiera tiene cabecera).");
                    return table;
                }

                var columns = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                for (int i = 0; i < rows[0].Count; i++)
                {
                    string header = rows[0][i].Trim().TrimStart('﻿');
                    if (header.Length > 0 && !columns.ContainsKey(header))
                        columns[header] = i;
                }

                foreach (string header in requiredHeaders)
                    if (!columns.ContainsKey(header))
                        report.Error(fileName, $"falta la columna '{header}'.");

                for (int i = 1; i < rows.Count; i++)
                {
                    List<string> cells = rows[i];
                    if (cells.All(string.IsNullOrWhiteSpace))
                        continue; // filas vacías (separadores, final de la hoja)

                    // i + 1 = número de fila tal como se ve en Google Sheets.
                    table.Rows.Add(new Row($"{tabName}, fila {i + 1}", columns, cells));
                }

                return table;
            }
        }

        // ==================================================================
        // Informe
        // ==================================================================

        private class WritePlan
        {
            public string Name;
            public string Path;
            public DialogueData Target;
            public DialogueData Original;
            public List<DialogueNode> Nodes;
        }

        private class Report
        {
            public readonly List<string> Errors = new();
            public readonly List<string> Warnings = new();
            public readonly List<string> Infos = new();

            public bool HasErrors => Errors.Count > 0;

            public void Error(string where, string message) => Errors.Add($"{where}: {message}");
            public void Warning(string where, string message) => Warnings.Add($"{where}: {message}");
            public void Info(string message) => Infos.Add(message);
        }

        private static void Finish(Report report, int created, int updated, bool updateOriginals)
        {
            foreach (string info in report.Infos)
                Debug.Log($"[DialogueImport] {info}");
            foreach (string warning in report.Warnings)
                Debug.LogWarning($"[DialogueImport] {warning}");
            foreach (string error in report.Errors)
                Debug.LogError($"[DialogueImport] {error}");

            string message;
            if (report.HasErrors)
            {
                message = $"No se ha importado nada: hay {report.Errors.Count} errores.\n\n" +
                          "Están en la Console con la pestaña y la fila. Corrígelos en la hoja, " +
                          "vuelve a descargar los CSV e importa otra vez.";
            }
            else
            {
                string destination = updateOriginals ? "originales" : $"prueba ({TestFolder})";
                message = $"Importación correcta en {destination}.\n\nCreados: {created}\nActualizados: {updated}";
                if (report.Warnings.Count > 0)
                    message += $"\n\n{report.Warnings.Count} avisos en la Console.";
            }

            Debug.Log($"[DialogueImport] {message}");
            EditorUtility.DisplayDialog(Title, message, "OK");
        }
    }
}
