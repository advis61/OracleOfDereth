using System;
using System.Collections.Generic;

namespace OracleOfDereth
{
    // Cache of identified item rows, keyed by world id. Lets a trade window closed and reopened
    // in the same spot reuse appraisals instead of re-identifying everything. Kept until we zone
    // somewhere significantly different (portal/recall/dungeon) — the same landblock-jump test
    // the auto-recruit pause uses — so it doesn't follow us across the world or live forever.
    public static class ItemCache
    {
        private struct Entry { public Item Item; public string BaseName; }
        private static readonly Dictionary<int, Entry> Cache = new Dictionary<int, Entry>();

        private static int _lastLandblock = -1;

        // A landblock jump of this many cells or more counts as a zone (portal/recall/dungeon);
        // smaller steps are ordinary walking into an adjacent landblock and keep the cache.
        private const int ZoneJumpThreshold = 2;

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

        // Called each tick: drop the whole cache when we zone somewhere far (teleport/recall),
        // but keep it while walking around or standing still. Mirrors Fellowship's zone detector
        // — compares the landblock (high 16 bits); X/Y are world-grid coords, adjacent blocks
        // differ by 1, so a teleport is a single large jump while walking is distance-1 steps.
        public static void Tick()
        {
            int current = Util.CurrentLandblock();
            if (_lastLandblock == -1) { _lastLandblock = current; return; }
            if (current == _lastLandblock) return;

            int dx = Math.Abs(((current >> 8) & 0xFF) - ((_lastLandblock >> 8) & 0xFF));
            int dy = Math.Abs((current & 0xFF) - (_lastLandblock & 0xFF));
            int distance = Math.Max(dx, dy);

            _lastLandblock = current;

            if (distance >= ZoneJumpThreshold) Clear();
        }
    }
}
