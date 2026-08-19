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

        private static readonly string[] FlagNames = { "questflag", "flag" };
        private static readonly string[] ServerNames = { "server", "world" };
        private static readonly string[] VerifiedNames = { "verifiedconquest", "verified" };
        private static readonly string[] NameNames = { "quest", "questname", "name", "title" };
        private static readonly string[] UrlNames = { "url", "link", "wiki" };
        private static readonly string[] InfoNames = { "info", "notes", "description" };
        private static readonly string[] HintNames = { "hint", "hints", "directions", "walkthrough" };
        private static readonly string[] RepeatNames = { "repeatable", "repeat" };

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
                int verifiedCol = ColumnIndex(columns, VerifiedNames);
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
                        VerifiedConquest = IsTrue(Field(fields, verifiedCol)),
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
            Assembly assembly = Assembly.GetExecutingAssembly();
            string resourceName = assembly.GetManifestResourceNames().FirstOrDefault(
                name => name.EndsWith(".Resources.quests.csv", StringComparison.OrdinalIgnoreCase));
            if (resourceName == null)
                throw new FileNotFoundException("Embedded resource quests.csv not found.");

            return new StreamReader(assembly.GetManifestResourceStream(resourceName));
        }
    }
}
