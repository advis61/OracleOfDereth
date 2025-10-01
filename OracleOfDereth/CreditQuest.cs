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
    public class CreditQuest
    {
        // Collection of CreditQuests loaded from creditquests.csv
        public static List<CreditQuest> CreditQuests = new List<CreditQuest>();

        // Properties
        public string Name = "";
        public string Flag = "";
        public int TimesTotal = 0;
        public string Url = "";
        public string Hint = "";

        public static void Init()
        {
            CreditQuests.Clear();
            LoadCreditQuestsCSV();
        }

        public static void LoadCreditQuestsCSV()
        {
            var quests = new List<CreditQuest>();

            var assembly = Assembly.GetExecutingAssembly();

            string resourceName = assembly.GetManifestResourceNames().FirstOrDefault(n => n.EndsWith("creditquests.csv", StringComparison.OrdinalIgnoreCase));
            if (resourceName == null) throw new FileNotFoundException("Embedded resource Creditquests.csv not found.");

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

                    quests.Add(new CreditQuest
                    {
                        Name = fields[0].Trim(),
                        Flag = fields[1].Trim().ToLower(),
                        TimesTotal = int.Parse(fields[2].Trim()),
                        Url = fields[3].Trim(),
                        Hint = fields[4].Trim()
                    });
                }
            }

            CreditQuests.AddRange(quests);

            // Util.Chat($"Loaded {CreditQuests.Count} Credit Quests from embedded CSV.", 1);
        }

        public new string ToString()
        {
            return $"{Name}: {Flag}";
        }

        public bool IsComplete()
        {
            QuestFlag.QuestFlags.TryGetValue(Flag, out QuestFlag questFlag);
            if(questFlag == null) { return false; }

            return questFlag.Solves >= TimesTotal;
        }
    }
}


