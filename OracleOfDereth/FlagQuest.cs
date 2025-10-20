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
    public class FlagQuest
    {
        // Collection of FlagQuests loaded from Flagquests.csv
        public static List<FlagQuest> FlagQuests = new List<FlagQuest>();

        // Properties
        public string Name = "";
        public string Flag = "";
        public string Flag2 = "";
        public string Url = "";
        public string Hint = "";

        public static void Init()
        {
            FlagQuests.Clear();
            LoadFlagQuestsCSV();
        }

        public static void LoadFlagQuestsCSV()
        {
            var quests = new List<FlagQuest>();

            var assembly = Assembly.GetExecutingAssembly();

            string resourceName = assembly.GetManifestResourceNames().FirstOrDefault(n => n.EndsWith("flagquests.csv", StringComparison.OrdinalIgnoreCase));
            if (resourceName == null) throw new FileNotFoundException("Embedded resource flagquests.csv not found.");

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

                    quests.Add(new FlagQuest
                    {
                        Name = fields[0].Trim(),
                        Flag = fields[1].Trim().ToLower(),
                        Flag2 = fields[2].Trim().ToLower(),
                        Url = fields[3].Trim(),
                        Hint = fields[4].Trim()
                    });
                }
            }

            FlagQuests.AddRange(quests);

            // Util.Chat($"Loaded {FlagQuests.Count} Flag Quests from embedded CSV.", 1);
        }

        public new string ToString()
        {
            return $"{Name}: {Flag}";
        }

        public bool IsComplete()
        {
            QuestFlag.QuestFlags.TryGetValue(Flag, out QuestFlag questFlag);
            if (questFlag == null) { return false; }

            if(Flag2.Length > 0) {
                QuestFlag.QuestFlags.TryGetValue(Flag2, out QuestFlag questFlag2);
                if (questFlag2 == null) { return false; }
            }

            return true;
        }
    }
}



