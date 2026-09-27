using System;
using System.Collections.Generic;
using System.Reflection;
using Decal.Adapter.Wrappers;
using OracleOfDereth;

internal static class InterfaceVisibilityTests
{
    private delegate void RadarTimeout(object timer);
    private sealed class RadarTimer
    {
        public event RadarTimeout Timeout;
        public void Tick() => Timeout?.Invoke(this);
    }

    public static void Run()
    {
        // A redraw after RenderFrame must still end with the HUD hidden. Exercise
        // the reflection/delegate path used for the optional Radar COM timer.
        var radarTimer = new RadarTimer();
        bool radarVisible = false;
        radarTimer.Timeout += timer => radarVisible = true;
        var timeout = typeof(RadarTimer).GetEvent("Timeout");
        var callback = InterfaceVisibility.CreateCallback(timeout.EventHandlerType, () => radarVisible = false);
        timeout.AddEventHandler(radarTimer, callback);
        radarTimer.Tick();
        Assert(!radarVisible, "Radar redraw escaped UI hiding.");
        timeout.RemoveEventHandler(radarTimer, callback);
        radarTimer.Tick();
        Assert(radarVisible, "Radar hide callback remained attached after restoration.");

        var panels = new UiPanelStates();
        var elements = new[] { UIElementType.Chat, UIElementType.Radar };
        var addresses = new Dictionary<UIElementType, IntPtr>
        {
            [elements[0]] = new IntPtr(1), [elements[1]] = new IntPtr(2)
        };
        var visible = new Dictionary<IntPtr, bool>
        {
            [new IntPtr(1)] = true, [new IntPtr(2)] = false
        };
        Func<UIElementType, IntPtr> lookup = element => addresses[element];
        Action<IntPtr, bool> set = (address, value) => visible[address] = value;
        Action hide = () => panels.Hide(elements, lookup, address => visible[address], set);
        hide();
        hide();
        panels.Restore(lookup, set);
        Assert(visible[new IntPtr(1)] && !visible[new IntPtr(2)], "Repeated hiding overwrote the original state.");
        Assert(panels.Count == 0, "Restored panel snapshots were retained.");

        // An appraisal can reopen the same inspection panel while manual hiding is active.
        var inspection = new UiPanelStates();
        var inspectionElements = new[] { UIElementType.Examination };
        bool inspectionVisible = false;
        int inspectionHides = 0;
        Action hideInspection = () => inspection.Hide(inspectionElements, element => new IntPtr(300),
            address => inspectionVisible, (address, value) => { inspectionVisible = value; inspectionHides++; });
        hideInspection();
        inspectionVisible = true;
        hideInspection();
        inspectionVisible = true;
        hideInspection();
        Assert(!inspectionVisible && inspectionHides == 2 && inspection.Count == 1,
            "Repeated inspections were not rehidden or duplicated their snapshot.");
        inspection.Restore(element => new IntPtr(300), (address, value) => inspectionVisible = value);
        Assert(inspectionVisible, "Restoring the UI lost the newly opened inspection window.");

        hide();
        // Simulate many portals rebuilding a panel. Old pointers must never be read again.
        for (int i = 3; i < 103; i++)
        {
            visible.Remove(addresses[elements[0]]);
            addresses[elements[0]] = new IntPtr(i);
            visible[new IntPtr(i)] = true;
            hide();
            Assert(panels.Count == 2, "Portal transitions grew the snapshot collection.");
            Assert(!visible[new IntPtr(i)], "A replacement panel was not hidden.");
        }
        // A previously closed panel opened while hidden should be restored open.
        visible[new IntPtr(2)] = true;
        hide();
        panels.Restore(lookup, set);
        Assert(visible[new IntPtr(102)] && visible[new IntPtr(2)], "Arrival or newly opened panel state was lost.");

        hide();
        addresses[elements[0]] = new IntPtr(200); // Replacement just before restoring.
        panels.Restore(lookup, (address, value) =>
        {
            Assert(address != new IntPtr(102), "Restore touched an obsolete native pointer.");
            set(address, value);
        });
        Assert(panels.Count == 0, "Obsolete snapshots were retained.");

        visible[new IntPtr(200)] = true;
        hide();
        bool fail = true;
        Action<IntPtr, bool> failingSet = (address, value) =>
        {
            if (address == new IntPtr(200) && fail) throw new InvalidOperationException("simulated failure");
            set(address, value);
        };
        try { panels.Restore(lookup, failingSet); throw new Exception("Expected restore failure."); }
        catch (AggregateException) { }
        Assert(visible[new IntPtr(2)] && panels.Count == 1, "One failure blocked other panels or lost retry state.");
        fail = false;
        panels.Restore(lookup, failingSet);
        Assert(visible[new IntPtr(200)] && panels.Count == 0, "A failed panel could not be restored on retry.");

        var undo = new UiRestoreActions();
        int restored = 0;
        int errors = 0;
        fail = true;
        undo.Add(() => restored++);
        undo.Add(() => { if (fail) throw new Exception("simulated overlay failure"); restored++; });
        undo.Add(() => restored++);
        undo.Restore(ex => errors++);
        Assert(restored == 2 && errors == 1 && undo.Count == 1, "Overlay cleanup did not isolate failures.");
        fail = false;
        undo.Restore(ex => errors++);
        undo.Restore(ex => errors++);
        Assert(restored == 3 && undo.Count == 0, "Restore repeated an already completed action.");

        int reentries = 0;
        undo.Add(() => { reentries++; undo.Restore(ex => errors++); });
        undo.Restore(ex => errors++);
        Assert(reentries == 1 && undo.Count == 0, "Nested restore ran an action recursively.");
        AssertShutdownCleanup();

        Assert(InterfaceVisibility.IsEscape(0x100, 27) && InterfaceVisibility.IsEscape(0x104, 27), "Escape recovery was not recognized.");
        Assert(!InterfaceVisibility.IsEscape(0x101, 27) && !InterfaceVisibility.IsEscape(0x100, 65), "Recovery intercepted an unrelated key.");
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static void AssertShutdownCleanup()
    {
        const BindingFlags flags = BindingFlags.Static | BindingFlags.NonPublic;
        var type = typeof(InterfaceVisibility);
        var actions = (UiRestoreActions)type.GetField("restore", flags).GetValue(null);
        var snapshots = (UiPanelStates)type.GetField("panelStates", flags).GetValue(null);
        var pointers = (HashSet<IntPtr>)type.GetField("notificationPanels", flags).GetValue(null);
        var timerField = type.GetField("watchdog", flags);
        var timer = new System.Windows.Forms.Timer { Interval = 250 };
        try
        {
            timer.Start();
            timerField.SetValue(null, timer);
            snapshots.Hide(new[] { UIElementType.Chat }, element => new IntPtr(123), address => true,
                (address, value) => { });
            pointers.Add(new IntPtr(123));
            int calls = 0;
            actions.Add(() =>
            {
                calls++;
                if (calls > 1) throw new Exception("Restore recursed.");
                InterfaceVisibility.Show();
                throw new InvalidOperationException("Simulated disposed overlay.");
            });
            InterfaceVisibility.Show();
            Assert(calls == 1 && actions.Count == 1 && ReferenceEquals(timerField.GetValue(null), timer),
                "Normal failed restoration lost its retry state or reentered cleanup.");

            int otherRestored = 0;
            actions.Add(() => otherRestored++);
            InterfaceVisibility.Shutdown();
            Assert(otherRestored == 1 && actions.Count == 0, "Shutdown retained a failed closure or skipped another restore.");
            Assert(timerField.GetValue(null) == null && !timer.Enabled && snapshots.Count == 0 && pointers.Count == 0,
                "Shutdown retained its timer or native snapshots after a restore failure.");
            type.GetMethod("OnWatchdog", flags).Invoke(null, new object[] { timer, EventArgs.Empty });

            // Shutdown may arrive synchronously from a native visibility callback.
            actions.Add(() => otherRestored++);
            actions.Add(() =>
            {
                InterfaceVisibility.Shutdown();
                Assert(actions.Count == 2, "Nested shutdown cleared the active restore iteration.");
            });
            InterfaceVisibility.Show();
            Assert(otherRestored == 2 && actions.Count == 0, "Deferred shutdown skipped an outstanding restore.");
            Assert(!(bool)type.GetField("inRestore", flags).GetValue(null), "The restoration guard stayed locked.");
            InterfaceVisibility.Shutdown();
        }
        finally
        {
            actions.Clear();
            InterfaceVisibility.Shutdown();
            timer.Dispose();
        }
    }
}
