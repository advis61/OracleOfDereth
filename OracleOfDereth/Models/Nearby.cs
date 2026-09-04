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

        private sealed class TrackedObject
        {
            public string Name;
            public ObjectClass Class;
            public int MissedScans;
            public DateTime FirstSeenAt;
        }

        private static readonly Dictionary<int, TrackedObject> Tracked = new Dictionary<int, TrackedObject>();
        private static readonly TimeSpan ScanInterval = TimeSpan.FromSeconds(2);
        private const int MaxMissedScans = 3;
        private static DateTime lastScanAt = DateTime.MinValue;

        // Preserve the old read API for callers, but resolve fresh wrappers from WorldFilter.
        public static List<WorldObject> Objects => Tracked.Keys
            .Select(id => CoreManager.Current.WorldFilter[id])
            .Where(item => item != null)
            .ToList();

        // Properties
        public string Landblock = "";

        public static void Init()
        {
            Nearbys.Clear();
            Tracked.Clear();
            lastScanAt = DateTime.MinValue;
            LoadNearbysCSV();
            Reconcile(); // catches objects whose CreateObject event preceded plugin startup
        }

        // A portal transition starts a new nearby-object set. WorldFilter can retain wrappers
        // from the previous landblock, so waiting for those wrappers to disappear is unreliable.
        public static void ClearObjects()
        {
            Tracked.Clear();
            lastScanAt = DateTime.MinValue;
        }

        public static List<WorldObject> All() {
            return Objects.Where(o => o.ObjectClass != ObjectClass.Player).ToList();
        }

        private static List<WorldObject> All(ObjectClass c) { 
            return All().Where(o => o.ObjectClass == c).ToList(); 
        }

        public static List<WorldObject> Monsters() { return All(ObjectClass.Monster); }
        public static List<WorldObject> Npcs() { return All(ObjectClass.Npc).Concat(All(ObjectClass.Vendor)).ToList(); }
        public static List<WorldObject> Portals() { return All(ObjectClass.Portal); }

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

                    var fields = Util.CsvParseLine(line);
                    string name = fields[0].Trim().ToLower();

                    Nearbys[name] = new Nearby {
                        Landblock = fields[1].Trim(),
                    };
                }
            }

            //Util.Chat($"Loaded {Nearbys.Count} Nearbys from embedded CSV.", 1);
        }

        public static void Add(WorldObject item)
        {
            if (item == null || item.Id == 0) return;
            Track(item);

            if (item.ObjectClass == ObjectClass.Player && Setting.AnnouncePlayers.IsYes)
            {
                Announce(item);
            }

            // Announcing other objects disabled
            //Nearby nearby = Nearbys[item.Name.ToLower()];
            //if (nearby != null) { Announce(item); }
        }

        public static void Remove(WorldObject item)
        {
            if (item != null) Tracked.Remove(item.Id);
        }

        public static void Tick()
        {
            if (DateTime.UtcNow - lastScanAt < ScanInterval) return;
            Reconcile();
        }

        private static void Reconcile()
        {
            lastScanAt = DateTime.UtcNow;
            var seen = new HashSet<int>();

            foreach (WorldObject item in CoreManager.Current.WorldFilter.GetLandscape())
            {
                if (item == null || item.Id == 0) continue;
                seen.Add(item.Id);
                Track(item);
            }

            foreach (int id in Tracked.Keys.ToList())
            {
                if (seen.Contains(id)) continue;
                if (++Tracked[id].MissedScans >= MaxMissedScans) Tracked.Remove(id);
            }
        }

        private static void Track(WorldObject item)
        {
            if (!Tracked.TryGetValue(item.Id, out TrackedObject tracked) ||
                tracked.Name != item.Name || tracked.Class != item.ObjectClass)
            {
                Tracked[item.Id] = new TrackedObject
                {
                    Name = item.Name,
                    Class = item.ObjectClass,
                    FirstSeenAt = DateTime.UtcNow
                };
            }
            else tracked.MissedScans = 0;
        }

        public static TimeSpan Age(int id)
        {
            return Tracked.TryGetValue(id, out TrackedObject item)
                ? DateTime.UtcNow - item.FirstSeenAt
                : TimeSpan.MaxValue;
        }

        public static void Announce(WorldObject item)
        {
            Util.Chat($"Detected: {item.Name}", 5, "[OD] ");
        }
    }
}


