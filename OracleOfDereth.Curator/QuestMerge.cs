using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace OracleOfDereth.Curator
{
    internal sealed class MergeItem
    {
        public string Action { get; set; }
        public string Flag { get; set; }
        public string Quest { get; set; }
        public string Source { get; set; }
        public bool NameInferred { get; set; }
    }

    internal sealed class MergeResult
    {
        public CsvTable Master { get; set; }
        public List<MergeItem> Items { get; } = new List<MergeItem>();
        public int Added => Items.Count(i => i.Action == "Add");
        public int Verified => Items.Count(i => i.Action == "Verify" || i.Action == "Verify + make shared");
        public int Unchanged => Items.Count(i => i.Action == "Already verified");
    }

    internal static class QuestMerge
    {
        private const string FlagColumn = "QuestFlag";
        private const string ServerColumn = "Server";
        private const string VerifiedColumn = "Verified Conquest";

        public static MergeResult Build(string masterPath, IEnumerable<string> inputPaths)
        {
            var master = CsvTable.Read(masterPath);
            Require(master, masterPath, FlagColumn, ServerColumn, VerifiedColumn);

            var byFlag = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
            foreach (var row in master.Rows)
            {
                string flag = CsvTable.Get(row, FlagColumn).Trim();
                if (flag.Length == 0) throw new InvalidDataException("Master contains a row with no QuestFlag.");
                if (byFlag.ContainsKey(flag)) throw new InvalidDataException($"Master contains duplicate QuestFlag '{flag}'.");
                byFlag.Add(flag, row);
            }

            var result = new MergeResult { Master = master };
            var processed = new Dictionary<string, MergeItem>(StringComparer.OrdinalIgnoreCase);
            foreach (string inputPath in inputPaths)
            {
                var input = CsvTable.Read(inputPath);
                Require(input, inputPath, FlagColumn);
                foreach (var incoming in input.Rows)
                {
                    string flag = CsvTable.Get(incoming, FlagColumn).Trim().ToLowerInvariant();
                    if (flag.Length == 0) throw new InvalidDataException($"Submission contains a row with no QuestFlag: {inputPath}");

                    if (processed.TryGetValue(flag, out MergeItem prior))
                    {
                        // Multiple players may report the same flag. For a row being added, retain
                        // the first non-empty value in each field and use later reports only to fill
                        // gaps. Existing curated rows never take metadata from submissions.
                        if (prior.Action == "Add" && byFlag.TryGetValue(flag, out var addedRow))
                        {
                            FillEmptyFields(master, input, addedRow, incoming);
                            string submittedName = CsvTable.Get(incoming, "Quest");
                            if (prior.NameInferred && QuestNameGuesser.IsUsefulName(submittedName))
                            {
                                addedRow[master.ActualHeader("Quest")] = submittedName;
                                prior.NameInferred = false;
                            }
                            prior.Quest = CsvTable.Get(addedRow, "Quest");
                        }
                        AddSource(prior, inputPath);
                        continue;
                    }

                    if (byFlag.TryGetValue(flag, out var existing))
                    {
                        bool verified = string.Equals(CsvTable.Get(existing, VerifiedColumn), "Verified", StringComparison.OrdinalIgnoreCase);
                        if (!verified) existing[master.ActualHeader(VerifiedColumn)] = "Verified";
                        string existingServer = CsvTable.Get(existing, ServerColumn);
                        bool broadened = existingServer.Length > 0 &&
                            !string.Equals(existingServer, "Conquest", StringComparison.OrdinalIgnoreCase);
                        // Server-specific rows are filtered out on other worlds. Conquest evidence
                        // proves this one is shared, so remove its old single-server scope.
                        if (broadened) existing[master.ActualHeader(ServerColumn)] = "";
                        string action = broadened ? "Verify + make shared"
                            : verified ? "Already verified"
                            : "Verify";
                        MergeItem item = Item(action, flag, existing, inputPath);
                        result.Items.Add(item);
                        processed.Add(flag, item);
                        continue;
                    }

                    var added = master.Headers.ToDictionary(h => h, h => "", StringComparer.OrdinalIgnoreCase);
                    FillEmptyFields(master, input, added, incoming);
                    added[master.ActualHeader(FlagColumn)] = flag;
                    added[master.ActualHeader(ServerColumn)] = "Conquest";
                    added[master.ActualHeader(VerifiedColumn)] = "Verified";
                    bool inferred = false;
                    string questHeader = master.ActualHeader("Quest");
                    if (questHeader != null && !QuestNameGuesser.IsUsefulName(added[questHeader]))
                    {
                        string guess = QuestNameGuesser.Guess(flag);
                        if (guess.Length > 0) { added[questHeader] = guess; inferred = true; }
                    }
                    master.Rows.Add(added);
                    byFlag.Add(flag, added);
                    MergeItem addedItem = Item("Add", flag, added, inputPath);
                    addedItem.NameInferred = inferred;
                    result.Items.Add(addedItem);
                    processed.Add(flag, addedItem);
                }
            }

            master.Rows.Sort((left, right) => StringComparer.OrdinalIgnoreCase.Compare(
                CsvTable.Get(left, FlagColumn),
                CsvTable.Get(right, FlagColumn)));
            ValidateMergedMaster(master);
            return result;
        }

        private static void ValidateMergedMaster(CsvTable master)
        {
            var flags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < master.Rows.Count; i++)
            {
                string flag = CsvTable.Get(master.Rows[i], FlagColumn).Trim();
                if (flag.Length == 0)
                    throw new InvalidDataException($"Merged CSV row {i + 2} has no QuestFlag.");
                if (!flags.Add(flag))
                    throw new InvalidDataException($"Merged CSV contains duplicate QuestFlag '{flag}'.");
            }
        }

        private static MergeItem Item(string action, string flag, Dictionary<string, string> row, string path) => new MergeItem
        {
            Action = action,
            Flag = flag,
            Quest = CsvTable.Get(row, "Quest"),
            Source = Path.GetFileName(path)
        };

        private static void FillEmptyFields(CsvTable master, CsvTable input,
            Dictionary<string, string> target, Dictionary<string, string> incoming)
        {
            foreach (string header in master.Headers)
            {
                string inputHeader = input.ActualHeader(header);
                if (inputHeader == null || CsvTable.Get(target, header).Length > 0) continue;
                target[header] = CsvTable.Get(incoming, inputHeader);
            }
        }

        private static void AddSource(MergeItem item, string path)
        {
            string file = Path.GetFileName(path);
            var sources = new HashSet<string>((item.Source ?? "").Split(new[] { ", " }, StringSplitOptions.RemoveEmptyEntries), StringComparer.OrdinalIgnoreCase);
            if (sources.Add(file)) item.Source = string.Join(", ", sources);
        }

        private static void Require(CsvTable table, string path, params string[] columns)
        {
            foreach (string column in columns)
                if (!table.HasColumn(column)) throw new InvalidDataException($"CSV is missing '{column}': {path}");
        }
    }
}
