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
    public class SocietyQuest
    {
        // Collection of SocietyQuests loaded from society.csv
        public static List<SocietyQuest> SocietyQuests = new List<SocietyQuest>();

        // Properties
        public string Name = "";
        public string Flag = "";
        public string Area = "";
        public string Url = "";
        public string Hint = "";

        public static void Init()
        {
            SocietyQuests.Clear();
            LoadSocietyQuestsCSV();
        }

        public static void LoadSocietyQuestsCSV()
        {
            var quests = new List<SocietyQuest>();

            var assembly = Assembly.GetExecutingAssembly();

            string resourceName = assembly.GetManifestResourceNames().FirstOrDefault(n => n.EndsWith("society.csv", StringComparison.OrdinalIgnoreCase));
            if (resourceName == null) throw new FileNotFoundException("Embedded resource society.csv not found.");

            using (var stream = assembly.GetManifestResourceStream(resourceName))
            using (var reader = new StreamReader(stream))
            {
                string headerLine = reader.ReadLine();
                if (headerLine == null) throw new InvalidDataException("CSV file is empty.");

                // Assume columns: Name,QuestFlag,Area,Url,Hint
                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine();
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    var fields = line.Split(',');

                    quests.Add(new SocietyQuest
                    {
                        Name = fields[0].Trim(),
                        Flag = fields[1].Trim().ToLower(),
                        Area = fields[2].Trim(),
                        Url = fields[3].Trim(),
                        Hint = fields[4].Trim()
                    });
                }
            }

            SocietyQuests.AddRange(quests);

            //Util.Chat($"Loaded {SocietyQuests.Count} Society Quests from embedded CSV.", 1);
        }

        public override string ToString()
        {
            return $"{Name}: {Flag}";
        }

        // Row types
        public bool IsBlank()
        {
            return Name == "Blank";
        }

        public bool IsHeader()
        {
            return Name.StartsWith("Rank:");
        }

        public bool IsQuest()
        {
            return !IsBlank() && !IsHeader();
        }

        // Header rows: whether the player has reached this rank
        public bool RankReached()
        {
            string rankName = Name.Replace("Rank:", "").Trim();
            return Society.HasReachedRank(rankName);
        }

        // Quest rows (repeatable)
        public bool IsComplete()
        {
            return !Ready();
        }

        public DateTime? CompletedOn()
        {
            QuestFlag.QuestFlags.TryGetValue(Flag, out QuestFlag questFlag);
            if (questFlag == null) { return null; }

            return questFlag.CompletedOn;
        }

        public TimeSpan? NextAvailableTime()
        {
            QuestFlag.QuestFlags.TryGetValue(Flag, out QuestFlag questFlag);
            if (questFlag == null) { return null; }

            return questFlag.NextAvailableTime();
        }

        public bool Ready()
        {
            QuestFlag.QuestFlags.TryGetValue(Flag, out QuestFlag questFlag);
            if (questFlag == null) { return true; }

            return questFlag.Ready();
        }

        public int Solves()
        {
            QuestFlag.QuestFlags.TryGetValue(Flag, out QuestFlag questFlag);
            if (questFlag == null) { return 0; }

            return questFlag.Solves;
        }
    }
}
