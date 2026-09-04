using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace OracleOfDereth
{
    // The working quest catalog: seeded from quests.csv, then extended with IsNew runtime discoveries.
    public static class QuestCatalog
    {
        public static List<Quest> Quests { get; private set; } = new List<Quest>();
        private static readonly Dictionary<string, Quest> questsByFlag =
            new Dictionary<string, Quest>(StringComparer.OrdinalIgnoreCase);

        public static bool Contains(string flag) =>
            !string.IsNullOrEmpty(flag) && questsByFlag.ContainsKey(flag);
        public static Quest.SortType CurrentSortType { get; private set; } = Quest.SortType.FlagAscending;
        public static bool UsingBundledList { get; private set; }
        private static bool openBundledOnce;

        private static readonly string[] FlagNames = { "questflag", "flag" };
        private static readonly string[] ServerNames = { "server", "world" };
        private static readonly string[] NameNames = { "quest", "questname", "name", "title" };
        private static readonly string[] UrlNames = { "url", "link", "wiki" };
        private static readonly string[] InfoNames = { "info", "notes", "description" };
        private static readonly string[] HintNames = { "hint", "hints", "directions", "walkthrough" };
        private static readonly string[] RepeatNames = { "repeatable", "repeat" };

        public static string FilePath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.Personal),
            @"Decal Plugins\Oracle of Dereth\quests.csv");

        public static void Init()
        {
            Quests.Clear();
            questsByFlag.Clear();
            CurrentSortType = Quest.SortType.FlagAscending;

            using (var reader = OpenCsv())
            {
                string headerLine = reader.ReadLine();
                if (headerLine == null) throw new InvalidDataException("CSV file is empty.");

                Dictionary<string, int> columns = MapColumns(headerLine);
                int flagCol = ColumnIndex(columns, FlagNames);
                if (flagCol < 0) throw new InvalidDataException("CSV has no quest flag column.");

                int serverCol = ColumnIndex(columns, ServerNames);
                int verifiedCol = VerificationColumn(columns, Server.Name);
                int nameCol = ColumnIndex(columns, NameNames);
                int urlCol = ColumnIndex(columns, UrlNames);
                int infoCol = ColumnIndex(columns, InfoNames);
                int hintCol = ColumnIndex(columns, HintNames);
                int repeatCol = ColumnIndex(columns, RepeatNames);

                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine();
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    string[] fields = Util.CsvParseLine(line);
                    string flag = Field(fields, flagCol).ToLowerInvariant();
                    if (flag.Length == 0) continue;

                    var quest = new Quest
                    {
                        Flag = flag,
                        Server = Field(fields, serverCol),
                        Verified = IsTrue(Field(fields, verifiedCol)),
                        Name = Field(fields, nameCol),
                        Url = Field(fields, urlCol),
                        Info = Field(fields, infoCol),
                        Hint = Field(fields, hintCol),
                        Repeatable = IsTrue(Field(fields, repeatCol))
                    };

                    if (quest.Server.Length > 0 &&
                        !string.Equals(quest.Server, Server.Name, StringComparison.OrdinalIgnoreCase)) continue;

                    Quests.Add(quest);
                    questsByFlag[flag] = quest;
                }
            }
        }

        public static void Reload()
        {
            List<Quest> discovered = Quests.Where(q => q.IsNew && QuestState.Observed(q.Flag)).ToList();
            Quest.SortType sortType = CurrentSortType;
            Init();

            foreach (Quest quest in discovered)
            {
                if (questsByFlag.ContainsKey(quest.Flag)) continue;
                Quests.Add(quest);
                questsByFlag[quest.Flag] = quest;
            }

            Sort(sortType);
        }

        public static void Reset()
        {
            File.Delete(FilePath);
            File.Delete(FilePath + ".bak");
            openBundledOnce = true;
            Reload();
            QuestCatalogUpdater.RecordCurrentVersion();
            Util.Chat("Quest list reset to the bundled catalog. The local quests.csv and its recovery backup were deleted.", Util.ColorPink, "");
        }

        public static string ContentVersion()
        {
            try
            {
                using (var reader = UsingBundledList ? OpenEmbeddedCsv() : new StreamReader(FilePath))
                    return QuestCatalogUpdater.ContentVersion(reader.ReadToEnd());
            }
            catch (Exception ex)
            {
                Util.Log(ex);
                return "Unknown";
            }
        }

        public static void Add(QuestFlag flag)
        {
            if (flag == null || string.IsNullOrEmpty(flag.Key)) return;

            if (questsByFlag.TryGetValue(flag.Key, out Quest discovered))
            {
                if (discovered.IsNew)
                {
                    string name = CleanDescription(flag.Description);
                    if (name.Length > 0) discovered.Name = name;
                    discovered.Repeatable = flag.RepeatTime != TimeSpan.Zero;
                }
                return;
            }

            var quest = new Quest
            {
                Flag = flag.Key.ToLowerInvariant(),
                Name = CleanDescription(flag.Description),
                Repeatable = flag.RepeatTime != TimeSpan.Zero,
                IsNew = true
            };
            Quests.Add(quest);
            questsByFlag[flag.Key] = quest;
        }

        public static void AddHistorical(string flag)
        {
            if (string.IsNullOrEmpty(flag) || questsByFlag.ContainsKey(flag)) return;

            var quest = new Quest { Flag = flag.ToLowerInvariant(), IsNew = true };
            Quests.Add(quest);
            questsByFlag[flag] = quest;
        }

        public static void RemoveUnobservedDiscoveries()
        {
            foreach (Quest quest in Quests.Where(q => q.IsNew && !QuestState.Observed(q.Flag)).ToList())
            {
                Quests.Remove(quest);
                questsByFlag.Remove(quest.Flag);
            }
        }

        private static string CleanDescription(string description)
        {
            if (string.IsNullOrEmpty(description)) return "";
            return new string(description.Where(c => c != '"' && c != '\'').ToArray()).Trim();
        }

        public static void Sort(Quest.SortType sortType)
        {
            CurrentSortType = sortType;
            switch (sortType)
            {
                case Quest.SortType.CompleteAscending:
                    Quests = Quests.OrderBy(q => q.IsCompleteInQuestView()).ThenBy(q => q.Flag).ToList();
                    break;
                case Quest.SortType.CompleteDescending:
                    Quests = Quests.OrderByDescending(q => q.IsCompleteInQuestView()).ThenBy(q => q.Flag).ToList();
                    break;
                case Quest.SortType.FlagAscending:
                    Quests = Quests.OrderBy(q => q.Flag).ToList();
                    break;
                case Quest.SortType.FlagDescending:
                    Quests = Quests.OrderByDescending(q => q.Flag).ToList();
                    break;
                case Quest.SortType.NameAscending:
                    Quests = Quests.OrderBy(q => q.DisplayName()).ThenBy(q => q.Flag).ToList();
                    break;
                case Quest.SortType.NameDescending:
                    Quests = Quests.OrderByDescending(q => q.DisplayName()).ThenBy(q => q.Flag).ToList();
                    break;
                case Quest.SortType.ReadyAscending:
                    Quests = Quests.OrderBy(q => q.Ready()).ThenBy(q => q.NextAvailableTime()).ThenBy(q => q.Flag).ToList();
                    break;
                case Quest.SortType.ReadyDescending:
                    Quests = Quests.OrderByDescending(q => q.NextAvailableTime()).ThenBy(q => q.Flag).ToList();
                    break;
                case Quest.SortType.SolvesAscending:
                    Quests = Quests.OrderBy(q => q.Solves()).ThenBy(q => q.Flag).ToList();
                    break;
                case Quest.SortType.SolvesDescending:
                    Quests = Quests.OrderByDescending(q => q.Solves()).ThenBy(q => q.Flag).ToList();
                    break;
            }
        }

        private static Dictionary<string, int> MapColumns(string headerLine)
        {
            var columns = new Dictionary<string, int>();
            string[] headers = Util.CsvParseLine(headerLine);

            for (int i = 0; i < headers.Length; i++)
            {
                string key = NormalizeHeader(headers[i]);
                if (key.Length > 0 && !columns.ContainsKey(key)) columns[key] = i;
            }

            return columns;
        }

        private static int VerificationColumn(Dictionary<string, int> columns, string server)
        {
            string serverColumn = "verified" + NormalizeHeader(server);
            if (columns.TryGetValue(serverColumn, out int index)) return index;
            return columns.TryGetValue("verified", out index) ? index : -1;
        }

        private static string NormalizeHeader(string header)
        {
            var normalized = new StringBuilder();
            foreach (char c in header ?? "")
            {
                if (char.IsLetterOrDigit(c)) normalized.Append(char.ToLowerInvariant(c));
            }
            return normalized.ToString();
        }

        private static int ColumnIndex(Dictionary<string, int> columns, string[] names)
        {
            foreach (string name in names)
            {
                if (columns.TryGetValue(name, out int index)) return index;
            }
            return -1;
        }

        private static string Field(string[] fields, int index) =>
            index >= 0 && index < fields.Length ? fields[index].Trim() : "";

        private static bool IsTrue(string value)
        {
            switch (value.ToLowerInvariant())
            {
                case "true":
                case "yes":
                case "y":
                case "1":
                case "x":
                case "verified":
                    return true;
                default:
                    return false;
            }
        }

        private static StreamReader OpenCsv()
        {
            if (openBundledOnce)
            {
                openBundledOnce = false;
                UsingBundledList = true;
                return OpenEmbeddedCsv();
            }

            try
            {
                QuestDataFile.Recover(FilePath);
                if (!File.Exists(FilePath))
                {
                    using (var embedded = OpenEmbeddedCsv())
                        QuestDataFile.Write(FilePath, ReadLines(embedded));
                }

                string[] lines = File.ReadAllLines(FilePath);
                if (Validate(lines, out string error))
                {
                    UsingBundledList = false;
                    return new StreamReader(FilePath);
                }

                Util.Log(new InvalidDataException("Local quests.csv is invalid: " + error));
            }
            catch (Exception ex) { Util.Log(ex); }

            UsingBundledList = true;
            return OpenEmbeddedCsv();
        }

        public static string SourceDescription()
        {
            string source = UsingBundledList
                ? "bundled Resources/quests.csv"
                : "live filesystem quests.csv";
            string lastChecked = QuestCatalogUpdater.LastChecked();

            return lastChecked.Length > 0
                ? $"Quest list source: {source}. Last checked: {lastChecked}."
                : $"Quest list source: {source}. Never checked for updates.";
        }

        internal static bool Validate(IEnumerable<string> lines, out string error)
        {
            string[] all = lines?.ToArray() ?? Array.Empty<string>();
            if (all.Length == 0)
            {
                error = "the file is empty";
                return false;
            }

            Dictionary<string, int> columns = MapColumns(all[0]);
            int flagCol = ColumnIndex(columns, FlagNames);
            if (flagCol < 0)
            {
                error = "the quest flag column is missing";
                return false;
            }
            if (ColumnIndex(columns, NameNames) < 0)
            {
                error = "the quest name column is missing";
                return false;
            }
            if (ColumnIndex(columns, RepeatNames) < 0)
            {
                error = "the repeatable column is missing";
                return false;
            }

            var flags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string line in all.Skip(1))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                string flag = Field(Util.CsvParseLine(line), flagCol);
                if (flag.Length == 0)
                {
                    error = "a row has no quest flag";
                    return false;
                }
                if (!flags.Add(flag))
                {
                    error = "duplicate quest flag: " + flag;
                    return false;
                }
            }

            if (flags.Count == 0)
            {
                error = "the file has no quests";
                return false;
            }

            error = "";
            return true;
        }

        private static IEnumerable<string> ReadLines(TextReader reader)
        {
            string line;
            while ((line = reader.ReadLine()) != null) yield return line;
        }

        private static StreamReader OpenEmbeddedCsv()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            string resourceName = assembly.GetManifestResourceNames().FirstOrDefault(
                name => name.EndsWith(".Resources.quests.csv", StringComparison.OrdinalIgnoreCase));
            if (resourceName == null)
                throw new FileNotFoundException("Embedded resource quests.csv not found.");

            return new StreamReader(assembly.GetManifestResourceStream(resourceName));
        }
    }
}
