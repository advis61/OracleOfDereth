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

namespace OracleOfDereth {

    public class Marker
    {
        // Collection of Exploration Markers loaded from markers.csv
        public static List<Marker> Markers = new List<Marker>();

        // Properties
        public int Number = 0;
        public string Name = "";
        public string Location = "";
        public int BitMask = 0;
        public string Flag = "";
        public string Hint = "";

        public static void Init()
        {
            Markers.Clear();
            LoadMarkersCSV();
        }

        public static void LoadMarkersCSV()
        {
            var markers = new List<Marker>();

            var assembly = Assembly.GetExecutingAssembly();

            string resourceName = assembly.GetManifestResourceNames().FirstOrDefault(n => n.EndsWith("markers.csv", StringComparison.OrdinalIgnoreCase));
            if (resourceName == null) throw new FileNotFoundException("Embedded resource markers.csv not found.");

            using (var stream = assembly.GetManifestResourceStream(resourceName))
            using (var reader = new StreamReader(stream))
            {
                string headerLine = reader.ReadLine();
                if (headerLine == null) throw new InvalidDataException("CSV file is empty.");

                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine();
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    var fields = line.Split(',');

                    markers.Add(new Marker
                    {
                        Number = int.Parse(fields[0].Trim()),
                        Name = fields[1].Trim(),
                        Location = fields[2].Trim(),
                        BitMask = int.Parse(fields[3].Trim()),
                        Flag = fields[4].Trim().ToLower(),
                        Hint = fields[5].Trim()
                    });
                }
            }

            Markers.AddRange(markers);

            //Util.Chat($"Loaded {Quests.Count} John Quests from embedded CSV.", 1);
        }

        public new string ToString()
        {
            return $"{Number} {Name}: {Flag} BitMask:{BitMask}";
        }

        public bool IsComplete()
        {
            QuestFlag.QuestFlags.TryGetValue(Flag, out QuestFlag questFlag);
            if (questFlag == null) { return false; }

            // Check if the BitMask is set in solves
            return (questFlag.Solves & BitMask) == BitMask;
        }
    }
}


