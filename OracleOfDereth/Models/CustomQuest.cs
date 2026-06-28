using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace OracleOfDereth
{
    // Server-specific quests loaded from customquests.csv. Unlike the other quest models the
    // CSV carries a Server column, and only rows matching the current world are kept (mirrors
    // how JohnQuest drops the Levistras-only "Apostate Finale" off-server). Otherwise this is a
    // plain single-flag quest: ready/solves come from the tracked QuestFlag, same as FacilityQuest.
    public class CustomQuest
    {
        // Collection of CustomQuests loaded from customquests.csv (current server only)
        public static List<CustomQuest> CustomQuests = new List<CustomQuest>();

        // Properties
        public string Server = "";
        public string Name = "";
        public string Flag = "";
        private string _url = "";
        public string Url { get => Util.WikiUrl(_url); set => _url = value; }
        public string Hint = "";

        public static void Init()
        {
            CustomQuests.Clear();
            LoadCustomQuestsCSV();
        }

        public static void LoadCustomQuestsCSV()
        {
            var quests = new List<CustomQuest>();

            var assembly = Assembly.GetExecutingAssembly();

            string resourceName = assembly.GetManifestResourceNames().FirstOrDefault(n => n.EndsWith("customquests.csv", StringComparison.OrdinalIgnoreCase));
            if (resourceName == null) throw new FileNotFoundException("Embedded resource customquests.csv not found.");

            using (var stream = assembly.GetManifestResourceStream(resourceName))
            using (var reader = new StreamReader(stream))
            {
                string headerLine = reader.ReadLine();
                if (headerLine == null) throw new InvalidDataException("CSV file is empty.");

                // Assume columns: Server,Name,QuestFlag,Url,Hint
                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine();
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    var fields = line.Split(',');

                    var quest = new CustomQuest
                    {
                        Server = fields[0].Trim(),
                        Name = fields[1].Trim(),
                        Flag = fields[2].Trim().ToLower(),
                        Url = fields[3].Trim(),
                        Hint = fields[4].Trim()
                    };

                    // Only keep quests for the world this character is on. The static Server
                    // helper is qualified because the instance "Server" property shadows it here.
                    if (!string.Equals(quest.Server, OracleOfDereth.Server.Name, StringComparison.OrdinalIgnoreCase)) continue;

                    quests.Add(quest);
                }
            }

            CustomQuests.AddRange(quests);

            // Util.Chat($"Loaded {CustomQuests.Count} Custom Quests from embedded CSV.", 1);
        }

        public override string ToString()
        {
            return $"{Name}: {Flag} ({Server})";
        }

        public bool IsComplete()
        {
            QuestFlag.QuestFlags.TryGetValue(Flag, out QuestFlag questFlag);
            if (questFlag == null) { return false; }

            return questFlag.Solves > 0;
        }
    }
}
