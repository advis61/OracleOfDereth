using Decal.Adapter;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace OracleOfDereth
{
    // Formats quest rows for Copy and writes submission files under My Documents.
    public static class QuestExport
    {
        public static string ToSubmissionCsv(List<Quest> quests, string nameOverride = null)
        {
            string path = ExportPath(nameOverride);

            var lines = new List<string>
            {
                "QuestFlag,Server,Verified Conquest,Quest,Url,Info,Hint,Repeatable"
            };
            foreach (Quest quest in quests)
            {
                QuestFlag.QuestFlags.TryGetValue(quest.Flag, out QuestFlag questFlag);
                string description = questFlag?.Description ?? "";
                string[] row =
                {
                    quest.Flag,
                    Server.Name,
                    Server.IsConquest ? "Verified" : "",
                    quest.Name,
                    quest.Url,
                    quest.Info,
                    description,
                    quest.IsRepeatable() ? "TRUE" : "FALSE"
                };
                lines.Add(string.Join(",", row.Select(Util.CsvEscape)));
            }

            File.WriteAllLines(path, lines);
            return path;
        }

        // One line per quest for Copy, mirroring the columns on screen.
        public static string Describe(Quest quest)
        {
            // Repeat type sits before the status so the fields line up row to row — the trailing
            // solve count is the only part that comes and goes.
            string repeat = quest.IsOneTime() ? "one-time" : "repeatable";

            string solves = quest.SolvesText();
            if (solves.Length > 0) { solves = $" | {solves} solves"; }

            return $"{quest.Flag} | {quest.DisplayName()} | {repeat} | {quest.StatusInQuestView()}{solves}";
        }

        private static string ExportPath(string nameOverride)
        {
            string raw = string.IsNullOrEmpty(nameOverride) ? CoreManager.Current.CharacterFilter.Name : nameOverride;
            string name = Regex.Replace((raw ?? "").ToLower(), "[^a-z0-9]", "-");

            string filename = $"{DateTime.Now:yyyyMMdd-HHmmss}-{name}-newflags.csv";
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), filename);
        }

    }
}
