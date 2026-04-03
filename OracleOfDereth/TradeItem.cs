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

        public static bool AutoAddEnabled = false;

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

        public static bool IsTradeable(WorldObject wo)
        {
            if (wo == null) return false;
            if (wo.ObjectClass == ObjectClass.Container) return false;
            if (wo.Values(LongValueKey.Attuned, 0) > 0) return false;
            if (!IsInInventory(wo)) return false;
            return true;
        }

        private static bool IsInInventory(WorldObject wo)
        {
            int characterId = CoreManager.Current.CharacterFilter.Id;
            int containerId = wo.Container;
            while (containerId != 0)
            {
                if (containerId == characterId) return true;
                WorldObject container = CoreManager.Current.WorldFilter[containerId];
                if (container == null) return false;
                containerId = container.Container;
            }
            return false;
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

            if (wo.ObjectClass == ObjectClass.Container) return false;
            if (!IsInInventory(wo)) return false;
            if (TradeItems.Any(t => t.Id == id)) return false;

            if (wo.HasIdData)
            {
                if (!IsTradeable(wo)) return false;
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

            if (!IsTradeable(item)) return true;

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
