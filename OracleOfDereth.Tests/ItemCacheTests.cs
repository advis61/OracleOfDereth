using System;
using Decal.Adapter.Wrappers;
using OracleOfDereth;

internal static class ItemCacheTests
{
    public static void Run()
    {
        ItemCache.Init();
        try
        {
            for (int id = 1; id <= ItemCache.MaxEntries; id++) ItemCache.Store(Row(id));
            Check(ItemCache.Count == ItemCache.MaxEntries, "Cache filled to an unexpected size.");

            // Reads and replacement appraisals keep useful entries; a failed name match
            // must not keep a stale, recycled object ID alive.
            Check(ItemCache.Get(1, "Item 1") != null, "Cache missed a stored appraisal.");
            ItemCache.Store(Row(2, "Replacement"));
            Check(ItemCache.Get(3, "Wrong name") == null, "Cache accepted a recycled ID.");
            ItemCache.Store(Row(ItemCache.MaxEntries + 1));
            Check(ItemCache.Count == ItemCache.MaxEntries, "Cache exceeded its entry limit.");
            Check(ItemCache.Get(3, "Item 3") == null, "Least recently used appraisal was not evicted.");
            Check(ItemCache.Get(1, "Item 1") != null, "Recently read appraisal was evicted.");
            Check(ItemCache.Get(2, "Replacement") != null, "Replacement appraisal was evicted.");
            Check(ItemCache.Get(2, "Item 2") == null, "Replacement retained its old name.");

            // Invalid stores must neither evict useful rows nor replace complete data.
            var incomplete = Row(2, "Incomplete");
            incomplete.PopulateStub();
            ItemCache.Store(incomplete);
            ItemCache.Store(null);
            Check(ItemCache.Count == ItemCache.MaxEntries && ItemCache.Get(2, "Replacement") != null,
                "An incomplete appraisal displaced complete cached data.");

            // Returned rows remain independent of the caller and of the stored row.
            var cached = ItemCache.Get(2, "Replacement");
            cached.PopulateStub();
            Check(ItemCache.Get(2, "Replacement").IsComplete, "A caller mutated the cached row.");

            for (int id = ItemCache.MaxEntries + 2; id <= ItemCache.MaxEntries * 3; id++)
            {
                ItemCache.Store(Row(id));
                Check(ItemCache.Count == ItemCache.MaxEntries, "Cache grew during a long session.");
            }
            Check(ItemCache.Get(1, "Item 1") == null, "Old appraisals survived repeated eviction.");

            ItemCache.Clear();
            Check(ItemCache.Count == 0, "Portal cleanup retained appraisals.");
            // Refill after Clear to exercise recency cleanup as well as dictionary cleanup.
            for (int id = 1; id <= ItemCache.MaxEntries + 1; id++) ItemCache.Store(Row(id));
            Check(ItemCache.Get(1, "Item 1") == null && ItemCache.Get(2, "Item 2") != null,
                "Portal cleanup left stale recency entries.");
            ItemCache.Init();
            Check(ItemCache.Count == 0, "Login reset retained appraisals.");
        }
        finally { ItemCache.Clear(); }
    }

    private static ItemListRow Row(int id, string name = null)
    {
        var row = new ItemListRow(new Item("Test", "Character", id, name ?? "Item " + id,
            ObjectClass.SpellComponent, hasIdData: true));
        row.Populate();
        return row;
    }

    private static void Check(bool success, string message)
    {
        if (!success) throw new InvalidOperationException(message);
    }
}
