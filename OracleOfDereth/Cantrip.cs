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
        public int Icon = 0;
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
                        Icon = int.Parse(fields[2].Trim()),
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
            if(SkillId == 0) { return true; }
            return new Skill((CharFilterSkillType)SkillId).IsKnown();
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

        public int Another()
        {
            FileService service = CoreManager.Current.Filter<FileService>();
            Decal.Filters.Spell spell = service.SpellTable.GetById(SkillId);
            if(spell == null) { return 0; }
            return spell.IconId;
        }

        public string Level()
        {
            if(IsLegendary()) { return "Legendary"; };
            if(IsEpic()) { return "Epic"; };
            if(IsMajor()) { return "Major"; };
            if(IsModerate()) { return "Moderate"; };
            if(IsMinor()) { return "Minor"; };
            return "-";
        }
    }
}

