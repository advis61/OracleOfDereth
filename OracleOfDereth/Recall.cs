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
    public class Recall
    {
        // Collection of Recalls loaded from Recalls.csv
        public static List<Recall> Recalls = new List<Recall>();

        // Properties
        public string Name = "";
        public int SpellId = 0;
        public string Url = "";
        public string Hint = "";

        public static void Init()
        {
            Recalls.Clear();
            LoadRecallsCSV();
        }

        public static void LoadRecallsCSV()
        {
            var recalls = new List<Recall>();

            var assembly = Assembly.GetExecutingAssembly();

            string resourceName = assembly.GetManifestResourceNames().FirstOrDefault(n => n.EndsWith("recalls.csv", StringComparison.OrdinalIgnoreCase));
            if (resourceName == null) throw new FileNotFoundException("Embedded resource Recalls.csv not found.");

            using (var stream = assembly.GetManifestResourceStream(resourceName))
            using (var reader = new StreamReader(stream))
            {
                string headerLine = reader.ReadLine();
                if (headerLine == null) throw new InvalidDataException("CSV file is empty.");

                // Assume columns: Name,SpellId,Url,Hint
                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine();
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    var fields = line.Split(',');

                    recalls.Add(new Recall
                    {
                        Name = fields[0].Trim(),
                        SpellId = int.Parse(fields[1].Trim()),
                        Url = fields[2].Trim(),
                        Hint = fields[3].Trim()
                    });
                }
            }

            Recalls.AddRange(recalls);

            // Util.Chat($"Loaded {Recalls.Count} Credit Quests from embedded CSV.", 1);
        }

        public new string ToString()
        {
            return $"{Name}: {SpellId}";
        }

        public bool IsComplete()
        {
            int id = CoreManager.Current.CharacterFilter.SpellBook.FirstOrDefault(x => x == SpellId);
            if (id == 0) { return false; }

            return true;
        }
    }
}



