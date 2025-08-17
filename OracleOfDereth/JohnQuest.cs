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
        public static readonly List<JohnQuest> Quests = new List<JohnQuest> {
            new JohnQuest { Name = "Apostate Finale", QuestFlag = "c", LegendaryQuestsFlag = "legendaryquestsc", BitMask = 1 },
            new JohnQuest { Name = "Bloodstone Investigation", QuestFlag = "a", LegendaryQuestsFlag = "legendaryquestsa", BitMask = 16384 },
            new JohnQuest { Name = "Count Phainor's Amulet", QuestFlag = "b", LegendaryQuestsFlag = "legendaryquestsb", BitMask = 8192 },
            new JohnQuest { Name = "Deewain's Dark Cavern", QuestFlag = "a", LegendaryQuestsFlag = "legendaryquestsa", BitMask = 32768 },
            new JohnQuest { Name = "Defeating the Curator of Torment", QuestFlag = "a", LegendaryQuestsFlag = "legendaryquestsa", BitMask = 256 },
            new JohnQuest { Name = "Dream Reaver Investigation", QuestFlag = "b", LegendaryQuestsFlag = "legendaryquestsb", BitMask = 1 },
            new JohnQuest { Name = "Empyrean Rescue", QuestFlag = "a", LegendaryQuestsFlag = "legendaryquestsa", BitMask = 65536 },
            new JohnQuest { Name = "End of Days", QuestFlag = "a", LegendaryQuestsFlag = "legendaryquestsa", BitMask = 8 },
            new JohnQuest { Name = "Fear Factory", QuestFlag = "a", LegendaryQuestsFlag = "legendaryquestsa", BitMask = 16 },
            new JohnQuest { Name = "First Sister (Harvesting the Bulb of Mornings)", QuestFlag = "b", LegendaryQuestsFlag = "legendaryquestsb", BitMask = 256 },
            new JohnQuest { Name = "Foundry of Izexi", QuestFlag = "b", LegendaryQuestsFlag = "legendaryquestsb", BitMask = 131072 },
            new JohnQuest { Name = "Four Corners of Dereth", QuestFlag = "a", LegendaryQuestsFlag = "legendaryquestsa", BitMask = 4 },
            new JohnQuest { Name = "Geraine's Hosts", QuestFlag = "b", LegendaryQuestsFlag = "legendaryquestsb", BitMask = 524288 },
            new JohnQuest { Name = "Geraine's Study (Mhoire Infiltration)", QuestFlag = "a", LegendaryQuestsFlag = "legendaryquestsa", BitMask = 8192 },
            new JohnQuest { Name = "Gurog Creation", QuestFlag = "a", LegendaryQuestsFlag = "legendaryquestsa", BitMask = 64 },
            new JohnQuest { Name = "Halt Dericost Ritual", QuestFlag = "a", LegendaryQuestsFlag = "legendaryquestsa", BitMask = 2048 },
            new JohnQuest { Name = "Hive Queen Assault", QuestFlag = "a", LegendaryQuestsFlag = "legendaryquestsa", BitMask = 2 },
            new JohnQuest { Name = "Hoshino Fortress Infiltration", QuestFlag = "a", LegendaryQuestsFlag = "legendaryquestsa", BitMask = 512 },
            new JohnQuest { Name = "Hoshino Must Die (Defeat Hoshino Kei)", QuestFlag = "a", LegendaryQuestsFlag = "legendaryquestsa", BitMask = 1 },
            new JohnQuest { Name = "Janthef's Release", QuestFlag = "b", LegendaryQuestsFlag = "legendaryquestsb", BitMask = 2 },
            new JohnQuest { Name = "Liberation of Uziz", QuestFlag = "a", LegendaryQuestsFlag = "legendaryquestsa", BitMask = 524288 },
            new JohnQuest { Name = "Lost Lore", QuestFlag = "b", LegendaryQuestsFlag = "legendaryquestsb", BitMask = 32 },
            new JohnQuest { Name = "Lugian Assault", QuestFlag = "a", LegendaryQuestsFlag = "legendaryquestsa", BitMask = 32 },
            new JohnQuest { Name = "Mhoire Castle (Castle of Lord Mhoire)", QuestFlag = "a", LegendaryQuestsFlag = "legendaryquestsa", BitMask = 262144 },
            new JohnQuest { Name = "Nanjou Stockade", QuestFlag = "b", LegendaryQuestsFlag = "legendaryquestsb", BitMask = 2048 },
            new JohnQuest { Name = "Ninja Academy", QuestFlag = "b", LegendaryQuestsFlag = "legendaryquestsb", BitMask = 262144 },
            new JohnQuest { Name = "Oubliette of Mhoire Castle", QuestFlag = "a", LegendaryQuestsFlag = "legendaryquestsa", BitMask = 1024 },
            new JohnQuest { Name = "Purging the Corruption", QuestFlag = "b", LegendaryQuestsFlag = "legendaryquestsb", BitMask = 32768 },
            new JohnQuest { Name = "Releasing the Light", QuestFlag = "b", LegendaryQuestsFlag = "legendaryquestsb", BitMask = 128 },
            new JohnQuest { Name = "Rescuing Mouf P", QuestFlag = "b", LegendaryQuestsFlag = "legendaryquestsb", BitMask = 8 },
            new JohnQuest { Name = "Rynthid Foothold", QuestFlag = "a", LegendaryQuestsFlag = "legendaryquestsa", BitMask = 131072 },
            new JohnQuest { Name = "Rynthid Foundry", QuestFlag = "b", LegendaryQuestsFlag = "legendaryquestsb", BitMask = 64 },
            new JohnQuest { Name = "Rynthid Training", QuestFlag = "a", LegendaryQuestsFlag = "legendaryquestsa", BitMask = 128 },
            new JohnQuest { Name = "Save Karul", QuestFlag = "b", LegendaryQuestsFlag = "legendaryquestsb", BitMask = 16384 },
            new JohnQuest { Name = "Second Sister (Harvesting the Bulb of Harvests)", QuestFlag = "b", LegendaryQuestsFlag = "legendaryquestsb", BitMask = 512 },
            new JohnQuest { Name = "Seed of Power", QuestFlag = "b", LegendaryQuestsFlag = "legendaryquestsb", BitMask = 65536 },
            new JohnQuest { Name = "Serpent Burial Grounds", QuestFlag = "b", LegendaryQuestsFlag = "legendaryquestsb", BitMask = 16 },
            new JohnQuest { Name = "Shroud of Emotion", QuestFlag = "b", LegendaryQuestsFlag = "legendaryquestsb", BitMask = 4 },
            new JohnQuest { Name = "Slave Master", QuestFlag = "a", LegendaryQuestsFlag = "legendaryquestsa", BitMask = 4096 },
            new JohnQuest { Name = "Tanada Intercept and Slaughter", QuestFlag = "b", LegendaryQuestsFlag = "legendaryquestsb", BitMask = 4096 },
            new JohnQuest { Name = "Third Sister (Harvesting the Bulb of Twilight)", QuestFlag = "b", LegendaryQuestsFlag = "legendaryquestsb", BitMask = 1024 }
        };

        public static readonly int IconComplete = 0x60011F9;   // Green Circle
        public static readonly int IconNotComplete = 0x60011F8;    // Red Circle

        public static readonly int IconCompleted22 = 0x60020B5;  // Cicle (Supposed to represent a question mark, a backwards one I guess...)
        public static readonly int IconNone = 0x600287A;	// Small Grayish Dot

        // Collection of Quest Flags data objects
        public static Dictionary<string, QuestFlag> QuestFlags = new Dictionary<string, QuestFlag>();

        // Properties
        public string Name = "";
        public string QuestFlag = "";
        public string LegendaryQuestsFlag = "";
        public int BitMask = 0;

        public new string ToString()
        {
            return $"{Name}: {QuestFlag} BitMask:{BitMask}";
        }

        public bool IsComplete()
        {
            //if (QuestFlag.QuestFlags.TryGetValue("pathwardencomplete", out legendaryQuestsA) && legendaryQuestsA != null)
            return (QuestFlag == "a");
        }


        //public static bool Process(string line)
        //{
        //    QuestFlag questFlag = FromMyQuestsLine(line);
        //    if (questFlag == null) { return false; }

        //    // Store this quest flag in the QuestFlags dictionary
        //    if (QuestFlagsToTrack.Contains(questFlag.Key))
        //    {
        //        QuestFlags[questFlag.Key] = questFlag;
        //        CoreManager.Current.Actions.AddChatText($"Now tracking #{questFlag.ToString()}.#{QuestFlags.Count()} quests tracked total", 1);
        //    }

        //    return true;
        //}
    }
}

