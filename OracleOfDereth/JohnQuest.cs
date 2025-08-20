using Decal.Adapter;
using Decal.Adapter.Wrappers;
using Decal.Filters;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace OracleOfDereth
{
    public class JohnQuest
    {
        public static readonly int IconComplete = 0x60011F9;   // Green Circle
        public static readonly int IconNotComplete = 0x60011F8;    // Red Circle

        // Collection of JohnQuests loaded from quests.csv
        public static List<JohnQuest> JohnQuests = new List<JohnQuest>();
        public static SortType CurrentSortType = SortType.NameAscending;

        public enum SortType
        {
            NameAscending,
            NameDescending,
            ReadyAscending,
            ReadyDescending,
            SolvesAscending,
            SolvesDescending,
        }

        // Properties
        public string Name = "";
        public int BitMask = 0;
        public string LegendaryQuestsFlag = "";
        public string Flag = "";
        public string Url = "";
        public string Hint = "";

        public static void Init()
        {
            JohnQuests.Clear();
            LoadJohnQuestsCSV();
        }

        public static void LoadJohnQuestsCSV()
        {
            var quests = new List<JohnQuest>();

            var assembly = Assembly.GetExecutingAssembly();

            string resourceName = assembly.GetManifestResourceNames().FirstOrDefault(n => n.EndsWith("quests.csv", StringComparison.OrdinalIgnoreCase));
            if (resourceName == null) throw new FileNotFoundException("Embedded resource quests.csv not found.");

            using (var stream = assembly.GetManifestResourceStream(resourceName))
            using (var reader = new StreamReader(stream))
            {
                string headerLine = reader.ReadLine();
                if (headerLine == null) throw new InvalidDataException("CSV file is empty.");

                // Assume columns: Name,BitMask,LegendaryQuestsFlag,QuestFlag,Url,Hint
                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine();
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    var fields = line.Split(',');

                    quests.Add(new JohnQuest
                    {
                        Name = fields[0].Trim(),
                        BitMask = int.Parse(fields[1].Trim()),
                        LegendaryQuestsFlag = fields[2].Trim().ToLower(),
                        Flag = fields[3].Trim().ToLower(),
                        Url = fields[4].Trim(),
                        Hint = fields[5].Trim()
                    });
                }
            }

            JohnQuests.AddRange(quests);

            //Util.Chat($"Loaded {Quests.Count} John Quests from embedded CSV.", 1);
        }

        public static void Sort(SortType sortType)
        {
            CurrentSortType = sortType;
            switch (sortType)
            {
                case SortType.NameAscending:
                    JohnQuests = JohnQuests.OrderBy(q => q.Name).ToList();
                    break;
                case SortType.NameDescending:
                    JohnQuests = JohnQuests.OrderByDescending(q => q.Name).ToList();
                    break;
                case SortType.ReadyAscending:
                    JohnQuests = JohnQuests.OrderBy(q => q.NextAvailableTime()).ThenBy(q => q.Name).ToList();
                    break;
                case SortType.ReadyDescending:
                    JohnQuests = JohnQuests.OrderByDescending(q => q.NextAvailableTime()).ThenBy(q => q.Name).ToList();
                    break;
                case SortType.SolvesAscending:
                    JohnQuests = JohnQuests.OrderBy(q => q.Solves()).ThenBy(q => q.Name).ToList();
                    break;
                case SortType.SolvesDescending:
                    JohnQuests = JohnQuests.OrderByDescending(q => q.Solves()).ThenBy(q => q.Name).ToList();
                    break;
            }
        }

        public new string ToString()
        {
            return $"{Name}: {Flag} BitMask:{BitMask}";
        }

        public bool IsComplete()
        {
            QuestFlag questFlag;
            QuestFlag.QuestFlags.TryGetValue(LegendaryQuestsFlag, out questFlag);

            if (questFlag == null) { return false; }

            // Check if the BitMask is set in solves
            return (questFlag.Solves & BitMask) == BitMask;
        }

        public DateTime? CompletedOn()
        {
            QuestFlag questFlag;
            QuestFlag.QuestFlags.TryGetValue(Flag, out questFlag);

            if (questFlag == null) { return null; }

            return questFlag.CompletedOn;
        }
        public TimeSpan? NextAvailableTime()
        {
            QuestFlag questFlag;
            QuestFlag.QuestFlags.TryGetValue(Flag, out questFlag);

            if (questFlag == null) { return null; }

            return questFlag.NextAvailableTime();
        }

        public int Solves()
        {
            QuestFlag questFlag;
            QuestFlag.QuestFlags.TryGetValue(Flag, out questFlag);

            if (questFlag == null) { return 0; }

            return questFlag.Solves;
        }
    }
}

