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
        public static SortType CurrentSortType = SortType.NameAscending;

        public enum SortType
        {
            NameAscending,
            NameDescending,
            Col1Ascending,
            Col1Descending,
            Col2Ascending,
            Col2Descending,
            Col3Ascending,
            Col3Descending,
            Col4Ascending,
            Col4Descending,
        }

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
        public int ObjectClassId = 0;
        public int SortCategory = 0; // Groups like items together: 0=weapon, 1=armor, 2=jewelry, 3=cloak, 4=summon, 5=aetheria, 9=other
        public string SummaryCol1 = "";
        public string SummaryCol2 = "";
        public string SummaryCol3 = "";
        public string SummaryCol4 = "";
        public int SortCol2 = 0;
        public int SortCol3 = 0;
        public int SortCol4 = 0;
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
            if (wo.ObjectClass == ObjectClass.MissileWeapon && wo.Values(LongValueKey.StackMax, 0) > 0) return false;
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

        private static bool IsAddAllClass(ObjectClass objClass)
        {
            return objClass == ObjectClass.MeleeWeapon
                || objClass == ObjectClass.MissileWeapon
                || objClass == ObjectClass.WandStaffOrb
                || objClass == ObjectClass.Armor
                || objClass == ObjectClass.Clothing
                || objClass == ObjectClass.Jewelry
                || objClass == ObjectClass.Misc   // Aetheria and Summons
                || objClass == ObjectClass.Salvage;
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
        public static void AddAll()
        {
            using (var inv = CoreManager.Current.WorldFilter.GetInventory())
            {
                foreach (WorldObject wo in inv)
                {
                    if (!IsAddAllClass(wo.ObjectClass)) continue;
                    if (!IsInInventory(wo)) continue;
                    if (TradeItems.Any(t => t.Id == wo.Id)) continue;

                    if (wo.HasIdData)
                    {
                        if (!IsTradeable(wo)) continue;
                        AddFromWorldObject(wo);
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
                ObjectClassId = (int)wo.ObjectClass,
                SortCategory = GetSortCategory(info),
                SummaryCol1 = GetSummaryCol1(info),
                SummaryCol2 = GetSummaryCol2(info),
                SummaryCol3 = GetSummaryCol3(info),
                SummaryCol4 = GetSummaryCol4(info),
                SortCol2 = GetSortInt(info.GetODValue()),
                SortCol3 = GetSortInt(info.GetOMValue()),
                SortCol4 = GetSortInt(info.GetOAValue()),
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
            if (info.IsWeapon) return info.GetOMString() ?? "";
            if (info.IsCloak) return info.GetRatingsString();
            if (info.IsSummon) return info.GetSummonDefense();
            if (info.IsAetheria) return info.GetAetheriaSurge();
            if (info.IsArmorClothing) return info.GetRatingsString();
            return "";
        }

        private static string GetSummaryCol4(ItemInfo info)
        {
            if (info.IsWeapon) return info.GetOAString() ?? "";
            if (info.IsCloak) return "";
            if (info.IsArmorClothing || info.IsJewelry) return info.GetCantripsString();
            return "";
        }

        private static int GetSortCategory(ItemInfo info)
        {
            if (info.IsWeapon) return 0;
            if (info.IsArmorClothing) return 1;
            if (info.IsJewelry) return 2;
            if (info.IsCloak) return 3;
            if (info.IsSummon) return 4;
            if (info.IsAetheria) return 5;
            if (info.IsFoolproof) return 6;
            return 9;
        }

        private static int GetSortInt(int? value)
        {
            return value ?? 0;
        }

        private static bool IsEmpty(string s) => string.IsNullOrEmpty(s);

        public static void Sort(SortType sortType)
        {
            CurrentSortType = sortType;
            switch (sortType)
            {
                case SortType.NameAscending:
                    TradeItems = TradeItems.OrderBy(t => t.Name).ToList();
                    break;
                case SortType.NameDescending:
                    TradeItems = TradeItems.OrderByDescending(t => t.Name).ToList();
                    break;
                case SortType.Col1Ascending:
                    TradeItems = TradeItems.OrderBy(t => IsEmpty(t.SummaryCol1)).ThenBy(t => t.SummaryCol1).ThenBy(t => t.Name).ToList();
                    break;
                case SortType.Col1Descending:
                    TradeItems = TradeItems.OrderBy(t => IsEmpty(t.SummaryCol1)).ThenByDescending(t => t.SummaryCol1).ThenBy(t => t.Name).ToList();
                    break;
                case SortType.Col2Ascending:
                    TradeItems = TradeItems.OrderBy(t => IsEmpty(t.SummaryCol2)).ThenBy(t => t.SortCategory).ThenBy(t => t.SortCol2).ThenBy(t => t.SummaryCol2).ThenBy(t => t.Name).ToList();
                    break;
                case SortType.Col2Descending:
                    TradeItems = TradeItems.OrderBy(t => IsEmpty(t.SummaryCol2)).ThenBy(t => t.SortCategory).ThenByDescending(t => t.SortCol2).ThenByDescending(t => t.SummaryCol2).ThenBy(t => t.Name).ToList();
                    break;
                case SortType.Col3Ascending:
                    TradeItems = TradeItems.OrderBy(t => IsEmpty(t.SummaryCol3)).ThenBy(t => t.SortCategory).ThenBy(t => t.SortCol3).ThenBy(t => t.SummaryCol3).ThenBy(t => t.Name).ToList();
                    break;
                case SortType.Col3Descending:
                    TradeItems = TradeItems.OrderBy(t => IsEmpty(t.SummaryCol3)).ThenBy(t => t.SortCategory).ThenByDescending(t => t.SortCol3).ThenByDescending(t => t.SummaryCol3).ThenBy(t => t.Name).ToList();
                    break;
                case SortType.Col4Ascending:
                    TradeItems = TradeItems.OrderBy(t => IsEmpty(t.SummaryCol4)).ThenBy(t => t.SortCategory).ThenBy(t => t.SortCol4).ThenBy(t => t.SummaryCol4).ThenBy(t => t.Name).ToList();
                    break;
                case SortType.Col4Descending:
                    TradeItems = TradeItems.OrderBy(t => IsEmpty(t.SummaryCol4)).ThenBy(t => t.SortCategory).ThenByDescending(t => t.SortCol4).ThenByDescending(t => t.SummaryCol4).ThenBy(t => t.Name).ToList();
                    break;
            }
        }

        public static void Add(TradeItem item)
        {
            if (TradeItems.Any(t => t.Id == item.Id)) return;
            TradeItems.Add(item);
            Sort(CurrentSortType);
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
