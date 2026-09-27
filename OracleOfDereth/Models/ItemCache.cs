using System.Collections.Generic;

namespace OracleOfDereth
{
    // Cache of identified item rows, keyed by world id. Lets a trade window closed and reopened
    // in the same spot reuse appraisals instead of re-identifying everything. Cleared when we zone
    // (portal/recall/dungeon) so it doesn't follow us across the world — PluginCore calls Clear()
    // on the ChangePortalMode event.
    public static class ItemCache
    {
        public const int MaxEntries = 1000;
        public static int Count => Cache.Count;

        private sealed class Entry
        {
            public ItemListRow Item;
            public string BaseName;
            public LinkedListNode<int> Recency;
        }
        private static readonly Dictionary<int, Entry> Cache = new Dictionary<int, Entry>();
        // Oldest use first. Store and successful lookup both refresh recency in O(1).
        private static readonly LinkedList<int> RecentlyUsed = new LinkedList<int>();

        // Remember a complete row. Its captured plain name is checked on
        // lookup so a recycled id can't hand back another item's appraisal.
        public static void Store(ItemListRow row)
        {
            if (row == null || !row.IsComplete) return;
            ItemListRow copy = row.Clone();
            if (Cache.TryGetValue(row.Id, out Entry existing))
            {
                existing.Item = copy;
                existing.BaseName = row.Item.Name;
                Touch(existing);
                return;
            }

            if (Cache.Count >= MaxEntries)
            {
                Cache.Remove(RecentlyUsed.First.Value);
                RecentlyUsed.RemoveFirst();
            }
            Cache[row.Id] = new Entry
            {
                Item = copy,
                BaseName = row.Item.Name,
                Recency = RecentlyUsed.AddLast(row.Id)
            };
        }

        // A fresh cached copy for this id, or null if missing / a different item.
        public static ItemListRow Get(int id, string baseName)
        {
            if (!Cache.TryGetValue(id, out Entry e)) return null;
            if (e.BaseName != (baseName ?? "")) return null;
            Touch(e);
            return e.Item.Clone();
        }

        private static void Touch(Entry entry)
        {
            RecentlyUsed.Remove(entry.Recency);
            RecentlyUsed.AddLast(entry.Recency);
        }

        public static void Clear()
        {
            Cache.Clear();
            RecentlyUsed.Clear();
        }

        // Reset for a fresh character on login (called from PluginCore.Init). Drops any appraisals
        // cached under the previous character, so nothing carries across a character switch.
        // Mirrors the other models' Init()-clears-its-collection pattern.
        public static void Init()
        {
            Clear();
        }
    }
}
