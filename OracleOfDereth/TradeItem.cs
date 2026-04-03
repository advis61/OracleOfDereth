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

        // Queue of item ids waiting to be identified (for Add All)
        private static Queue<int> IdentifyQueue = new Queue<int>();
        public static bool IsProcessingQueue = false;

        public static bool AutoAddEnabled = false;

        // Callback to refresh the UI after an item is added from the queue
        public static Action OnTradeListChanged;
        public static Action OnQueueFinished;

        // Properties
        public string Name = "";
        public int Id = 0;
        public int Icon = 0;
        public string SummaryCol1 = "";
        public string SummaryCol2 = "";
        public string SummaryCol3 = "";
        public string SummaryCol4 = "";
        public string Description = "";

        public static void Init()
        {
            TradeItems.Clear();
            PendingIds.Clear();
            IdentifyQueue.Clear();
            IsProcessingQueue = false;
        }

        public static int QueueCount => IdentifyQueue.Count + PendingIds.Count;

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

        /// <summary>
        /// Scans the entire inventory. Adds identified tradeable items immediately,
        /// queues unidentified items for identification one at a time.
        /// </summary>
        public static int AddAll()
        {
            int addedImmediately = 0;

            using (var inv = CoreManager.Current.WorldFilter.GetInventory())
            {
                foreach (WorldObject wo in inv)
                {
                    if (wo.ObjectClass == ObjectClass.Container) continue;
                    if (!IsInInventory(wo)) continue;
                    if (TradeItems.Any(t => t.Id == wo.Id)) continue;

                    if (wo.HasIdData)
                    {
                        if (!IsTradeable(wo)) continue;
                        AddFromWorldObject(wo);
                        addedImmediately++;
                    }
                    else
                    {
                        if (!PendingIds.Contains(wo.Id) && !IdentifyQueue.Contains(wo.Id))
                        {
                            IdentifyQueue.Enqueue(wo.Id);
                        }
                    }
                }
            }

            if (IdentifyQueue.Count > 0 && !IsProcessingQueue)
            {
                IsProcessingQueue = true;
                CoreManager.Current.WorldFilter.ChangeObject += TradeItem_ChangeObject;
                ProcessNextInQueue();
            }

            return addedImmediately;
        }

        private static void ProcessNextInQueue()
        {
            // Skip items that have already been added or no longer exist
            while (IdentifyQueue.Count > 0)
            {
                int id = IdentifyQueue.Dequeue();

                WorldObject wo = CoreManager.Current.WorldFilter[id];
                if (wo == null) continue;
                if (TradeItems.Any(t => t.Id == id)) continue;

                PendingIds.Add(id);
                CoreManager.Current.Actions.RequestId(id);
                return;
            }

            // Queue is empty, stop listening
            StopProcessingQueue();
        }

        private static void TradeItem_ChangeObject(object sender, ChangeObjectEventArgs e)
        {
            try
            {
                if (e.Change != WorldChangeType.IdentReceived) return;
                if (!PendingIds.Contains(e.Changed.Id)) return;

                PendingIds.Remove(e.Changed.Id);

                if (IsTradeable(e.Changed))
                {
                    AddFromWorldObject(e.Changed);
                }

                OnTradeListChanged?.Invoke();
                ProcessNextInQueue();
            }
            catch (Exception ex) { Util.Log(ex); }
        }

        public static void CancelQueue()
        {
            IdentifyQueue.Clear();
            PendingIds.Clear();
            StopProcessingQueue();
        }

        private static void StopProcessingQueue()
        {
            if (!IsProcessingQueue) return;
            IsProcessingQueue = false;
            CoreManager.Current.WorldFilter.ChangeObject -= TradeItem_ChangeObject;
            OnQueueFinished?.Invoke();
        }

        public static string StatusText()
        {
            int pending = IdentifyQueue.Count + PendingIds.Count;
            if (pending > 0)
                return $"Trade Items: {TradeItems.Count} done, {pending} pending";
            return $"Trade Items: {TradeItems.Count} selected";
        }

        private static void AddFromWorldObject(WorldObject wo)
        {
            ItemInfo info = new ItemInfo(wo);

            Add(new TradeItem
            {
                Id = wo.Id,
                Name = info.GetName(),
                Icon = wo.Icon,
                SummaryCol1 = GetSummaryCol1(info),
                SummaryCol2 = GetSummaryCol2(info),
                SummaryCol3 = GetSummaryCol3(info),
                SummaryCol4 = GetSummaryCol4(info),
                Description = info.ToString(),
            });
        }


        private static string GetSummaryCol1(ItemInfo info)
        {
            return info.GetItemSlotName();
        }

        private static string GetSummaryCol2(ItemInfo info)
        {
            if (info.IsWeapon) return info.GetODString() ?? "";
            if (info.IsCloak) return info.GetCloakProc();
            if (info.IsSummon) return info.GetSummonDamage();
            if (info.IsAetheria) return info.GetSetName();
            if (info.IsArmorClothing) return info.GetSetName();
            if (info.IsJewelry) return info.GetRatingsString();
            return "";
        }

        private static string GetSummaryCol3(ItemInfo info)
        {
            if (info.IsWeapon) return info.GetOAString() ?? "";
            if (info.IsCloak) return info.GetRatingsString();
            if (info.IsSummon) return info.GetSummonDefense();
            if (info.IsAetheria) return info.GetAetheriaSurge();
            if (info.IsArmorClothing) return info.GetRatingsString();
            return "";
        }

        private static string GetSummaryCol4(ItemInfo info)
        {
            if (info.IsWeapon) return info.GetOMString() ?? "";
            return "";
        }

        public static void Add(TradeItem item)
        {
            if (TradeItems.Any(t => t.Id == item.Id)) return;
            TradeItems.Add(item);
            TradeItems.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
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
