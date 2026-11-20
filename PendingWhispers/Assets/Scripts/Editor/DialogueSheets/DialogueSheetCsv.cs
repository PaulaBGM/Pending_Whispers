using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PendingWhispers.EditorTools
{
    /// <summary>
    /// Formato compartido por el exportador y el importador de diálogos.
    /// Las columnas de aquí son el "contrato" con la Google Sheet: si se cambian,
    /// hay que cambiar también la cabecera de la hoja.
    /// </summary>
    public static class DialogueSheetCsv
    {
        public const string NodesFile = "Nodos.csv";
        public const string ChoicesFile = "Opciones.csv";
        public const string EvidenceFile = "Pruebas.csv";

        /// <summary>Separador para varios valores en una misma celda (p. ej. varias flags).</summary>
        public const char ListSeparator = ';';

        public static readonly string[] NodeHeaders =
        {
            "dialogue", "id", "speaker", "text", "next", "expression", "testimony",
            "requiredFlags", "onEnterFlags", "onEnterEvents",
            "allowsEvidence", "wrongEvidenceNode", "wrongEvidenceFeedback"
        };

        public static readonly string[] ChoiceHeaders =
        {
            "dialogue", "nodeId", "text", "next", "requiredFlags", "addFlags",
            "onSelectedEvent", "endsDialogue", "requiredReputation", "reputationChange"
        };

        public static readonly string[] EvidenceHeaders =
        {
            "dialogue", "nodeId", "item", "next", "addFlags", "onSelectedEvent"
        };

        // ---------- Escritura ----------

        public static void AppendRow(StringBuilder sb, IEnumerable<string> cells)
        {
            sb.Append(string.Join(",", cells.Select(Escape)));
            sb.Append('\n');
        }

        private static string Escape(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            bool needsQuotes = value.IndexOfAny(new[] { ',', '"', '\n', '\r' }) >= 0;
            string escaped = value.Replace("\"", "\"\"");
            return needsQuotes ? $"\"{escaped}\"" : escaped;
        }

        public static string Bool(bool value) => value ? "TRUE" : "FALSE";

        // ---------- Lectura (la usará el importador) ----------

        /// <summary>Parser CSV estándar: comillas, comas y saltos de línea dentro de celdas.</summary>
        public static List<List<string>> Parse(string csv)
        {
            var rows = new List<List<string>>();
            var row = new List<string>();
            var cell = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < csv.Length; i++)
            {
                char c = csv[i];

                if (inQuotes)
                {
                    if (c == '"')
                    {
                        if (i + 1 < csv.Length && csv[i + 1] == '"')
                        {
                            cell.Append('"');
                            i++;
                        }
                        else
                        {
                            inQuotes = false;
                        }
                    }
                    else
                    {
                        cell.Append(c);
                    }
                    continue;
                }

                switch (c)
                {
                    case '"':
                        inQuotes = true;
                        break;
                    case ',':
                        row.Add(cell.ToString());
                        cell.Clear();
                        break;
                    case '\r':
                        break;
                    case '\n':
                        row.Add(cell.ToString());
                        cell.Clear();
                        rows.Add(row);
                        row = new List<string>();
                        break;
                    default:
                        cell.Append(c);
                        break;
                }
            }

            if (cell.Length > 0 || row.Count > 0)
            {
                row.Add(cell.ToString());
                rows.Add(row);
            }

            return rows;
        }
    }
}
