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
            new JohnQuest { 
                Name = "Apostate Finale", 
                Flag = "apostatefinalemaskshardpickup", 
                LegendaryQuestsFlag = "legendaryquestsc", 
                BitMask = 1 
            },
            new JohnQuest {
                Name = "Bloodstone Investigation",
                Flag = "",
                LegendaryQuestsFlag = "legendaryquestsa",
                BitMask = 16384
            },
            new JohnQuest {
                Name = "Count Phainor's Amulet",
                Flag = "",
                LegendaryQuestsFlag = "legendaryquestsb",
                BitMask = 8192
            },
            new JohnQuest {
                Name = "Deewain's Dark Cavern",
                Flag = "deewaincompleted0211",
                LegendaryQuestsFlag = "legendaryquestsa",
                BitMask = 32768
            },
            new JohnQuest {
                Name = "Defeating the Curator of Torment",
                Flag = "rtwcompleted_1013",
                LegendaryQuestsFlag = "legendaryquestsa",
                BitMask = 256
            },
            new JohnQuest {
                Name = "Dream Reaver Investigation",
                Flag = "",
                LegendaryQuestsFlag = "legendaryquestsb",
                BitMask = 1
            },
            new JohnQuest {
                Name = "Empyrean Rescue",
                Flag = "",
                LegendaryQuestsFlag = "legendaryquestsa",
                BitMask = 65536
            },
            new JohnQuest {
                Name = "End of Days",
                Flag = "",
                LegendaryQuestsFlag = "legendaryquestsa",
                BitMask = 8
            },
            new JohnQuest {
                Name = "Fear Factory",
                Flag = "fearfactorycompleted_0813",
                LegendaryQuestsFlag = "legendaryquestsa",
                BitMask = 16
            },
            new JohnQuest {
                Name = "First Sister (Harvesting the Bulb of Mornings)",
                Flag = "firstsistercompleted_1012b",
                LegendaryQuestsFlag = "legendaryquestsb",
                BitMask = 256
            },
            new JohnQuest {
                Name = "Foundry of Izexi",
                Flag = "foundryofizexicompleted_1212",
                LegendaryQuestsFlag = "legendaryquestsb",
                BitMask = 131072
            },
            new JohnQuest {
                Name = "Four Corners of Dereth",
                Flag = "fourcornerscompleted_1113",
                LegendaryQuestsFlag = "legendaryquestsa",
                BitMask = 4
            },
            new JohnQuest {
                Name = "Geraine's Hosts",
                Flag = "",
                LegendaryQuestsFlag = "legendaryquestsb",
                BitMask = 524288
            },
            new JohnQuest {
                Name = "Geraine's Study (Mhoire Infiltration)",
                Flag = "",
                LegendaryQuestsFlag = "legendaryquestsa",
                BitMask = 8192
            },
            new JohnQuest {
                Name = "Gurog Creation",
                Flag = "gurogcreationcompleted_1110",
                LegendaryQuestsFlag = "legendaryquestsa",
                BitMask = 64
            },
            new JohnQuest {
                Name = "Halt Dericost Ritual",
                Flag = "",
                LegendaryQuestsFlag = "legendaryquestsa",
                BitMask = 2048
            },
            new JohnQuest {
                Name = "Hive Queen Assault",
                Flag = "",
                LegendaryQuestsFlag = "legendaryquestsa",
                BitMask = 2
            },
            new JohnQuest {
                Name = "Hoshino Fortress Infiltration",
                Flag = "",
                LegendaryQuestsFlag = "legendaryquestsa",
                BitMask = 512
            },
            new JohnQuest {
                Name = "Hoshino Must Die (Defeat Hoshino Kei)",
                Flag = "",
                LegendaryQuestsFlag = "legendaryquestsa",
                BitMask = 1
            },
            new JohnQuest {
                Name = "Janthef's Release",
                Flag = "",
                LegendaryQuestsFlag = "legendaryquestsb",
                BitMask = 2
            },
            new JohnQuest {
                Name = "Liberation of Uziz",
                Flag = "",
                LegendaryQuestsFlag = "legendaryquestsa",
                BitMask = 524288
            },
            new JohnQuest {
                Name = "Lost Lore",
                Flag = "",
                LegendaryQuestsFlag = "legendaryquestsb",
                BitMask = 32
            },
            new JohnQuest {
                Name = "Lugian Assault",
                Flag = "lugianassaultcompleted_0913",
                LegendaryQuestsFlag = "legendaryquestsa",
                BitMask = 32
            },
            new JohnQuest {
                Name = "Mhoire Castle (Castle of Lord Mhoire)",
                Flag = "",
                LegendaryQuestsFlag = "legendaryquestsa",
                BitMask = 262144
            },
            new JohnQuest {
                Name = "Nanjou Stockade",
                Flag = "",
                LegendaryQuestsFlag = "legendaryquestsb",
                BitMask = 2048
            },
            new JohnQuest {
                Name = "Ninja Academy",
                Flag = "ninjaacademyswordpickup",
                LegendaryQuestsFlag = "legendaryquestsb",
                BitMask = 262144
            },
            new JohnQuest {
                Name = "Oubliette of Mhoire Castle",
                Flag = "pickeduptokenoublietteboss_0112",
                LegendaryQuestsFlag = "legendaryquestsa",
                BitMask = 1024
            },
            new JohnQuest {
                Name = "Purging the Corruption",
                Flag = "",
                LegendaryQuestsFlag = "legendaryquestsb",
                BitMask = 32768
            },
            new JohnQuest {
                Name = "Releasing the Light",
                Flag = "",
                LegendaryQuestsFlag = "legendaryquestsb",
                BitMask = 128
            },
            new JohnQuest {
                Name = "Rescuing Mouf P",
                Flag = "moufreward",
                LegendaryQuestsFlag = "legendaryquestsb",
                BitMask = 8
            },
            new JohnQuest {
                Name = "Rynthid Foothold",
                Flag = "",
                LegendaryQuestsFlag = "legendaryquestsa",
                BitMask = 131072
            },
            new JohnQuest {
                Name = "Rynthid Foundry",
                Flag = "",
                LegendaryQuestsFlag = "legendaryquestsb",
                BitMask = 64
            },
            new JohnQuest {
                Name = "Rynthid Training",
                Flag = "rynthidtrainingcompleted_1013",
                LegendaryQuestsFlag = "legendaryquestsa",
                BitMask = 128
            },
            new JohnQuest {
                Name = "Save Karul",
                Flag = "",
                LegendaryQuestsFlag = "legendaryquestsb",
                BitMask = 16384
            },
            new JohnQuest {
                Name = "Second Sister (Harvesting the Bulb of Harvests)",
                Flag = "secondsistercompleted_1112",
                LegendaryQuestsFlag = "legendaryquestsb",
                BitMask = 512
            },
            new JohnQuest {
                Name = "Seed of Power",
                Flag = "",
                LegendaryQuestsFlag = "legendaryquestsb",
                BitMask = 65536
            },
            new JohnQuest {
                Name = "Serpent Burial Grounds",
                Flag = "serpentburialgroundsdone",
                LegendaryQuestsFlag = "legendaryquestsb",
                BitMask = 16
            },
            new JohnQuest {
                Name = "Shroud of Emotion",
                Flag = "",
                LegendaryQuestsFlag = "legendaryquestsb",
                BitMask = 4
            },
            new JohnQuest {
                Name = "Slave Master",
                Flag = "",
                LegendaryQuestsFlag = "legendaryquestsa",
                BitMask = 4096
            },
            new JohnQuest {
                Name = "Tanada Intercept and Slaughter",
                Flag = "",
                LegendaryQuestsFlag = "legendaryquestsb",
                BitMask = 4096
            },
            new JohnQuest {
                Name = "Third Sister (Harvesting the Bulb of Twilight)",
                Flag = "thirdsistercompleted_1212",
                LegendaryQuestsFlag = "legendaryquestsb",
                BitMask = 1024
            }
        };

        public static readonly int IconComplete = 0x60011F9;   // Green Circle
        public static readonly int IconNotComplete = 0x60011F8;    // Red Circle

        //public static readonly int IconCompleted22 = 0x60020B5;  // Cicle (Supposed to represent a question mark, a backwards one I guess...)
        //public static readonly int IconNone = 0x600287A;	// Small Grayish Dot

        // Properties
        public string Name = "";
        public string Flag = "";
        public string LegendaryQuestsFlag = "";
        public int BitMask = 0;

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
    }
}

