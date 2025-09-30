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
    public class AugQuest
    {
        // Collection of AugQuests loaded from quests.csv
        public static List<AugQuest> AugQuests = new List<AugQuest>();

        // Properties
        public string Name = "";
        public string Flag = "";
        public string Url = "";
        public string Hint = "";

        public static void Init()
        {
            AugQuests.Clear();
            LoadAugQuestsCSV();
        }

        public static void LoadAugQuestsCSV()
        {
            var quests = new List<AugQuest>();

            var assembly = Assembly.GetExecutingAssembly();

            string resourceName = assembly.GetManifestResourceNames().FirstOrDefault(n => n.EndsWith("augquests.csv", StringComparison.OrdinalIgnoreCase));
            if (resourceName == null) throw new FileNotFoundException("Embedded resource augquests.csv not found.");

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

                    quests.Add(new AugQuest
                    {
                        Name = fields[0].Trim(),
                        Flag = fields[1].Trim().ToLower(),
                        Url = fields[2].Trim(),
                        Hint = fields[3].Trim()
                    });
                }
            }

            AugQuests.AddRange(quests);

            //Util.Chat($"Loaded {Quests.Count} Aug Quests from embedded CSV.", 1);
        }

        public new string ToString()
        {
            return $"{Name}: {Flag}";
        }

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


