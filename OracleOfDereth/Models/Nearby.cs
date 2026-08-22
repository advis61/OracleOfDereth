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

        // Track identities, not WorldObject wrappers. CreateObject can be raised more than once
        // for the same object, while wrappers can become stale after zoning or a missed release.
        // Keeping only ids makes duplicate creates harmless and avoids retaining those wrappers
        // indefinitely. Nearby collections are small, so a list keeps the implementation simple.
        private static readonly List<int> ObjectIds = new List<int>();

        // Preserve the old read API for callers, but resolve fresh wrappers from WorldFilter.
        public static List<WorldObject> Objects => ObjectIds
            .Select(id => CoreManager.Current.WorldFilter[id])
            .Where(item => item != null)
            .ToList();

        // Properties
        public string Landblock = "";

        public static void Init()
        {
            Nearbys.Clear();
            ObjectIds.Clear(); // drop the previous character's tracked object identities
            LoadNearbysCSV();

            // Hot reload can start after the landscape's CreateObject events have already fired.
            foreach (WorldObject item in CoreManager.Current.WorldFilter.GetLandscape())
            {
                if (item.Id != 0) ObjectIds.Add(item.Id);
            }
        }

        // A portal transition starts a new nearby-object set. WorldFilter can retain wrappers
        // from the previous landblock, so waiting for those wrappers to disappear is unreliable.
        public static void ClearObjects() { ObjectIds.Clear(); }

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
            // We track players differently via Fellow
            if(item.ObjectClass == ObjectClass.Player && Setting.AnnouncePlayers.IsYes) { 
                Announce(item);
                return; 
            }

            // Add all non-player objects once, keyed by their stable world id.
            if (item.Id != 0 && !ObjectIds.Contains(item.Id)) ObjectIds.Add(item.Id);

            // Announcing other objects disabled
            //Nearby nearby = Nearbys[item.Name.ToLower()];
            //if (nearby != null) { Announce(item); }
        }

        public static void Remove(WorldObject item)
        {
            if (item != null) ObjectIds.Remove(item.Id);
        }

        // Drop tracked ids the client no longer knows about. ReleaseObject isn't guaranteed to
        // fire on every despawn (zone/portal/recall transitions can miss it), so this reconciles
        // the set without retaining stale WorldObject wrappers.
        // Mirrors FellowshipTracker's RemoveGonePlayers reconciliation.
        public static void Tick()
        {
            ObjectIds.RemoveAll(id => CoreManager.Current.WorldFilter[id] == null);
        }

        public static void Announce(WorldObject item)
        {
            Util.Chat($"Detected: {item.Name}", 5, "[OD] ");
        }
    }
}


