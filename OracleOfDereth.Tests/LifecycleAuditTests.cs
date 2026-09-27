using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.InteropServices;
using OracleOfDereth;
using OracleOfDereth.Models;

internal static class LifecycleAuditTests
{
    private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;

    public static void Run()
    {
        SelectionsExpireWithoutReplies();
        SearchReleasesFailedEnumerator();
        SessionListsReleaseCallbacks();
        RecruitingListResets();
        DisposedInventoryIgnoresQueuedTick();
        LateResultsCannotRetainOldSession(typeof(QuestSubmit), "SendResult", "pendingResult");
        LateResultsCannotRetainOldSession(typeof(QuestFlagLookup), "LookupResult", "pending");
        FavoritesForgetPreviousServer();
        NativeStringReleasesOnlyOwnedReference();
    }

    private static void SelectionsExpireWithoutReplies()
    {
        // No Decal event source is needed to test expiry and disposal of the managed state.
        var identifier = (WorldObjectIdentifier)FormatterServices.GetUninitializedObject(typeof(WorldObjectIdentifier));
        var selections = new Dictionary<int, DateTime>();
        typeof(WorldObjectIdentifier).GetField("itemsSelected", Hidden).SetValue(identifier, selections);
        DateTime now = DateTime.UtcNow;
        for (int i = 1; i <= 10000; i++) selections[i] = now.AddMinutes(-1);
        selections[10001] = now.AddSeconds(-5);
        identifier.ExpireSelections(now);
        Assert(selections.Count == 1 && selections.ContainsKey(10001), "Unanswered selections accumulated.");
        identifier.ExpireSelections(now.AddSeconds(6));
        Assert(selections.Count == 0, "Selections required an appraisal reply to expire.");
        identifier.Identified += (sender, item) => { };
        selections[1] = now;
        identifier.Dispose();
        identifier.Dispose();
        Assert(selections.Count == 0 && typeof(WorldObjectIdentifier).GetField("Identified", Hidden).GetValue(identifier) == null,
            "Identifier disposal retained selections or callbacks.");
    }

    private static void SearchReleasesFailedEnumerator()
    {
        var inventory = new VGInventory();
        var scan = new FailingScan();
        typeof(VGInventory).GetField("scan", Hidden).SetValue(inventory, scan);
        try { inventory.CancelSearch(); }
        catch (InvalidOperationException ex) when (ex.Message == "dispose failure") { }
        Assert(!inventory.IsSearching, "A failed database cleanup retained the scan.");
        inventory.CancelSearch();
        Assert(scan.Disposals == 1, "Failed scan was disposed repeatedly.");
    }

    private static void SessionListsReleaseCallbacks()
    {
        ItemList.Init();
        var inventory = ItemList.Inventory;
        var trade = ItemList.Trade;
        foreach (var list in new[] { inventory, trade })
        {
            list.IsProcessingQueue = true;
            list.OnQueueFinished = () => throw new InvalidOperationException("Shutdown called a dead view.");
            list.OnItemsListChanged = list.OnQueueFinished;
            list.Items.Add(null);
        }
        ItemList.Shutdown();
        ItemList.Shutdown();
        Assert(ItemList.Inventory == null && ItemList.Trade == null, "Session lists survived shutdown.");
        foreach (var list in new[] { inventory, trade })
            Assert(list.Items.Count == 0 && !list.IsProcessingQueue && list.OnQueueFinished == null && list.OnItemsListChanged == null,
                "List cleanup retained rows, pending work, or callbacks.");
        ItemList.Init();
    }

    private static void RecruitingListResets()
    {
        ConquestFship.NoteChat("- Example (Leader: Someone) [1/9] @ Town");
        var request = (ChatRequest)typeof(ConquestFship).GetField("Request", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
        request.Sent();
        Assert(ConquestFship.All.Count > 0 && request.Awaiting, "Recruiting test did not populate state.");
        ConquestFship.Init();
        Assert(ConquestFship.All.Count == 0 && !request.Awaiting, "Previous character's recruiting state survived reset.");
    }

    private static void DisposedInventoryIgnoresQueuedTick()
    {
        Type type = typeof(ItemList).Assembly.GetType("OracleOfDereth.MainView", true);
        object view = FormatterServices.GetUninitializedObject(type);
        // Controls are absent: a stale timer callback must return before touching them.
        type.GetMethod("VGInventorySearchTick", Hidden).Invoke(view, new object[] { new object(), EventArgs.Empty });
    }

    private static void FavoritesForgetPreviousServer()
    {
        const BindingFlags flags = BindingFlags.Static | BindingFlags.NonPublic;
        typeof(QuestFavorite).GetField("flags", flags).SetValue(null, new List<string> { "old-server" });
        typeof(QuestFavorite).GetField("filePath", flags).SetValue(null, "old-server-favorites.csv");
        QuestFavorite.Reset();
        Assert(typeof(QuestFavorite).GetField("flags", flags).GetValue(null) == null &&
            typeof(QuestFavorite).GetField("filePath", flags).GetValue(null) == null,
            "Favorites retained a previous server's file path.");
    }

    private static void LateResultsCannotRetainOldSession(Type type, string resultName, string pendingName)
    {
        const BindingFlags flags = BindingFlags.Static | BindingFlags.NonPublic;
        var generation = type.GetField("generation", flags);
        var pending = type.GetField(pendingName, flags);
        var publish = type.GetMethod("PublishResult", flags);
        var shutdown = type.GetMethod("Shutdown");
        var resultType = type.GetNestedType(resultName, BindingFlags.NonPublic);
        object oldResult = Activator.CreateInstance(resultType);
        resultType.GetField("Generation").SetValue(oldResult, generation.GetValue(null));
        publish.Invoke(null, new[] { oldResult });
        Assert(ReferenceEquals(pending.GetValue(null), oldResult), "Active result was not published.");
        shutdown.Invoke(null, null);
        publish.Invoke(null, new[] { oldResult });
        Assert(pending.GetValue(null) == null, "A late result resurrected shutdown state.");
        object currentResult = Activator.CreateInstance(resultType);
        resultType.GetField("Generation").SetValue(currentResult, generation.GetValue(null));
        publish.Invoke(null, new[] { currentResult });
        publish.Invoke(null, new[] { oldResult });
        Assert(ReferenceEquals(pending.GetValue(null), currentResult), "An old worker overwrote the new session's result.");
        shutdown.Invoke(null, null);
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
    private delegate IntPtr DestroyBuffer(IntPtr buffer, uint flags);

    private static unsafe void NativeStringReleasesOnlyOwnedReference()
    {
        IntPtr buffer = Marshal.AllocHGlobal(8);
        IntPtr vtable = Marshal.AllocHGlobal(IntPtr.Size);
        int destructions = 0;
        DestroyBuffer destroy = (address, flags) =>
        {
            Assert(address == buffer && flags == 1, "Native string used the wrong destructor arguments.");
            destructions++;
            return IntPtr.Zero;
        };
        try
        {
            Marshal.WriteIntPtr(vtable, Marshal.GetFunctionPointerForDelegate(destroy));
            Marshal.WriteIntPtr(buffer, vtable);
            Marshal.WriteInt32(buffer, 4, 2);
            var first = new AcClient.PStringBase<char> { m_buffer = (AcClient.PSRefBuffer<char>*)buffer };
            var second = first; // Represents a second owner, accounted for in the count above.
            first.ReleaseOwnedBuffer();
            Assert(first.m_buffer == null && Marshal.ReadInt32(buffer, 4) == 1 && destructions == 0,
                "Releasing one string owner destroyed a shared buffer.");
            first.ReleaseOwnedBuffer();
            second.ReleaseOwnedBuffer();
            second.ReleaseOwnedBuffer();
            Assert(destructions == 1 && second.m_buffer == null, "Native buffer was leaked or released twice.");
        }
        finally
        {
            GC.KeepAlive(destroy);
            Marshal.FreeHGlobal(buffer);
            Marshal.FreeHGlobal(vtable);
        }
    }

    private sealed class FailingScan : IEnumerator<bool>
    {
        public int Disposals;
        public bool Current => false;
        object System.Collections.IEnumerator.Current => Current;
        public bool MoveNext() => false;
        public void Reset() { }
        public void Dispose() { Disposals++; throw new InvalidOperationException("dispose failure"); }
    }
}
