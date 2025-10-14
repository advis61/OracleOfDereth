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
using System.Windows.Forms.VisualStyles;

namespace OracleOfDereth
{
    public class Title
    {
        // Collection of Titles loaded from titles.csv
        public static List<Title> Titles = new List<Title>();
        public static List<int> KnownTitleIds = new List<int>();

        // Properties
        public string Name = "";
        public int TitleId = 0;
        public string Category = "";
        public string Url = "";

        public static void Init()
        {
            Titles.Clear();
            KnownTitleIds.Clear();
            LoadTitlesCSV();
        }

        public static void Parse(MessageStruct titles)
        {
            for (int i = 0; i < titles.Count; i++) {
                Int32 titleId = titles.Struct(i).Value<Int32>("title");
                if (!KnownTitleIds.Contains(titleId)) { KnownTitleIds.Add(titleId); }
            }
        }

        public static void ParseUpdate(int titleId)
        {
            if (!KnownTitleIds.Contains(titleId)) { KnownTitleIds.Add(titleId); }
        }

        public static void LoadTitlesCSV()
        {
            var titles = new List<Title>();

            var assembly = Assembly.GetExecutingAssembly();

            string resourceName = assembly.GetManifestResourceNames().FirstOrDefault(n => n.EndsWith("titles.csv", StringComparison.OrdinalIgnoreCase));
            if (resourceName == null) throw new FileNotFoundException("Embedded resource Titles.csv not found.");

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

                    titles.Add(new Title
                    {
                        Name = fields[0].Trim(),
                        TitleId = int.Parse(fields[1].Trim()),
                        Category = fields[2].Trim(),
                        Url = fields[3].Trim()
                    });
                }
            }

            Titles.AddRange(titles);

            // Util.Chat($"Loaded {Titles.Count} Credit Quests from embedded CSV.", 1);
        }

        public new string ToString()
        {
            return $"{Name}: {TitleId}";
        }

        public bool IsComplete()
        {
            return KnownTitleIds.Contains(TitleId);
        }
    }
}




