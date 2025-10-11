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

        private static int lasta = 0;
        private static int lastb = 0;
        private static int lastc = 0;
        private static int lastd = 0;
        private static int laste = 0;
        private static int lastf = 0;
        private static int lastg = 0;
        private static int lasth = 0;
        private static int lasti = 0;
        private static int lastj = 0;

        public static void Info()
        {
            int count = GetMarkerInfo("explorationmarkersfound");
            int a = GetMarkerInfo("explorationmarkersfoundingroupa");
            int b = GetMarkerInfo("explorationmarkersfoundingroupb");
            int c = GetMarkerInfo("explorationmarkersfoundingroupc");
            int d = GetMarkerInfo("explorationmarkersfoundingroupd");
            int e = GetMarkerInfo("explorationmarkersfoundingroupe");
            int f = GetMarkerInfo("explorationmarkersfoundingroupf");
            int g = GetMarkerInfo("explorationmarkersfoundingroupg");
            int h = GetMarkerInfo("explorationmarkersfoundingrouph");
            int i = GetMarkerInfo("explorationmarkersfoundingroupi");
            int j = GetMarkerInfo("explorationmarkersfoundingroupj");

            Util.Think($"{count} Markers A:{a} B:{b} C:{c} D:{d} E:{e} F:{f} G:{g} H:{h} I:{i} J:{j}");

            if(a != 0 && a != lasta) { Util.Think($"A: {lasta}->{a}, #{count}: {a - lasta} explorationmarkersfoundingroupa"); }
            if(b != 0 && b != lastb) { Util.Think($"B: {lastb}->{b}, #{count}: {b - lastb} explorationmarkersfoundingroupb"); }
            if(c != 0 && c != lastc) { Util.Think($"C: {lastc}->{c}, #{count}: {c - lastc} explorationmarkersfoundingroupc"); }
            if(d != 0 && d != lastd) { Util.Think($"D: {lastd}->{d}, #{count}: {d - lastd} explorationmarkersfoundingroupd"); }
            if(e != 0 && e != laste) { Util.Think($"E: {laste}->{e}, #{count}: {e - laste} explorationmarkersfoundingroupe"); }
            if(f != 0 && f != lastf) { Util.Think($"F: {lastf}->{f}, #{count}: {f - lastf} explorationmarkersfoundingroupf"); }
            if(g != 0 && g != lastg) { Util.Think($"G: {lastg}->{g}, #{count}: {g - lastg} explorationmarkersfoundingroupg"); }
            if(h != 0 && h != lasth) { Util.Think($"H: {lasth}->{h}, #{count}: {h - lasth} explorationmarkersfoundingrouph"); }
            if(i != 0 && i != lasti) { Util.Think($"I: {lasti}->{i}, #{count}: {i - lasti} explorationmarkersfoundingroupi"); }
            if(j != 0 && j != lastj) { Util.Think($"J: {lastj}->{j}, #{count}: {j - lastj} explorationmarkersfoundingroupj"); }

            lasta = a;
            lastb = b;
            lastc = c;
            lastd = d;
            laste = e;
            lastf = f;
            lastg = g;
            lasth = h;
            lasti = i;
            lastj = j;

            QuestFlag.Refresh();
        }

        public static int GetMarkerInfo(string flag)
        {
            QuestFlag.QuestFlags.TryGetValue(flag, out QuestFlag questFlag);
            if (questFlag == null) { return 0; }

            return questFlag.Solves;
        }


        public new string ToString()
        {
            return $"{Number} {Name}: {Flag} BitMask:{BitMask}";
        }

        public string Url()
        {
            return $"https://acportalstorm.com/wiki/Dereth_Exploration/Markers_by_Efficiency#{Location.Replace(" ", "_")}";
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


