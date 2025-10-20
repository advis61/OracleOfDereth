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

        public static SortType CurrentSortType = SortType.CategoryAscending;

        public enum SortType
        {
            CompleteAscending,
            CompleteDescending,
            NameAscending,
            NameDescending,
            LevelAscending,
            LevelDescending,
            CategoryAscending,
            CategoryDescending,
        }

        // Properties
        public int Number = 0;
        public string Name = "";
        public int TitleId = 0;
        public string Type = "";
        public string Category = "";
        public int Level = 0;
        public string Url = "";
        public string Hint = "";

        public static void Init()
        {
            Util.Chat("Clearing known titles data.", Util.ColorPink);
            Titles.Clear();
            LoadTitlesCSV();
        }
        public static List<Title> Available() { return Titles.Where(a => a.Category != "Unavailable").ToList(); }
        public static List<Title> Unavailable() { return Titles.Where(a => a.Category == "Unavailable").ToList(); }

        public static void Parse(MessageStruct titles)
        {
            KnownTitleIds.Clear();

            for (int i = 0; i < titles.Count; i++) {
                Int32 titleId = titles.Struct(i).Value<Int32>("title");
                if (!KnownTitleIds.Contains(titleId)) { KnownTitleIds.Add(titleId); }
            }
            
            Util.Chat($"Titles data updated. {KnownTitleIds.Count} titles completed.", Util.ColorPink);
        }

        public static void ParseUpdate(int titleId)
        {
            if (!KnownTitleIds.Contains(titleId)) { KnownTitleIds.Add(titleId); }
        }

        public static void Sort(SortType sortType)
        {
            CurrentSortType = sortType;
            switch (sortType)
            {
                case SortType.CompleteAscending:
                    Titles = Titles.OrderBy(q => q.IsComplete()).ThenBy(q => q.CategorySortKey()).ThenBy(q => q.Level).ThenBy(q => q.Number).ToList();
                    break;
                case SortType.CompleteDescending:
                    Titles = Titles.OrderByDescending(q => q.IsComplete()).ThenBy(q => q.CategorySortKey()).ThenBy(q => q.Level).ThenBy(q => q.Number).ToList();
                    break;
                case SortType.NameAscending:
                    Titles = Titles.OrderBy(q => q.Name).ToList();
                    break;
                case SortType.NameDescending:
                    Titles = Titles.OrderByDescending(q => q.Name).ToList();
                    break;
                case SortType.LevelAscending:
                    Titles = Titles.OrderBy(q => q.Level).ThenBy(q => q.Number).ToList();
                    break;
                case SortType.LevelDescending:
                    Titles = Titles.OrderByDescending(q => q.Level).ThenByDescending(q => q.Number).ToList();
                    break;
                case SortType.CategoryAscending:
                    Titles = Titles.OrderBy(q => q.CategorySortKey()).ThenBy(q => q.Level).ThenBy(q => q.Number).ToList();
                    break;
                case SortType.CategoryDescending:
                    Titles = Titles.OrderByDescending(q => q.CategorySortKey()).ThenByDescending(q => q.Level).ThenByDescending(q => q.Number).ToList();
                    break;
            }
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
                        Number = int.Parse(fields[0].Trim()),
                        Name = fields[1].Trim(),
                        TitleId = int.Parse(fields[2].Trim()),
                        Type = fields[3].Trim(),
                        Category = fields[4].Trim(),
                        Level = int.Parse(fields[5].Trim()),
                        Url = fields[6].Trim(),
                        Hint = fields[7].Trim()
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

        public string CategorySortKey()
        {
            if(Type.Length > 0) { return Type; }
            return Category;
        }

    }

}




