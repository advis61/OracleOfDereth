using Decal.Adapter;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace OracleOfDereth
{
    // Serializes a list of Quests to disk (txt / csv / json) under My Documents and returns the
    // path written. The Flags-tab counterpart to ItemExport, deliberately self-contained the same
    // way each quest model carries its own CSV loader.
    public static class QuestExport
    {
        public static string ToText(List<Quest> quests, string nameOverride = null)
        {
            string path = ExportPath("txt", nameOverride);
            File.WriteAllLines(path, quests.Select(Describe));
            return path;
        }

        public static string ToCsv(List<Quest> quests, string nameOverride = null)
        {
            string path = ExportPath("csv", nameOverride);

            var lines = new List<string> { string.Join(",", Headers.Select(CsvEscape)) };
            foreach (Quest quest in quests)
                lines.Add(string.Join(",", Row(quest).Select(CsvEscape)));

            File.WriteAllLines(path, lines);
            return path;
        }

        public static string ToJson(List<Quest> quests, string nameOverride = null)
        {
            string path = ExportPath("json", nameOverride);

            var sb = new StringBuilder();
            sb.AppendLine("[");
            for (int i = 0; i < quests.Count; i++)
            {
                string[] row = Row(quests[i]);
                sb.AppendLine("  {");

                int colCount = Math.Min(Headers.Length, row.Length);
                for (int c = 0; c < colCount; c++)
                {
                    string comma = c < colCount - 1 ? "," : "";
                    sb.AppendLine($"    {JsonEscape(Headers[c])}: {JsonEscape(row[c])}{comma}");
                }

                sb.AppendLine("  }" + (i < quests.Count - 1 ? "," : ""));
            }
            sb.AppendLine("]");

            File.WriteAllText(path, sb.ToString());
            return path;
        }

        // One line per quest — what the text export writes and what the Copy button puts on the
        // clipboard, mirroring the flag / name / ready / solves columns actually on screen.
        public static string Describe(Quest quest)
        {
            // Repeat type sits before the status so the fields line up row to row — the trailing
            // solve count is the only part that comes and goes.
            string repeat = quest.IsOneTime() ? "one-time" : "repeatable";

            string solves = quest.SolvesText();
            if (solves.Length > 0) { solves = $" | {solves} solves"; }

            return $"{quest.Flag} | {quest.Name} | {repeat} | {quest.Status()}{solves}";
        }

        private static string ExportPath(string extension, string nameOverride = null)
        {
            string raw = string.IsNullOrEmpty(nameOverride) ? CoreManager.Current.CharacterFilter.Name : nameOverride;
            string name = Regex.Replace((raw ?? "").ToLower(), "[^a-z0-9]", "-");

            string filename = $"{DateTime.Now:yyyyMMdd-HHmmss}-{name}-quests.{extension}";
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), filename);
        }

        private static readonly string[] Headers =
        {
            "Character", "Server", "Quest Flag", "Name", "Completed", "Ready", "Solves",
            "Completed On (UTC)", "One Time", "New", "Url", "Info", "Hint"
        };

        private static string[] Row(Quest quest)
        {
            QuestFlag.QuestFlags.TryGetValue(quest.Flag, out QuestFlag questFlag);

            return new[] {
                CoreManager.Current.CharacterFilter.Name,
                Server.Name,
                quest.Flag,
                quest.Name,
                quest.IsComplete() ? "Yes" : "No",
                quest.Status(),
                // Raw count here, unlike the tab's Solves column — an export is data, so a
                // one-time stamp's count is worth carrying even though the tab hides it.
                questFlag == null ? "" : questFlag.Solves.ToString(),
                questFlag == null || questFlag.CompletedOn == DateTime.MinValue ? "" : questFlag.CompletedOn.ToString("yyyy-MM-dd HH:mm:ss"),
                quest.IsOneTime() ? "Yes" : "No",
                quest.IsNew ? "Yes" : "No",
                quest.Url,
                quest.Info,
                quest.Hint,
            };
        }

        private static string CsvEscape(string value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            if (value.Contains(",") || value.Contains("\"") || value.Contains("\n"))
                return "\"" + value.Replace("\"", "\"\"") + "\"";
            return value;
        }

        private static string JsonEscape(string value)
        {
            if (value == null) return "null";
            return "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
        }
    }
}
