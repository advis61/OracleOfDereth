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
    public class Cantrip 
    {
        // Collection of Cantrips loaded from cantrips.csv
        public static List<Cantrip> Cantrips = new List<Cantrip>();

        // Properties
        public string Name = "";
        public int SkillId = 0;
        public int SpellId = 0;
        public int Minor = 0;
        public int Moderate = 0;
        public int Major = 0;
        public int Epic = 0;
        public int Legendary = 0;

        public static void Init()
        {
            Cantrips.Clear();
            LoadCantripsCSV();
        }

        public static void LoadCantripsCSV()
        {
            var cantrips = new List<Cantrip>();

            var assembly = Assembly.GetExecutingAssembly();

            string resourceName = assembly.GetManifestResourceNames().FirstOrDefault(n => n.EndsWith("cantrips.csv", StringComparison.OrdinalIgnoreCase));
            if (resourceName == null) throw new FileNotFoundException("Embedded resource cantrips.csv not found.");

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

                    cantrips.Add(new Cantrip
                    {
                        Name = fields[0].Trim(),
                        SkillId = int.Parse(fields[1].Trim()),
                        SpellId = int.Parse(fields[2].Trim()),
                        Minor = int.Parse(fields[3].Trim()),
                        Moderate = int.Parse(fields[4].Trim()),
                        Major = int.Parse(fields[5].Trim()),
                        Epic = int.Parse(fields[6].Trim()),
                        Legendary = int.Parse(fields[7].Trim()),
                    });
                }
            }

            Cantrips.AddRange(cantrips);

            //Util.Chat($"Loaded {Cantrips.Count} Cantrips from embedded CSV.", 1);
        }

        public new string ToString()
        {
            return $"{Name}";
        }

        public bool SkillIsKnown()
        {
            if(SkillId <= 0) { return true; }
            return new Skill((CharFilterSkillType)SkillId).IsKnown();
        }

        public bool IsSetBonus()
        {
            return (SkillId == -1);
        }
        public bool IsSetDedicationBonus()
        {
            return (SkillId == -2);
        }

        public bool IsEssence()
        {
            return (SkillId == -3);
        }

        public bool IsWarriorsVitality()
        { 
            return (SkillId == -4); 
        }

        public bool IsMinor()
        {
            return CoreManager.Current.CharacterFilter.Enchantments.Where(x => x.SpellId == Minor).Count() > 0;
        }
        public bool IsModerate()
        {
            return CoreManager.Current.CharacterFilter.Enchantments.Where(x => x.SpellId == Moderate).Count() > 0;
        }

        public bool IsMajor()
        {
            return CoreManager.Current.CharacterFilter.Enchantments.Where(x => x.SpellId == Major).Count() > 0;
        }

        public bool IsEpic()
        {
            return CoreManager.Current.CharacterFilter.Enchantments.Where(x => x.SpellId == Epic).Count() > 0;
        }

        public bool IsLegendary()
        {
            return CoreManager.Current.CharacterFilter.Enchantments.Where(x => x.SpellId == Legendary).Count() > 0;
        }

        public int Icon()
        {
            FileService service = CoreManager.Current.Filter<FileService>();
            Decal.Filters.Spell spell = service.SpellTable.GetById(SpellId);
            if(spell == null) { return 0; }
            return spell.IconId;
        }

        public string Level()
        {
            if(IsSetDedicationBonus()) { return SetDedicationBonusLevel(); }
            if(IsSetBonus()) { return SetBonusLevel(); }
            if(IsEssence()) { return EssenceLevel(); }
            if(IsWarriorsVitality()) { return WarriorsVitalityLevel(); }
            return CantripLevel();
        }

        public string CantripLevel()
        {
            if(IsLegendary()) { return "Legendary"; };
            if(IsEpic()) { return "Epic"; };
            if(IsMajor()) { return "Major"; };
            if(IsModerate()) { return "Moderate"; };
            if(IsMinor()) { return "Minor"; };
            return "-";
        }

        public string SetBonusLevel()
        {
            if (IsLegendary()) { return "5 pieces"; };
            if (IsEpic()) { return "4 pieces"; };
            if (IsMajor()) { return "3 pieces"; };
            if (IsModerate()) { return "2 pieces"; };
            return "-";
        }
        public string SetDedicationBonusLevel()
        {
            if (IsLegendary()) { return "9 pieces"; };
            if (IsEpic()) { return "8 pieces"; };
            if (IsMajor()) { return "6 pieces"; };
            if (IsModerate()) { return "4 pieces"; }
            if (IsMinor()) { return "2 pieces"; }
            return "-";
        }

        public string EssenceLevel()
        {
            if (IsLegendary()) { return "+30 health"; };
            if (IsEpic()) { return "+25 health"; };
            if (IsMajor()) { return "+25 helath"; };
            if (IsModerate()) { return "+20 health"; } 
            if (IsMinor()) { return "+15 health"; }
            return "-";
        }

        public string WarriorsVitalityLevel()
        {
            if (IsLegendary()) { return "+20 health"; };
            if (IsEpic()) { return "+15 health"; };
            if (IsMajor()) { return "+10 health"; };
            if (IsModerate()) { return "+5 health"; }
            return "-";
        }
    }
}

