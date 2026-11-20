using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Inventory.Model;
using UnityEditor;
using UnityEngine;

namespace PendingWhispers.EditorTools
{
    /// <summary>
    /// Tools > Dialogue > Exportar a CSV
    /// Vuelca los DialogueData a tres CSV (Nodos, Opciones, Pruebas) para pegarlos
    /// como pestañas en Google Sheets. Solo lee los assets: no modifica nada.
    ///
    /// Si hay carpetas seleccionadas en la ventana Project, exporta solo lo que hay
    /// dentro de ellas; si no, exporta todos los DialogueData del proyecto.
    /// Las referencias (flags, eventos, pistas, testimonios) se escriben por nombre de asset.
    /// </summary>
    public static class DialogueSheetExporter
    {
        private const string DefaultFolderName = "DialogueSheets";
        private const string Title = "Exportar diálogos";

        [MenuItem("Tools/Dialogue/Exportar a CSV")]
        public static void ExportMenu()
        {
            string[] searchFolders = GetSelectedFolders();
            string scope = searchFolders.Length > 0 ? string.Join(", ", searchFolders) : "todo el proyecto";

            List<DialogueData> dialogues = FindDialogues(searchFolders);
            if (dialogues.Count == 0)
            {
                EditorUtility.DisplayDialog(Title, $"No hay ningún DialogueData en {scope}.", "OK");
                return;
            }

            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            string defaultFolder = Path.Combine(projectRoot, DefaultFolderName);
            Directory.CreateDirectory(defaultFolder);

            string outputFolder = EditorUtility.SaveFolderPanel("Carpeta donde guardar los CSV", defaultFolder, "");
            if (string.IsNullOrEmpty(outputFolder))
                return;

            var warnings = new List<string>();
            int nodeCount = Export(dialogues, outputFolder, warnings);

            foreach (string warning in warnings)
                Debug.LogWarning($"[DialogueExport] {warning}");

            string summary =
                $"Exportados {dialogues.Count} diálogos ({nodeCount} nodos) desde {scope}.\n\n" +
                $"Carpeta: {outputFolder}";

            if (warnings.Count > 0)
                summary += $"\n\n{warnings.Count} avisos: revisa la Console.";

            Debug.Log($"[DialogueExport] {summary}");
            EditorUtility.DisplayDialog(Title, summary, "OK");
            EditorUtility.RevealInFinder(Path.Combine(outputFolder, DialogueSheetCsv.NodesFile));
        }

        // ------------------------------------------------------------------

        private static int Export(List<DialogueData> dialogues, string outputFolder, List<string> warnings)
        {
            var nodes = new StringBuilder();
            var choices = new StringBuilder();
            var evidence = new StringBuilder();

            DialogueSheetCsv.AppendRow(nodes, DialogueSheetCsv.NodeHeaders);
            DialogueSheetCsv.AppendRow(choices, DialogueSheetCsv.ChoiceHeaders);
            DialogueSheetCsv.AppendRow(evidence, DialogueSheetCsv.EvidenceHeaders);

            int nodeCount = 0;

            foreach (DialogueData dialogue in dialogues)
            {
                if (dialogue.nodes == null)
                    continue;

                string dialogueName = dialogue.name;
                var seenIds = new HashSet<string>();

                foreach (DialogueNode node in dialogue.nodes)
                {
                    if (node == null)
                        continue;

                    string where = $"{dialogueName} / nodo '{node.id}'";

                    if (string.IsNullOrWhiteSpace(node.id))
                        warnings.Add($"{dialogueName}: hay un nodo sin id.");
                    else if (!seenIds.Add(node.id))
                        warnings.Add($"{dialogueName}: id de nodo duplicado '{node.id}'.");

                    DialogueSheetCsv.AppendRow(nodes, new[]
                    {
                        dialogueName,
                        node.id,
                        node.speakerID,
                        node.text,
                        node.nextNodeID,
                        node.expression.ToString(),
                        Name(node.testimony),
                        Names(node.requiredFlags, $"{where} (requiredFlags)", warnings),
                        Names(node.onEnterFlags, $"{where} (onEnterFlags)", warnings),
                        Names(node.onEnterEvents, $"{where} (onEnterEvents)", warnings),
                        DialogueSheetCsv.Bool(node.allowsEvidencePresentation),
                        node.wrongEvidenceNodeID,
                        node.wrongEvidenceFeedback
                    });
                    nodeCount++;

                    if (node.choices != null)
                    {
                        foreach (DialogueChoice choice in node.choices)
                        {
                            if (choice == null)
                                continue;

                            string choiceWhere = $"{where} / opción '{choice.text}'";

                            DialogueSheetCsv.AppendRow(choices, new[]
                            {
                                dialogueName,
                                node.id,
                                choice.text,
                                choice.nextNodeID,
                                Names(choice.requiredFlags, $"{choiceWhere} (requiredFlags)", warnings),
                                Names(choice.addFlags, $"{choiceWhere} (addFlags)", warnings),
                                Name(choice.onSelectedEvent),
                                DialogueSheetCsv.Bool(choice.endsDialogue),
                                choice.requiredReputation.ToString(),
                                choice.reputationChange.ToString()
                            });
                        }
                    }

                    if (node.evidenceOptions != null)
                    {
                        foreach (DialogueEvidenceOption option in node.evidenceOptions)
                        {
                            if (option == null)
                                continue;

                            if (option.item == null)
                                warnings.Add($"{where}: opción de prueba sin ítem asignado.");

                            DialogueSheetCsv.AppendRow(evidence, new[]
                            {
                                dialogueName,
                                node.id,
                                Name(option.item),
                                option.nextNodeID,
                                Names(option.addFlags, $"{where} (prueba {Name(option.item)} / addFlags)", warnings),
                                Name(option.onSelectedEvent)
                            });
                        }
                    }
                }
            }

            var utf8 = new UTF8Encoding(false);
            File.WriteAllText(Path.Combine(outputFolder, DialogueSheetCsv.NodesFile), nodes.ToString(), utf8);
            File.WriteAllText(Path.Combine(outputFolder, DialogueSheetCsv.ChoicesFile), choices.ToString(), utf8);
            File.WriteAllText(Path.Combine(outputFolder, DialogueSheetCsv.EvidenceFile), evidence.ToString(), utf8);

            return nodeCount;
        }

        // ------------------------------------------------------------------

        private static string Name(Object asset) => asset != null ? asset.name : string.Empty;

        /// <summary>Nombres de assets separados por ';'. Avisa de huecos vacíos en la lista.</summary>
        private static string Names<T>(List<T> assets, string where, List<string> warnings) where T : Object
        {
            if (assets == null || assets.Count == 0)
                return string.Empty;

            var names = new List<string>();
            foreach (T asset in assets)
            {
                if (asset == null)
                {
                    warnings.Add($"{where}: hay un hueco vacío (None) en la lista; se ignora.");
                    continue;
                }

                if (asset.name.IndexOf(DialogueSheetCsv.ListSeparator) >= 0)
                    warnings.Add($"{where}: el asset '{asset.name}' lleva ';' en el nombre y se leerá mal al importar.");

                names.Add(asset.name);
            }

            return string.Join(DialogueSheetCsv.ListSeparator.ToString(), names);
        }

        private static string[] GetSelectedFolders()
        {
            return Selection.GetFiltered<Object>(SelectionMode.Assets)
                .Select(obj => AssetDatabase.GetAssetPath(obj))
                .Where(path => AssetDatabase.IsValidFolder(path))
                .ToArray();
        }

        private static List<DialogueData> FindDialogues(string[] folders)
        {
            string[] guids = folders.Length > 0
                ? AssetDatabase.FindAssets("t:DialogueData", folders)
                : AssetDatabase.FindAssets("t:DialogueData");

            return guids
                .Select(guid => AssetDatabase.GUIDToAssetPath(guid))
                .OrderBy(path => path)
                .Select(path => AssetDatabase.LoadAssetAtPath<DialogueData>(path))
                .Where(d => d != null)
                .ToList();
        }
    }
}
