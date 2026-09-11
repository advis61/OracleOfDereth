using OracleOfDereth;
using System;
using System.Collections;
using System.Reflection;

internal static class ScreenshotLifecycleTests
{
    public static void Run()
    {
        const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Static;
        var capture = typeof(Screenshot).GetMethod("Capture", flags);
        var timerField = typeof(Screenshot).GetField("timer", flags);
        var coreField = typeof(Screenshot).GetField("core", flags);
        var panelsField = typeof(Screenshot).GetField("clientPanels", flags);
        var panels = (IList)panelsField.GetValue(null);
        // No game session exists: stale callbacks must return without touching Decal.
        capture.Invoke(null, new object[] { new object(), EventArgs.Empty });
        var currentTimer = (IDisposable)Activator.CreateInstance(timerField.FieldType);
        try
        {
            timerField.SetValue(null, currentTimer);
            capture.Invoke(null, new object[] { new object(), EventArgs.Empty });
            if (!ReferenceEquals(timerField.GetValue(null), currentTimer))
                throw new InvalidOperationException("An old capture callback cancelled the newer screenshot.");

            Type panelType = panelsField.FieldType.GetGenericArguments()[0];
            Type elementType = panelType.GetGenericArguments()[0];
            panels.Add(Activator.CreateInstance(panelType, new object[]
                { Enum.ToObject(elementType, 0), new IntPtr(123), new System.Drawing.Point(10, 20), (uint)1 }));
            coreField.SetValue(null, null);
            typeof(Screenshot).GetMethod("RestoreClientUi", flags).Invoke(null, null);
            if (panels.Count != 0)
                throw new InvalidOperationException("Cleanup retained panel pointers after the game session disappeared.");
        }
        finally
        {
            timerField.SetValue(null, null);
            panels.Clear();
            currentTimer.Dispose();
        }
    }
}
