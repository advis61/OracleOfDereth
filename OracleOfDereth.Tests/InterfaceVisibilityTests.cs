using System;
using System.Collections.Generic;
using Decal.Adapter.Wrappers;
using OracleOfDereth;

internal static class InterfaceVisibilityTests
{
    public static void Run()
    {
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

        Assert(InterfaceVisibility.IsEscape(0x100, 27) && InterfaceVisibility.IsEscape(0x104, 27), "Escape recovery was not recognized.");
        Assert(!InterfaceVisibility.IsEscape(0x101, 27) && !InterfaceVisibility.IsEscape(0x100, 65), "Recovery intercepted an unrelated key.");
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
