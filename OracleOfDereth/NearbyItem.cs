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

        // Instance variables
        public WorldObject Item;

        public static void Init() { }

        public static List<NearbyItem> NearbyItems()
        {
            var items = new List<NearbyItem>();
            int myId = CoreManager.Current.CharacterFilter.Id;

            foreach (WorldObject worldObject in CoreManager.Current.WorldFilter.GetLandscape()) {
                if (worldObject.Name == null || worldObject.Name.Length == 0) continue;
                if (worldObject.Icon == 8384) continue; // Bugged item
                if (worldObject.Id == myId && !Fellowship.IsInFellowship()) continue;
                if (worldObject.Behavior == 148) continue; // Cannot be picked up, cannot be selected, is not an NPC. Enemy spell projectiles.
                if (worldObject.Behavior == 4116 && worldObject.Icon == 4887) continue; // Doors

                NearbyItem item = new() { Item = worldObject };
                items.Add(item);
            }

            //return items.OrderBy(i => i.Item.Name).ThenBy(i => i.Distance()).ToList();
            return items.OrderBy(i => i.Distance()).ThenBy(i => i.Item.Name).ToList();
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

        public string FellowshipName()
        {
            if (IsPlayer() == false) return "";

            Fellow fellow = Fellow.Find(Item.Id);
            if (fellow == null) return "";
            
            return fellow.FellowshipName;
        }

        public bool IsPlayerInFellowship() { return Item.ObjectClass == ObjectClass.Player; }

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

    }
}


