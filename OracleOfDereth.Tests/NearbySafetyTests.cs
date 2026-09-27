using System;
using System.Collections;
using System.Reflection;
using System.Runtime.Serialization;
using System.Xml;
using Decal.Adapter.Wrappers;
using OracleOfDereth;

internal static class NearbySafetyTests
{
    private const BindingFlags HiddenStatic = BindingFlags.NonPublic | BindingFlags.Static;

    public static void Run()
    {
        var policy = typeof(WorldObjectVisibility).GetMethod("ShouldCheckOwnership", HiddenStatic);
        foreach (ObjectClass category in Enum.GetValues(typeof(ObjectClass)))
            foreach (bool summons in new[] { false, true })
                foreach (bool pets in new[] { false, true })
                {
                    bool expected = category == ObjectClass.Monster ? summons : category == ObjectClass.Npc && pets;
                    if ((bool)policy.Invoke(null, new object[] { category, summons, pets }) != expected)
                        throw new InvalidOperationException("Ownership checks did not respect the category's opt-in.");
                }

        var documentField = typeof(SettingsFile).GetField("_doc", HiddenStatic);
        object savedDocument = documentField.GetValue(null);
        Setting savedSummons = Setting.DeleteOtherSummons;
        Setting savedPets = Setting.DeleteOtherPets;
        var portal = typeof(Nearby).GetMethod("ChangePortalMode", HiddenStatic);
        var isInPortal = typeof(Nearby).GetProperty("IsInPortal", HiddenStatic);
        var tracked = (IDictionary)typeof(Nearby).GetField("Tracked", HiddenStatic).GetValue(null);
        var isOwned = typeof(WorldObjectVisibility).GetMethod("IsPlayerOwnedCreature", HiddenStatic,
            null, new[] { typeof(WorldObject) }, null);

        // This wrapper has no COM object. Reading its properties would fail: disabled
        // features and portal transitions must return before touching it or CoreManager.
        var staleObject = (WorldObject)FormatterServices.GetUninitializedObject(typeof(WorldObject));
        GC.SuppressFinalize(staleObject);
        try
        {
            var document = new XmlDocument();
            document.LoadXml("<Settings><DeleteOtherSummons>No</DeleteOtherSummons><DeleteOtherPets>No</DeleteOtherPets></Settings>");
            documentField.SetValue(null, document);
            var summonsSetting = new Setting { Key = "DeleteOtherSummons", DefaultValue = "No" };
            var petsSetting = new Setting { Key = "DeleteOtherPets", DefaultValue = "No" };
            Setting.DeleteOtherSummons = summonsSetting;
            Setting.DeleteOtherPets = petsSetting;
            portal.Invoke(null, new object[] { PortalEventType.ExitPortal });
            AssertNotOwned(isOwned, staleObject);
            WorldObjectVisibility.Tick();

            // Uninitialized settings must also fail closed during startup/shutdown.
            Setting.DeleteOtherSummons = null;
            Setting.DeleteOtherPets = null;
            AssertNotOwned(isOwned, staleObject);
            WorldObjectVisibility.Tick();
            Setting.DeleteOtherSummons = summonsSetting;
            Setting.DeleteOtherPets = petsSetting;

            document.SelectSingleNode("/Settings/DeleteOtherSummons").InnerText = "Yes";
            document.SelectSingleNode("/Settings/DeleteOtherPets").InnerText = "Yes";
            tracked.Add(123, Activator.CreateInstance(tracked.GetType().GetGenericArguments()[1], true));
            portal.Invoke(null, new object[] { PortalEventType.EnterPortal });
            if (!(bool)isInPortal.GetValue(null) || tracked.Count != 0)
                throw new InvalidOperationException("Portal entry did not suspend and clear Nearby.");
            Nearby.Tick();
            Nearby.Add(staleObject);
            if (Nearby.Objects.Count != 0)
                throw new InvalidOperationException("Nearby repopulated during a portal transition.");
            AssertNotOwned(isOwned, staleObject);
            WorldObjectVisibility.Tick();

            portal.Invoke(null, new object[] { PortalEventType.ExitPortal });
            if ((bool)isInPortal.GetValue(null))
                throw new InvalidOperationException("Nearby did not resume after portal exit.");
            var lastScan = (DateTime)typeof(Nearby).GetField("lastScanAt", HiddenStatic).GetValue(null);
            if (lastScan != DateTime.MinValue)
                throw new InvalidOperationException("Portal exit did not schedule an immediate landscape reconciliation.");
        }
        finally
        {
            portal.Invoke(null, new object[] { PortalEventType.ExitPortal });
            Setting.DeleteOtherSummons = savedSummons;
            Setting.DeleteOtherPets = savedPets;
            documentField.SetValue(null, savedDocument);
        }
    }

    private static void AssertNotOwned(MethodInfo method, WorldObject creature)
    {
        if ((bool)method.Invoke(null, new object[] { creature }))
            throw new InvalidOperationException("Disabled ownership detection should return unknown/unowned.");
    }
}
