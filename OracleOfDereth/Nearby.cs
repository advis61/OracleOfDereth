using Decal.Adapter;
using Decal.Adapter.Wrappers;
using Microsoft.SqlServer.Server;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ToolTip;


namespace OracleOfDereth
{
    public class Nearby
    {
        // Collection
        public static Dictionary<string, Nearby> Nearbys = new Dictionary<string, Nearby>(); // The list we match against
        public static List<WorldObject> Objects = new List<WorldObject>(); // The nearby WorldObjects we Track

        public static bool Verbose = true;

        // Properties
        public string Landblock = "";

        public static void Init()
        {
            Nearbys.Clear();
            LoadNearbysCSV();
        }

        private static List<WorldObject> All(ObjectClass c) { return Objects.Where(o => o.ObjectClass == c).ToList(); }

        public static List<WorldObject> Monsters() { return All(ObjectClass.Monster); }
        public static List<WorldObject> Npcs() { return All(ObjectClass.Npc).Concat(All(ObjectClass.Vendor)).ToList(); }

        public static List<WorldObject> Items() {
            return Objects.Where(o =>
                o.ObjectClass != ObjectClass.Player &&
                o.ObjectClass != ObjectClass.Monster &&
                o.ObjectClass != ObjectClass.Npc &&
                o.ObjectClass != ObjectClass.Vendor
            ).ToList();
        }

        public static void LoadNearbysCSV()
        {
            var nearbys = new List<Nearby>();

            var assembly = Assembly.GetExecutingAssembly();

            string resourceName = assembly.GetManifestResourceNames().FirstOrDefault(n => n.EndsWith("nearbys.csv", StringComparison.OrdinalIgnoreCase));
            if (resourceName == null) throw new FileNotFoundException("Embedded resource nearbys.csv not found.");

            using (var stream = assembly.GetManifestResourceStream(resourceName))
            using (var reader = new StreamReader(stream))
            {
                string headerLine = reader.ReadLine();
                if (headerLine == null) throw new InvalidDataException("CSV file is empty.");

                // Assume columns: Name,Landblock
                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine();
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    var fields = line.Split(',');
                    string name = fields[0].Trim().ToLower();

                    Nearbys[name] = new Nearby {
                        Landblock = fields[1].Trim(),
                    };
                }
            }

            Util.Chat($"Loaded {Nearbys.Count} Nearbys from embedded CSV.", 1);
        }

        public static void Add(WorldObject item)
        {
            // We track players differently via Fellow
            if(item.ObjectClass == ObjectClass.Player) { 
                Announce(item);
                return; 
            }

            Nearby nearby = Nearbys[item.Name.ToLower()];
            if(nearby == null) { return; }

            Objects.Add(item);

            Announce(item);
        }

        public static void Remove(WorldObject item)
        {
            Objects.Remove(item);
        }

        public static void Announce(WorldObject item)
        {
            if(Verbose == false) { return; }
            Util.Chat($"Detected: {item.Name}", 5, "[OD] ");
        }
    }
}


