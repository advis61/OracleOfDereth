using Decal.Adapter;
using Decal.Adapter.Wrappers;
using Decal.Filters;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using VirindiViewService;

namespace OracleOfDereth
{
    public class Augmentation
    {
        // Collection of Augmentations loaded from augmentations.csv
        public static List<Augmentation> Augmentations = new List<Augmentation>();

        // Tracking special augmentation IDs
        public static readonly List<int> InateAttributeIds = new List<int>() { 218, 219, 220, 221, 222, 223 };
        public static readonly List<int> InateResistanceIds = new List<int>() { 240, 241, 242, 243, 244, 245, 246 };
        public static readonly List<int> LuminanceSpecializationIds = new List<int>() { -333, -334, -335, -336 };
        public static readonly List<int> LuminanceSeerIds = new List<int>() { -4, -5, -6, -7, -8 };

        // Properties
        public string Name = "";
        public string Category = "";
        public int Id = 0;
        public string Effect = "";
        public string Cost = "";
        public int TimesTotal = 0;
        public string Flag = "";
        public string Url = "";
        public string Hint = "";

        public static void Init()
        {
            Augmentations.Clear();
            LoadAugmentationsCSV();
        }

        public static void LoadAugmentationsCSV()
        {
            var augmentations = new List<Augmentation>();

            var assembly = Assembly.GetExecutingAssembly();

            string resourceName = assembly.GetManifestResourceNames().FirstOrDefault(n => n.EndsWith("augmentations.csv", StringComparison.OrdinalIgnoreCase));
            if (resourceName == null) throw new FileNotFoundException("Embedded resource augmentations.csv not found.");

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

                    augmentations.Add(new Augmentation
                    {
                        Name = fields[0].Trim(),
                        Category = fields[1].Trim(),
                        Id = int.TryParse(fields[2].Trim(), out int id) ? id : 0,
                        Effect = fields[3].Trim(),
                        Cost = fields[4].Trim(),
                        TimesTotal = int.TryParse(fields[5].Trim(), out int times) ? times : 0,
                        Flag = fields[6].Trim().ToLower(),
                        Url = fields[7].Trim(),
                        Hint = fields[8].Trim()
                    });
                }
            }

            Augmentations.AddRange(augmentations);

            // Util.Chat($"Loaded {Augmentations.Count} Augmentations from embedded CSV.", 1);
        }

        public static List<Augmentation> XPAugmentations() { return Augmentations.Where(a => a.Category == "XP").ToList(); }
        public static List<Augmentation> LuminanceAugmentations() { return Augmentations.Where(a => a.Category == "Luminance").ToList(); }

        public static int TotalLuminanceSpent()
        {
            return LuminanceAugmentations().Sum(x => x.LuminanceSpent());
        }

        public static int TotalLuminance()
        {
            return 19_000_000;
        }

        public static int TotalLuminanceRemaining()
        {
            return TotalLuminance() - TotalLuminanceSpent();
        }
        public static int TotalLuminancePercentage()
        {
            return (int)(TotalLuminanceSpent() / (float)TotalLuminance() * 100);
        }

        public new string ToString()
        {
            return $"{Name}";
        }
        public string CostText()
        {
            if(IsXP()) { return Cost; }

            if (Id == 0) { return ""; }
            if (IsLuminanceSeer()) { return ""; }
            if (Times() >= TimesTotal) { return ""; }
            if (Flag.Length > 0 && !IsQuestComplete()) { return ""; }

            if (Id == 365) // World
            {
                return (100 + (Times() * 100)).ToString() + "k";
            }

            if (Id == 344 || IsLuminanceSpecialization()) // Specialization
            {
                return (350 + (Times() * 50)).ToString() + "k";
            }

            return (100 + (Times() * 50)).ToString() + "k";
        }

        public int LuminanceSpent()
        {
            if(Times() == 0) { return 0; }

            // 1 = 100
            // 2 = 100 + 200 = 300
            // 3 = 100 + 200 + 300 = 600
            // 4 = 100 + 200 + 300 + 400 = 1000

            if (Id == 365)  // World
            {
                return (100_000 * Times() * (Times() + 1) / 2);
            }
            else if (Id == 344 || IsLuminanceSpecialization()) // Specialization
            { 
                return (Times() * (700_000 + (Times() - 1) * 50_000) / 2);
            }
            else
            {
                return (Times() * (200_000 + (Times() - 1) * 50_000) / 2);
            }
        }

        private bool IsInateAttributes() { return Id == -1; }
        private bool IsInateResistances() { return Id == -2; }
        private bool IsAsheronsBenediction() { return Id == -3; }
        private bool IsLuminanceSeer() { return LuminanceSeerIds.Contains(Id); }
        private bool IsInateAttribute() { return InateAttributeIds.Contains(Id); }
        private bool IsInateResistance() { return InateResistanceIds.Contains(Id); }
        private bool IsLuminanceSpecialization() { return LuminanceSpecializationIds.Contains(Id); }
        private int InateAttributesTimes() { return InateAttributeIds.Sum(id => CoreManager.Current.CharacterFilter.GetCharProperty(id)); }
        private int InateResistancesTimes() { return InateResistanceIds.Sum(id => CoreManager.Current.CharacterFilter.GetCharProperty(id)); }
        private int LuminanceSpecializationTimes() { return Math.Max(CoreManager.Current.CharacterFilter.GetCharProperty(Math.Abs(Id)) - 5, 0); }
        private int AsheronsBenedictionTimes() { return CoreManager.Current.WorldFilter.GetByNameSubstring("Asheron's Lesser Benediction").ToList().Count(); }

        public int Times()
        {
            if (Flag.Length > 0 && !IsQuestComplete()) { return 0; }
            if (IsInateAttributes()) { return InateAttributesTimes(); }
            if (IsInateResistances()) { return InateResistancesTimes(); }
            if (IsAsheronsBenediction()) { return AsheronsBenedictionTimes(); }
            if (IsLuminanceSpecialization()) { return LuminanceSpecializationTimes(); }

            int times = CoreManager.Current.CharacterFilter.GetCharProperty(Id);
            return Math.Min(times, TimesTotal);
        }

        public bool IsComplete()
        {
            if(TimesTotal == 0) { return false; }
            if(IsInateAttribute()) { return InateAttributesTimes() >= 10; };
            if(IsInateResistance()) { return InateResistancesTimes() >= 2; };
            if(IsLuminanceSeer()) { return IsQuestComplete(); }

            return Times() >= TimesTotal;
        }

        public string Text()
        {
            if (TimesTotal == 0) { return Times().ToString(); }
            if (IsInateAttribute() || IsInateResistance()) { return Times().ToString(); }
            if (IsLuminanceSeer()) { return ""; }

            return $"{Times()}/{TimesTotal}";
        }

        public bool IsXP()
        {
            return Category == "XP";
        }
        public bool IsLuminance()
        {
            return Category == "Luminance";
        }

        // Luminance Seer and Luminance Specialization require quest completion for seer
        public bool IsQuestComplete()
        {
            if(Flag.Length == 0) { return false; }

            QuestFlag.QuestFlags.TryGetValue(Flag, out QuestFlag questFlag);
            if (questFlag == null) { return false; }

            return questFlag.Solves > 0;
        }
    }
}

