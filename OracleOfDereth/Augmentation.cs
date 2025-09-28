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
        public static readonly int IconComplete = 0x60011F9;   // Green Circle
        public static readonly int IconNotComplete = 0x60011F8;    // Red Circle

        // Collection of Augmentations loaded from augmentations.csv
        public static List<Augmentation> Augmentations = new List<Augmentation>();

        // Properties
        public string Name = "";
        public string Category = "";
        public int Id = 0;
        public string Effect = "";
        public string Cost = "";
        public int TimesTotal = 0;
        public string Npc = "";
        public string Url = "";

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
                        Npc = fields[6].Trim(),
                        Url = fields[7].Trim()
                    });
                }
            }

            Augmentations.AddRange(augmentations);

            Util.Chat($"Loaded {Augmentations.Count} Augmentations from embedded CSV.", 1);
        }

        public static List<Augmentation> XPAugmentations() { return GetByCategory("XP"); }
        public static List<Augmentation> LuminanceAugmentations() { return GetByCategory("Luminance"); }

        public static List<Augmentation> GetByCategory(string category)
        {
            return Augmentations.Where(a => a.Category.Equals(category)).ToList();
        }

        public static Augmentation Get(int id)
        {
            return Augmentations.FirstOrDefault(a => a.Id == id);
        }

        public new string ToString()
        {
            return $"{Name}";
        }
        public string CostText()
        {
            return Cost;
        }
        public int Times()
        {
            return CoreManager.Current.CharacterFilter.GetCharProperty(Id);
        }

        public bool IsComplete()
        {
            return Times() >= TimesTotal;
        }

        public string Text()
        {
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
    }
}

