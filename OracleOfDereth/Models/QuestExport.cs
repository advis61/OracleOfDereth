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

        // Writes the quests back out as /myquests lines, byte-for-byte the shape the game prints
        // them, so the file is interchangeable with a real chat log — the same parser reads either.
        // Generating it from the tracked flags rather than capturing a chat log means no /log
        // toggling, no client buffering to race, and no surrounding combat spam in the file.
        //
        // Only flags the character actually holds have server data to write, so a quest with no
        // tracked QuestFlag is skipped rather than invented.
        public static string ToMyQuests(List<Quest> quests, string nameOverride = null)
        {
            string path = ExportPath("txt", nameOverride, "newflags");

            var lines = new List<string>();
            foreach (Quest quest in quests)
            {
                QuestFlag.QuestFlags.TryGetValue(quest.Flag, out QuestFlag questFlag);
                if (questFlag == null) { continue; }

                lines.Add(MyQuestsLine(questFlag));
            }

            File.WriteAllLines(path, lines);
            return path;
        }

        // The two shapes the game uses: with a description it carries the quoted name plus the
        // max-solves and repeat-timer fields, without one the line simply stops after the
        // completion stamp. Emitting an empty "" pair instead would not round-trip.
        public static string MyQuestsLine(QuestFlag questFlag)
        {
            long completedOn = Util.DateTimeToUnixTimeStamp(questFlag.CompletedOn);
            string description = CleanDescription(questFlag.Description);

            if (description.Length == 0)
            {
                return $"{questFlag.Key} - {questFlag.Solves} solves ({completedOn})";
            }

            return $"{questFlag.Key} - {questFlag.Solves} solves ({completedOn}) \"{description}\" {questFlag.MaxSolves} {(long)questFlag.RepeatTime.TotalSeconds}";
        }

        // The parsed description keeps the quote characters that delimited it (and, for the ones
        // that follow a space in the raw line, that space too). Strip both so re-quoting produces
        // one pair rather than nesting them.
        private static string CleanDescription(string description)
        {
            if (string.IsNullOrEmpty(description)) { return ""; }

            return new string(description.Where(c => c != '"' && c != '\'').ToArray()).Trim();
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

            return $"{quest.Flag} | {quest.DisplayName()} | {repeat} | {quest.Status()}{solves}";
        }

        // `suffix` is what distinguishes one export from another in a folder that collects them
        // all: the three full-list exports are "-quests", the Send button's filtered one is
        // "-newflags", so a .txt from each doesn't read as the same kind of file.
        private static string ExportPath(string extension, string nameOverride = null, string suffix = "quests")
        {
            string raw = string.IsNullOrEmpty(nameOverride) ? CoreManager.Current.CharacterFilter.Name : nameOverride;
            string name = Regex.Replace((raw ?? "").ToLower(), "[^a-z0-9]", "-");

            string filename = $"{DateTime.Now:yyyyMMdd-HHmmss}-{name}-{suffix}.{extension}";
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
                quest.DisplayName(),
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

        // One escaping rule for every csv the plugin writes, and the exact inverse of the
        // Util.CsvParseLine every reader now uses — so an export can be read back in.
        private static string CsvEscape(string value) => Util.CsvEscape(value);

        private static string JsonEscape(string value)
        {
            if (value == null) return "null";
            return "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
        }
    }
}
