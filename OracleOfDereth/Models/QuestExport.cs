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
        // Writes the quests back out as /myquests lines, byte-for-byte the shape the game prints
        // them, so the file is interchangeable with a real chat log — the same parser reads either.
        // Generating it from the tracked flags rather than capturing a chat log means no /log
        // toggling, no client buffering to race, and no surrounding combat spam in the file.
        //
        // Only flags the character actually holds have server data to write, so a quest with no
        // tracked QuestFlag is skipped rather than invented.
        public static string ToMyQuests(List<Quest> quests, string nameOverride = null)
        {
            string path = ExportPath(nameOverride);

            var lines = new List<string>();
            foreach (Quest quest in quests)
            {
                QuestFlag.QuestFlags.TryGetValue(quest.Flag, out QuestFlag questFlag);
                if (questFlag != null) lines.Add(MyQuestsLine(questFlag));
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

            return $"{quest.Flag} | {quest.DisplayName()} | {repeat} | {quest.StatusInQuestView()}{solves}";
        }

        private static string ExportPath(string nameOverride)
        {
            string raw = string.IsNullOrEmpty(nameOverride) ? CoreManager.Current.CharacterFilter.Name : nameOverride;
            string name = Regex.Replace((raw ?? "").ToLower(), "[^a-z0-9]", "-");

            string filename = $"{DateTime.Now:yyyyMMdd-HHmmss}-{name}-newflags.txt";
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), filename);
        }

    }
}
