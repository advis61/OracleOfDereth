using Decal.Adapter;
using Decal.Adapter.Wrappers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OracleOfDereth
{
    public class TradeItem
    {
        // Collection of Trade Items
        public static List<TradeItem> TradeItems = new List<TradeItem>();

        // Items pending identification before being added
        private static HashSet<int> PendingIds = new HashSet<int>();

        // Properties
        public string Name = "";
        public int Id = 0;
        public int Icon = 0;
        public string Description = "";

        public static void Init()
        {
            TradeItems.Clear();
            PendingIds.Clear();
        }

        /// <summary>
        /// Request to add an item by id. If already identified, adds immediately and returns true.
        /// Otherwise requests identification and returns false (will be added via Identified()).
        /// </summary>
        public static bool RequestAdd(int id)
        {
            if (id == 0) return false;

            WorldObject wo = CoreManager.Current.WorldFilter[id];
            if (wo == null) return false;

            if (TradeItems.Any(t => t.Id == id)) return false;

            if (wo.HasIdData)
            {
                AddFromWorldObject(wo);
                return true;
            }

            PendingIds.Add(id);
            CoreManager.Current.Actions.RequestId(id);
            return false;
        }

        /// <summary>
        /// Called from WorldObjectIdentifier_Identified when an item finishes identification.
        /// Returns true if this item was pending for the trade list.
        /// </summary>
        public static bool Identified(WorldObject item)
        {
            if (!PendingIds.Contains(item.Id)) return false;

            PendingIds.Remove(item.Id);
            AddFromWorldObject(item);
            return true;
        }

        private static void AddFromWorldObject(WorldObject wo)
        {
            ItemInfo info = new ItemInfo(wo);

            Add(new TradeItem
            {
                Id = wo.Id,
                Name = wo.Name,
                Icon = wo.Icon,
                Description = info.ToString(),
            });
        }

        public static void Add(TradeItem item)
        {
            if (TradeItems.Any(t => t.Id == item.Id)) return;
            TradeItems.Add(item);
        }

        public static void Remove(int id)
        {
            TradeItems.RemoveAll(t => t.Id == id);
        }

        public static void Clear()
        {
            TradeItems.Clear();
        }

        public override string ToString()
        {
            return $"{Name} ({Id})";
        }
    }
}
