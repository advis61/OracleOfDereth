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
    public class NearbyItem
    {
        public WorldObject Item;

        public static SortType CurrentSortType = SortType.Default;

        public enum SortType
        {
            Default, // 0
            Distance, // 1
            Name, // 2
            Relevance // 3
        }

        public static void Init() { }

        public static void Sort(SortType sortType)
        {
            CurrentSortType = sortType;
        }

        public static List<NearbyItem> NearbyItems()
        {
            var items = new List<NearbyItem>();
            int myId = CoreManager.Current.CharacterFilter.Id;

            foreach (WorldObject worldObject in Nearby.Objects) {
                if (worldObject.Container != 0) continue;
                if (worldObject.Name == null || worldObject.Name.Length == 0) continue;
                if (worldObject.Icon == 8384) continue; // Bugged item
                if (worldObject.Id == myId && !Fellowship.IsInFellowship()) continue;
                if (worldObject.Behavior == 148) continue; // Cannot be picked up, cannot be selected, is not an NPC. Enemy spell projectiles.
                if (worldObject.Behavior == 4116 && worldObject.Icon == 4887) continue; // Doors

                NearbyItem item = new() { Item = worldObject };
                items.Add(item);
            }

            switch (CurrentSortType)
            {
                case SortType.Relevance:
                    var groupCounts = items.GroupBy(i => i.GroupKey(), StringComparer.OrdinalIgnoreCase)
                        .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);
                    return items.OrderByDescending(i => i.Relevance(groupCounts[i.GroupKey()] == 1))
                        .ThenBy(i => i.Distance()).ThenBy(i => i.Item.Name).ToList();
                case SortType.Name:
                    return items.OrderBy(i => i.FellowshipName()).ThenBy(i => i.Item.Name).ThenBy(i => i.Distance()).ToList();
                case SortType.Distance:
                    return items.OrderBy(i => i.FellowshipName()).ThenBy(i => i.Distance()).ThenBy(i => i.Item.Name).ToList();
                default:
                    return items.OrderBy(i => i.Priority()).ThenBy(i => i.FellowshipName()).ThenBy(i => i.Item.Name).ThenBy(i => i.Distance()).ToList();
            }
        }

        public bool IsPlayer() { return Item.ObjectClass == ObjectClass.Player; }
        public bool IsMonster() { return Item.ObjectClass == ObjectClass.Monster; }
        public bool IsNpc() { return Item.ObjectClass == ObjectClass.Npc; }
        public bool IsVendor() { return Item.ObjectClass == ObjectClass.Vendor; }
        public bool IsPortal() { return Item.ObjectClass == ObjectClass.Portal; }
        public bool IsSign() { return Item.ObjectClass == ObjectClass.Misc && (Item.Icon == 4819 || Item.Icon == 9046); }
        
        public bool IsMarker() { return Item.Name == "Exploration Marker"; }
        public bool IsCorpse() { return (Item.Behavior & 0x00002000) != 0; }
        public double Distance() { return Util.GetDistanceFromPlayer(Item); }

        private double Relevance(bool uniqueName)
        {
            double score = 0;
            double age = Nearby.Age(Item.Id).TotalSeconds;

            if (age <= 5) score += 1000;
            else if (age <= 15) score += 600;
            else if (age <= 30) score += 250;

            if (uniqueName && !IsPlayer()) score += IsMonster() ? 350 : 500;

            if (IsPlayer()) score -= 200;
            else if (IsMarker()) score += 450;
            else if (IsNpc() || IsVendor()) score += 180;
            else if (IsPortal()) score += 120;
            else if (IsCorpse()) score -= 100;
            else score += 300;

            string name = Item.Name.ToLowerInvariant();
            if (new[] { "lever", "switch", "button", "pedestal", "altar" }.Any(name.Contains)) score += 900;
            if (new[] { "key", "gate", "door", "chest", "cache", "crystal", "orb", "idol", "fragment" }.Any(name.Contains)) score += 650;

            if (!IsPlayer() && !IsMonster() && !IsCorpse() && age > 60)
                score -= Math.Min(500, (age - 60) * 2);

            return score + (200 / (1 + Math.Max(0, Distance())));
        }

        public static bool MatchesFilter(bool isPlayer, bool isMonster, bool players, bool monsters, bool other)
        {
            if (!players && !monsters && !other) return true;
            if (isPlayer) return players;
            if (isMonster) return monsters;
            return other;
        }

        public string FellowshipName()
        {
            if (!IsPlayer()) return "";

            Fellow fellow = FellowshipTracker.Find(Item.Id);
            if (fellow == null) return "";
            
            return fellow.FellowshipName;
        }

        public bool ForceGroup()
        {
            if(IsPlayer() && FellowshipName() != "") { return true; }
            return false;
        }

        public string GroupKey()
        {
            if(IsPlayer())
            {
                string fellowshipName = FellowshipName();
                return (fellowshipName.Length > 0 ? $"Fellow: {fellowshipName}" : "Players");
            }

            if(IsPortal()) return "Portals";
            else if(IsNpc()) return "NPCs";
            else if(IsVendor()) return "Vendors";
            else if(IsMarker()) return "Exploration Markers";
            else if(IsCorpse()) return "Corpses";
            else if(IsSign()) return "Signs";

            return Item.Name;
        }

        public int Priority()
        {
            if (IsPlayer() && FellowshipName() != "") return 1; // Fellowship members
            if (IsPlayer()) return 2;
            if (IsMarker()) return 3;
            // 4 = anything unexpected (default below)
            if (IsPortal()) return 5;
            if (IsNpc()) return 6;
            if (IsVendor()) return 7;
            if (IsCorpse()) return 8;
            if (IsMonster()) return 9;
            if (IsSign()) return 10;

            return 3;
        }

    }
}


