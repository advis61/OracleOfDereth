using System;
using System.Collections.Generic;

namespace OracleOfDereth
{
    // Cache of identified item rows, keyed by world id. Lets a trade window closed and reopened
    // in the same spot reuse appraisals instead of re-identifying everything. Cleared when we zone
    // (portal/recall/dungeon) so it doesn't follow us across the world — PluginCore calls Clear()
    // on the ChangePortalMode event.
    public static class ItemCache
    {
        private struct Entry { public Item Item; public string BaseName; }
        private static readonly Dictionary<int, Entry> Cache = new Dictionary<int, Entry>();

        // Remember an identified item. baseName is the WorldObject's plain name, checked on
        // lookup so a recycled id can't hand back another item's appraisal.
        public static void Store(int id, Item item, string baseName)
        {
            if (item == null || !item.IsIdentified) return;
            Cache[id] = new Entry { Item = item.Clone(), BaseName = baseName ?? "" };
        }

        // A fresh cached copy for this id, or null if missing / a different item.
        public static Item Get(int id, string baseName)
        {
            if (!Cache.TryGetValue(id, out Entry e)) return null;
            if (e.BaseName != (baseName ?? "")) return null;
            return e.Item.Clone();
        }

        public static void Clear() => Cache.Clear();

        // Reset for a fresh character on login (called from PluginCore.Init). Drops any appraisals
        // cached under the previous character, so nothing carries across a character switch.
        // Mirrors the other models' Init()-clears-its-collection pattern.
        public static void Init()
        {
            Cache.Clear();
        }
    }
}
