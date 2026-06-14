using Decal.Adapter;
using Decal.Adapter.Wrappers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OracleOfDereth
{
    // A self-contained collection of Items with its own identify/stub/sort pipeline.
    // Two independent instances exist: ItemList.Inventory (the Items tab, scans our own
    // packs) and ItemList.Trade (the trade window, fed the partner's offered items).
    public class ItemList
    {
        // Well-known instances. PluginCore creates them in Init() and broadcasts ticks /
        // identify responses to both via TickAll / IdentReceivedAll.
        public static ItemList Inventory;
        public static ItemList Trade;

        public static void Init()
        {
            Inventory = new ItemList();
            Trade = new ItemList();
        }

        public static void TickAll()
        {
            ItemCache.Tick();
            Inventory?.Tick();
            Trade?.Tick();
        }

        // An identify arrived: offer it to every list. Each one no-ops unless the id was
        // in its own pending/queue, so this is safe to broadcast.
        public static void IdentReceivedAll(WorldObject changed)
        {
            Inventory?.IdentReceived(changed);
            Trade?.IdentReceived(changed);
        }

        // Collection of Items
        public List<Item> Items = new List<Item>();
        public SortType CurrentSortType = SortType.NameAscending;

        public enum SortType
        {
            NameAscending,
            NameDescending,
            Col1Ascending,
            Col1Descending,
            Col2Ascending,
            Col2Descending,
            // Col3 cycles through four states: attack asc/desc, then melee-defense asc/desc.
            Col3AttackAscending,
            Col3AttackDescending,
            Col3MeleeAscending,
            Col3MeleeDescending,
            Col4Ascending,
            Col4Descending,
        }

        // In-flight identify requests: item id -> when we sent it. Tracked so a dropped
        // server response can be re-issued by Tick() instead of stalling the queue.
        private Dictionary<int, DateTime> PendingIds = new Dictionary<int, DateTime>();

        // Queue of item ids waiting to be identified (for Add All)
        private List<int> IdentifyQueue = new List<int>();
        public bool IsProcessingQueue = false;

        // Ids currently on screen (set by the view each refresh via PrioritizeIdentify). The
        // pump appraises these before the rest, so filtering to a category jumps those rows to
        // the front without flooding the server. Empty / whole-list when no filter is active.
        private HashSet<int> PriorityIds = new HashSet<int>();

        // Up to this many identify requests in flight at once. Kept small so the in-flight set
        // turns over fast and the pump can switch to newly-prioritized rows within a request or
        // two — a larger batch just floods the server's shared appraise channel and everything
        // trickles back slower. Tick() retries/drops any the server drops.
        private const int MaxConcurrentRequests = 5;
        private static readonly TimeSpan IdTimeout = TimeSpan.FromSeconds(5);

        // Throttle list rebuilds during bulk identify (sort + repaint is O(n)).
        private DateTime _lastRefresh = DateTime.MinValue;
        private static readonly TimeSpan RefreshInterval = TimeSpan.FromMilliseconds(250);

        public bool AutoAddEnabled = false;

        // Callbacks to refresh the UI / signal the identify queue finished.
        public Action OnItemsListChanged;
        public Action OnQueueFinished;

        public int QueueCount => IdentifyQueue.Count + PendingIds.Count;

        // Rows still showing as stubs (greyed, no detail columns). This is what the status
        // line reports as "identifying" — it matches what the user actually sees, unlike the
        // in-flight queue count which drops to 0 the moment requests resolve or are dropped.
        public int UnidentifiedCount => Items.Count(t => !t.IsIdentified);

        // Whether to include the item in the list. We intentionally keep attuned
        // items (they're shown, just not actually tradeable). All checks here are
        // known pre-ID, so an item never gets stubbed and then removed after its id.
        public static bool IsTradeable(WorldObject wo)
        {
            if (wo == null) return false;

            if (wo.ObjectClass == ObjectClass.Container) return false;
            if (wo.ObjectClass == ObjectClass.MissileWeapon && wo.Values(LongValueKey.StackMax, 0) > 0) return false;

            if (!IsInInventory(wo)) return false;
            return true;
        }

        // ObjectClasses whose items carry no appraisal-only detail — the name/icon is the whole
        // story. We mark these identified immediately instead of spending an appraisal request
        // that returns nothing useful (and would otherwise leave the row stuck "loading", and
        // re-requested every tick since it never gets HasIdData).
        public static bool NeedsNoAppraisal(WorldObject wo)
        {
            // Aetheria has ObjectClass.Gem but carries appraisal-only detail (level, surge,
            // set), so it must be identified despite its class being in the list below.
            if (wo.Name == "Aetheria") return false;

            // Rares (often Gems/Salvage) carry spells we want to show, so they need an ID too.
            if (new ItemInfo(wo).IsRare) return false;

            switch (wo.ObjectClass)
            {
                case ObjectClass.Salvage:
                case ObjectClass.SpellComponent:
                case ObjectClass.HealingKit:
                case ObjectClass.Food:
                case ObjectClass.Money:
                case ObjectClass.TradeNote:
                case ObjectClass.Gem:
                case ObjectClass.Plant:
                case ObjectClass.Ust:
                case ObjectClass.ManaStone:
                case ObjectClass.Foci:
                case ObjectClass.Bundle:
                case ObjectClass.Container:
                    return true;
                default:
                    return false;
            }
        }

        // Whether this item is currently in our inventory (chains up to our character).
        // Note: an item we drag into the trade pane may leave this chain, so callers that
        // need to recognise our own offered items also snapshot inventory at trade start.
        public static bool IsInInventory(WorldObject wo)
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
        /// Request to add an inventory item by id. If already identified, adds it immediately;
        /// otherwise stubs it and queues it for identification. Returns true if added immediately.
        /// </summary>
        public bool RequestAdd(int id)
        {
            WorldObject wo = id == 0 ? null : CoreManager.Current.WorldFilter[id];
            if (wo == null) return false;
            if (wo.ObjectClass == ObjectClass.Container) return false;
            if (!IsInInventory(wo)) return false;

            return Add(wo, useCache: false);
        }

        /// <summary>
        /// Add an item the trade partner dropped into the trade window. Unlike RequestAdd there's
        /// no inventory/container gating — a partner's offered item lives in the trade pane, not
        /// our packs — and a recent appraisal may be reused from the cache.
        /// </summary>
        public bool AddTradeItem(int id)
        {
            WorldObject wo = id == 0 ? null : CoreManager.Current.WorldFilter[id];
            if (wo == null) return false;

            return Add(wo, useCache: true);
        }

        // Shared add path: skip duplicates/in-flight, fill from live appraisal or (for trade)
        // the cache, else show a stub and queue the identify. Returns true if added identified.
        private bool Add(WorldObject wo, bool useCache)
        {
            int id = wo.Id;
            if (Items.Any(t => t.Id == id)) return false;
            if (PendingIds.ContainsKey(id) || IdentifyQueue.Contains(id)) return false;

            // Already appraised, or never needs one — add it as a finished row, no request.
            if (wo.HasIdData || NeedsNoAppraisal(wo))
            {
                AddFromWorldObject(wo);
                RefreshList();
                return true;
            }

            // Reopened a recent trade? Reuse the cached appraisal instead of re-identifying.
            if (useCache)
            {
                Item cached = ItemCache.Get(id, wo.Name);
                if (cached != null)
                {
                    Items.Add(cached);
                    RefreshList();
                    return true;
                }
            }

            // Show a stub now (icon + name); details fill in when the id arrives.
            AddStub(wo);
            IdentifyQueue.Add(id);
            RefreshList();
            PumpQueue();
            return false;
        }

        /// <summary>
        /// Scans the entire inventory. Adds already-identified tradeable items
        /// immediately and queues the rest for identification.
        /// </summary>
        public void AddAll()
        {
            bool added = false;

            using (var inv = CoreManager.Current.WorldFilter.GetInventory())
            {
                foreach (WorldObject wo in inv)
                {
                    if (!IsInInventory(wo)) continue;
                    if (Items.Any(t => t.Id == wo.Id && t.IsIdentified)) continue;                 // already done
                    if (PendingIds.ContainsKey(wo.Id) || IdentifyQueue.Contains(wo.Id)) continue;  // already in flight
                    // (an unidentified stub that gave up falls through here and gets re-queued)

                    // Don't list our own packs/containers on the items screen. (NeedsNoAppraisal
                    // returns true for containers, so without this they'd be added as finished
                    // rows below before the IsTradeable container check is ever reached.)
                    if (wo.ObjectClass == ObjectClass.Container) continue;

                    // Salvage, food, gems, healing kits, etc. carry no appraisal info — add now.
                    if (NeedsNoAppraisal(wo))
                    {
                        AddFromWorldObject(wo);
                        added = true;
                        continue;
                    }

                    if (wo.HasIdData)
                    {
                        if (IsTradeable(wo)) { AddFromWorldObject(wo); added = true; }
                        continue;
                    }

                    // Skip junk Misc up front so it never gets a stub (the only
                    // ObjectClass we can't tell apart without looking closer).
                    if (wo.ObjectClass == ObjectClass.Misc)
                    {
                        ItemInfo check = new ItemInfo(wo);
                        if (!check.IsSummon && !check.IsAetheria && !check.IsFoolproof) continue;
                    }

                    // Drop items we never list (containers, stackable missiles, not in
                    // inventory) before stubbing. All known pre-ID, so nothing gets
                    // popped in and then removed once its id comes back.
                    if (!IsTradeable(wo)) continue;

                    // Show a stub now; details fill in when its id arrives.
                    AddStub(wo);
                    IdentifyQueue.Add(wo.Id);
                    added = true;
                }
            }

            if (added) { Sort(CurrentSortType); RefreshList(); }

            // Work the identify queue in display order (alphabetical by base name)
            // so rows fill in top-to-bottom instead of in inventory order.
            IdentifyQueue = IdentifyQueue.OrderBy(id => CoreManager.Current.WorldFilter[id]?.Name ?? "").ToList();

            PumpQueue();
        }

        // Tell the list which ids are currently on screen (the view passes whatever's visible,
        // e.g. just Weapons when filtered). The pump appraises these before anything else, so
        // filtered rows jump the line as in-flight slots free up — no separate immediate-send
        // path needed. Passing the whole list (no filter) imposes no ordering.
        public void PrioritizeIdentify(IEnumerable<int> priorityIds)
        {
            PriorityIds = priorityIds == null ? new HashSet<int>() : new HashSet<int>(priorityIds);
            PumpQueue();    // a free slot should go to a now-prioritized row immediately
        }

        // Pop the next id to appraise: a still-queued on-screen (priority) one if any, else the
        // head of the queue. Lets filtered rows go first even as new items keep being added.
        private int DequeueNext()
        {
            if (PriorityIds.Count > 0)
            {
                for (int i = 0; i < IdentifyQueue.Count; i++)
                {
                    if (!PriorityIds.Contains(IdentifyQueue[i])) continue;
                    int pid = IdentifyQueue[i];
                    IdentifyQueue.RemoveAt(i);
                    return pid;
                }
            }

            int id0 = IdentifyQueue[0];
            IdentifyQueue.RemoveAt(0);
            return id0;
        }

        // Issue identify requests until we hit the concurrency cap. Items already
        // identified (by us or another plugin) are added without a request.
        private void PumpQueue()
        {
            bool added = false;

            while (IdentifyQueue.Count > 0 && PendingIds.Count < MaxConcurrentRequests)
            {
                int id = DequeueNext();

                WorldObject wo = CoreManager.Current.WorldFilter[id];
                if (wo == null) continue;                                            // can't appraise it right now; leave its row as-is
                if (Items.Any(t => t.Id == id && t.IsIdentified)) continue;          // already filled in (stubs don't block)
                if (PendingIds.ContainsKey(id)) continue;

                // Already identified — fill the stub in now, no request needed
                if (wo.HasIdData)
                {
                    AddFromWorldObject(wo);
                    added = true;
                    continue;
                }

                SendId(id);
            }

            if (added) MaybeRefresh();
            UpdateProcessingState();
        }

        // Send an identify request and remember when, for timeout/retry in Tick().
        private void SendId(int id)
        {
            PendingIds[id] = DateTime.UtcNow;
            CoreManager.Current.Actions.RequestId(id);
        }

        // An identification arrived (forwarded from PluginCore's ChangeObject).
        // Handles single Adds and the Add All queue uniformly.
        public void IdentReceived(WorldObject changed)
        {
            if (changed == null) return;

            bool wasPending = PendingIds.Remove(changed.Id);
            bool wasQueued = IdentifyQueue.Remove(changed.Id);

            // Act on any appraisal for a row we're still showing as a stub — even one whose
            // request already gave up, or that we never requested (e.g. the user clicked the
            // item in-game). This fills it the instant the data lands instead of waiting for
            // the next Tick self-heal. Ignore appraisals for items not in this list.
            Item existing = Items.FirstOrDefault(t => t.Id == changed.Id);
            if (!wasPending && !wasQueued && (existing == null || existing.IsIdentified)) return;

            // Fill the stub in place. We don't remove it here — an item earns its
            // spot when added (already pre-filtered) and keeps it while details load.
            AddFromWorldObject(changed);

            PumpQueue();    // refill the freed slot
            MaybeRefresh();
        }

        // Called once per second from the plugin tick. Drives the list to converge: fills
        // stubs whose data arrived, drops timed-out requests, and re-queues any row that's
        // still a stub so it gets requested again. We don't permanently give up on a present
        // item — that's what used to leave grey rows after the "identifying" count hit 0.
        public void Tick()
        {
            // Self-heal: fill any stub whose appraisal data is already available. Covers
            // identify responses that never reached us as a matching IdentReceived event —
            // notably trade items, where the appraisal can land before the row is queued.
            PopulateReadyStubs();

            // Drop timed-out in-flight requests so they can be re-issued below.
            if (PendingIds.Count > 0)
            {
                DateTime now = DateTime.UtcNow;
                List<int> timedOut = null;
                foreach (var kvp in PendingIds)
                {
                    if (now - kvp.Value < IdTimeout) continue;
                    (timedOut ?? (timedOut = new List<int>())).Add(kvp.Key);
                }
                if (timedOut != null)
                    foreach (int id in timedOut) PendingIds.Remove(id);
            }

            // Re-queue any present, still-unidentified row that isn't already in flight or
            // queued, so the list keeps converging instead of leaving stuck grey rows. Items
            // that have left the world (null WorldObject) can't be appraised, so we skip them.
            foreach (Item item in Items)
            {
                if (item.IsIdentified) continue;
                if (PendingIds.ContainsKey(item.Id) || IdentifyQueue.Contains(item.Id)) continue;

                WorldObject wo = CoreManager.Current.WorldFilter[item.Id];
                if (wo == null || wo.HasIdData) continue;   // gone, or PopulateReadyStubs already handled it

                IdentifyQueue.Add(item.Id);
            }

            PumpQueue();
            MaybeRefresh();
        }

        // Fill in any stub whose WorldObject already carries appraisal data. This makes the
        // list converge even when we don't see a matching IdentReceived event for an id we
        // requested (the identify pipeline otherwise relies on that event to fill stubs).
        private void PopulateReadyStubs()
        {
            bool added = false;

            foreach (Item item in Items)
            {
                if (item.IsIdentified) continue;

                WorldObject wo = CoreManager.Current.WorldFilter[item.Id];
                if (wo == null) continue;

                // Fill once we have the appraisal, or once we can tell it never needs one
                // (its ObjectClass may not have loaded when the stub was first created).
                if (!wo.HasIdData && !NeedsNoAppraisal(wo)) continue;

                Fill(item, wo);
                PendingIds.Remove(item.Id);
                IdentifyQueue.Remove(item.Id);
                added = true;
            }

            if (added)
            {
                PumpQueue();        // free slots may let queued ids go out
                MaybeRefresh();
            }
        }

        // True while there's outstanding identify work; flips the "Adding..." button
        // back and fires OnQueueFinished when the last request resolves.
        private void UpdateProcessingState()
        {
            bool active = PendingIds.Count > 0 || IdentifyQueue.Count > 0;

            if (active)
            {
                IsProcessingQueue = true;
            }
            else if (IsProcessingQueue)
            {
                IsProcessingQueue = false;
                RefreshList();          // final sorted repaint
                OnQueueFinished?.Invoke();
            }
        }

        // Repaint now. Does NOT re-sort — rows keep their position so details fill
        // in place without the list shuffling. Sorting is explicit only: once when
        // Add All builds the list, or when the user clicks a column header.
        private void RefreshList()
        {
            _lastRefresh = DateTime.UtcNow;
            OnItemsListChanged?.Invoke();
        }

        // Repaint, at most once per RefreshInterval, so a bulk identify doesn't
        // rebuild the whole list on every single item.
        private void MaybeRefresh()
        {
            if (DateTime.UtcNow - _lastRefresh < RefreshInterval) return;
            RefreshList();
        }

        public void CancelQueue()
        {
            IdentifyQueue.Clear();
            PendingIds.Clear();

            if (IsProcessingQueue)
            {
                IsProcessingQueue = false;
                RefreshList();
                OnQueueFinished?.Invoke();
            }
        }

        // Add an already-identified item, or fill in its existing stub row.
        private void AddFromWorldObject(WorldObject wo)
        {
            Item item = Items.FirstOrDefault(t => t.Id == wo.Id);
            if (item == null)
            {
                item = new Item { Id = wo.Id };
                Items.Add(item);
            }
            Fill(item, wo);
        }

        // Populate an item from its appraisal and remember it in the short-lived cache, so a
        // trade window reopened shortly after can reuse it instead of re-identifying.
        private static void Fill(Item item, WorldObject wo)
        {
            item.Populate(wo);
            ItemCache.Store(item.Id, item, wo.Name);
        }

        // Add a placeholder row carrying the base data available before ID.
        private void AddStub(WorldObject wo)
        {
            if (Items.Any(t => t.Id == wo.Id)) return;

            Item item = new Item { Id = wo.Id };
            item.PopulateStub(wo);
            Items.Add(item);
        }

        private static bool IsEmpty(string s) => string.IsNullOrEmpty(s);

        // Toggle a column header: descending if we're already sorted ascending on it, else
        // ascending. Lets the views' header-click handlers be one-liners.
        public void ToggleSort(SortType ascending, SortType descending)
        {
            Sort(CurrentSortType == ascending ? descending : ascending);
        }

        // Col3 packs both attack and melee-defense mods, so its header cycles through four
        // states: attack asc -> attack desc -> melee asc -> melee desc -> (back to attack asc).
        public void CycleCol3Sort()
        {
            switch (CurrentSortType)
            {
                case SortType.Col3AttackAscending: Sort(SortType.Col3AttackDescending); break;
                case SortType.Col3AttackDescending: Sort(SortType.Col3MeleeAscending); break;
                case SortType.Col3MeleeAscending: Sort(SortType.Col3MeleeDescending); break;
                default: Sort(SortType.Col3AttackAscending); break;
            }
        }

        public void Sort(SortType sortType)
        {
            CurrentSortType = sortType;
            switch (sortType)
            {
                case SortType.NameAscending:
                    Items = Items.OrderBy(t => t.Name).ToList();
                    break;
                case SortType.NameDescending:
                    Items = Items.OrderByDescending(t => t.Name).ToList();
                    break;
                case SortType.Col1Ascending:
                    Items = Items.OrderBy(t => IsEmpty(t.SummaryCol1)).ThenBy(t => t.SummaryCol1).ThenBy(t => t.Name).ToList();
                    break;
                case SortType.Col1Descending:
                    Items = Items.OrderBy(t => IsEmpty(t.SummaryCol1)).ThenByDescending(t => t.SummaryCol1).ThenBy(t => t.Name).ToList();
                    break;
                case SortType.Col2Ascending:
                    Items = Items.OrderBy(t => IsEmpty(t.SummaryCol2)).ThenBy(t => t.SortCategory).ThenBy(t => t.SortCol2).ThenBy(t => t.SummaryCol2).ThenBy(t => t.Name).ToList();
                    break;
                case SortType.Col2Descending:
                    Items = Items.OrderBy(t => IsEmpty(t.SummaryCol2)).ThenBy(t => t.SortCategory).ThenByDescending(t => t.SortCol2).ThenByDescending(t => t.SummaryCol2).ThenBy(t => t.Name).ToList();
                    break;
                case SortType.Col3AttackAscending:
                    Items = Items.OrderBy(t => IsEmpty(t.SummaryCol3)).ThenBy(t => t.SortCategory).ThenBy(t => t.SortCol3).ThenBy(t => t.SummaryCol3).ThenBy(t => t.Name).ToList();
                    break;
                case SortType.Col3AttackDescending:
                    Items = Items.OrderBy(t => IsEmpty(t.SummaryCol3)).ThenBy(t => t.SortCategory).ThenByDescending(t => t.SortCol3).ThenByDescending(t => t.SummaryCol3).ThenBy(t => t.Name).ToList();
                    break;
                case SortType.Col3MeleeAscending:
                    Items = Items.OrderBy(t => IsEmpty(t.SummaryCol3)).ThenBy(t => t.SortCategory).ThenBy(t => t.SortCol3Melee).ThenBy(t => t.SummaryCol3).ThenBy(t => t.Name).ToList();
                    break;
                case SortType.Col3MeleeDescending:
                    Items = Items.OrderBy(t => IsEmpty(t.SummaryCol3)).ThenBy(t => t.SortCategory).ThenByDescending(t => t.SortCol3Melee).ThenByDescending(t => t.SummaryCol3).ThenBy(t => t.Name).ToList();
                    break;
                case SortType.Col4Ascending:
                    Items = Items.OrderBy(t => IsEmpty(t.SummaryCol4)).ThenBy(t => t.SortCategory).ThenBy(t => t.SortCol4).ThenBy(t => t.SummaryCol4).ThenBy(t => t.Name).ToList();
                    break;
                case SortType.Col4Descending:
                    Items = Items.OrderBy(t => IsEmpty(t.SummaryCol4)).ThenBy(t => t.SortCategory).ThenByDescending(t => t.SortCol4).ThenByDescending(t => t.SummaryCol4).ThenBy(t => t.Name).ToList();
                    break;
            }
        }

        public void Remove(int id)
        {
            // Also pull it from the identify pipeline so a pending appraisal can't
            // re-add the row after it's been removed.
            Items.RemoveAll(t => t.Id == id);
            IdentifyQueue.Remove(id);
            PendingIds.Remove(id);
        }

        public void Clear()
        {
            // Reset the whole pipeline, not just the visible rows — otherwise stale
            // queued/in-flight ids survive and re-add rows on the next Add All.
            Items.Clear();
            IdentifyQueue.Clear();
            PendingIds.Clear();
            PriorityIds.Clear();

            if (IsProcessingQueue)
            {
                IsProcessingQueue = false;
                OnQueueFinished?.Invoke();
            }
        }

    }
}
