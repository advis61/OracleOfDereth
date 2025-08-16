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
            new JohnQuest { Name = "Apostate Finale", QuestFlag = "c", BitMask = 1 },
            new JohnQuest { Name = "Bloodstone Investigation", QuestFlag = "a", BitMask = 16384 },
            new JohnQuest { Name = "Count Phainor's Amulet", QuestFlag = "b", BitMask = 8192 },
            new JohnQuest { Name = "Deewain's Dark Cavern", QuestFlag = "a", BitMask = 32768 },
            new JohnQuest { Name = "Defeating the Curator of Torment", QuestFlag = "a", BitMask = 256 },
            new JohnQuest { Name = "Dream Reaver Investigation", QuestFlag = "b", BitMask = 1 },
            new JohnQuest { Name = "Empyrean Rescue", QuestFlag = "a", BitMask = 65536 },
            new JohnQuest { Name = "End of Days", QuestFlag = "a", BitMask = 8 },
            new JohnQuest { Name = "Fear Factory", QuestFlag = "a", BitMask = 16 },
            new JohnQuest { Name = "First Sister (Harvesting the Bulb of Mornings)", QuestFlag = "b", BitMask = 256 },
            new JohnQuest { Name = "Foundry of Izexi", QuestFlag = "b", BitMask = 131072 },
            new JohnQuest { Name = "Four Corners of Dereth", QuestFlag = "a", BitMask = 4 },
            new JohnQuest { Name = "Geraine's Hosts", QuestFlag = "b", BitMask = 524288 },
            new JohnQuest { Name = "Geraine's Study (Mhoire Infiltration)", QuestFlag = "a", BitMask = 8192 },
            new JohnQuest { Name = "Gurog Creation", QuestFlag = "a", BitMask = 64 },
            new JohnQuest { Name = "Halt Dericost Ritual", QuestFlag = "a", BitMask = 2048 },
            new JohnQuest { Name = "Hive Queen Assault", QuestFlag = "a", BitMask = 2 },
            new JohnQuest { Name = "Hoshino Fortress Infiltration", QuestFlag = "a", BitMask = 512 },
            new JohnQuest { Name = "Hoshino Must Die (Defeat Hoshino Kei)", QuestFlag = "a", BitMask = 1 },
            new JohnQuest { Name = "Janthef's Release", QuestFlag = "b", BitMask = 2 },
            new JohnQuest { Name = "Liberation of Uziz", QuestFlag = "a", BitMask = 524288 },
            new JohnQuest { Name = "Lost Lore", QuestFlag = "b", BitMask = 32 },
            new JohnQuest { Name = "Lugian Assault", QuestFlag = "a", BitMask = 32 },
            new JohnQuest { Name = "Mhoire Castle (Castle of Lord Mhoire)", QuestFlag = "a", BitMask = 262144 },
            new JohnQuest { Name = "Nanjou Stockade", QuestFlag = "b", BitMask = 2048 },
            new JohnQuest { Name = "Ninja Academy", QuestFlag = "b", BitMask = 262144 },
            new JohnQuest { Name = "Oubliette of Mhoire Castle", QuestFlag = "a", BitMask = 1024 },
            new JohnQuest { Name = "Purging the Corruption", QuestFlag = "b", BitMask = 32768 },
            new JohnQuest { Name = "Releasing the Light", QuestFlag = "b", BitMask = 128 },
            new JohnQuest { Name = "Rescuing Mouf P", QuestFlag = "b", BitMask = 8 },
            new JohnQuest { Name = "Rynthid Foothold", QuestFlag = "a", BitMask = 131072 },
            new JohnQuest { Name = "Rynthid Foundry", QuestFlag = "b", BitMask = 64 },
            new JohnQuest { Name = "Rynthid Training", QuestFlag = "a", BitMask = 128 },
            new JohnQuest { Name = "Save Karul", QuestFlag = "b", BitMask = 16384 },
            new JohnQuest { Name = "Second Sister (Harvesting the Bulb of Harvests)", QuestFlag = "b", BitMask = 512 },
            new JohnQuest { Name = "Seed of Power", QuestFlag = "b", BitMask = 65536 },
            new JohnQuest { Name = "Serpent Burial Grounds", QuestFlag = "b", BitMask = 16 },
            new JohnQuest { Name = "Shroud of Emotion", QuestFlag = "b", BitMask = 4 },
            new JohnQuest { Name = "Slave Master", QuestFlag = "a", BitMask = 4096 },
            new JohnQuest { Name = "Tanada Intercept and Slaughter", QuestFlag = "b", BitMask = 4096 },
            new JohnQuest { Name = "Third Sister (Harvesting the Bulb of Twilight)", QuestFlag = "b", BitMask = 1024 }
        };

        // Collection of Quest Flags data objects
        public static Dictionary<string, QuestFlag> QuestFlags = new Dictionary<string, QuestFlag>();

        // Properties
        public string Name = "";
        public string QuestFlag = "";
        public int BitMask = 0;

        public new string ToString()
        {
            return $"{Name}: {QuestFlag} BitMask:{BitMask}";
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

