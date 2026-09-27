using Decal.Adapter;
using Decal.Adapter.Wrappers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using VirindiViewService;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("OracleOfDereth.Tests")]

namespace OracleOfDereth
{
    // Manual UI hiding has its own lifetime; it never borrows Screenshot's snapshots.
    internal static class InterfaceVisibility
    {
        private static readonly UiRestoreActions restore = new();
        private static CoreManager core;
        private static Timer watchdog;
        private static int characterId;
        private static bool restoring;
        private static bool inRestore;
        private static bool shutdownRequested;
        private static readonly HashSet<IntPtr> notificationPanels = new();
        private static readonly UiPanelStates panelStates = new();
        // gmPaperDollUI::PostInit looks up 0x100001D5 as its UIElement_Viewport.
        // Hide this child before its parent so the 3D renderer receives the transition.
        private const UIElementType PaperDollViewport = (UIElementType)0x100001D5;
        private static readonly UIElementType[] notifiedElements =
        {
            PaperDollViewport, UIElementType.Chat, UIElementType.FloatChat1,
            UIElementType.FloatChat2, UIElementType.FloatChat3, UIElementType.FloatChat4,
            UIElementType.Examination
        };
        private static readonly UIElementType[] panels =
        {
            PaperDollViewport,
            UIElementType.Chat, UIElementType.FloatChat1, UIElementType.FloatChat2,
            UIElementType.FloatChat3, UIElementType.FloatChat4, UIElementType.Examination,
            UIElementType.Vitals, (UIElementType)0x100006D5, UIElementType.EnvPack,
            UIElementType.Panels, UIElementType.TBar, UIElementType.Indicators,
            UIElementType.ProgressBar, UIElementType.Combat, UIElementType.Radar
        };

        public static void Hide()
        {
            if (inRestore || shutdownRequested || restore.Count != 0) return;
            notificationPanels.Clear();
            if (Screenshot.IsPending) return;
            try
            {
                core = CoreManager.Current;
                restoring = false;
                if (core == null || core.CharacterFilter.LoginStatus < 1 || Nearby.IsInPortal)
                    throw new InvalidOperationException("Enter the game before hiding the UI.");
                ValidateClient();
                // Install recovery before changing anything, including partial failures.
                core.WindowMessage += OnWindowMessage;
                core.CharacterFilter.Logoff += OnLogoff;
                Service.DeviceLost += OnDeviceLost;
                watchdog = new Timer { Interval = 250 };
                watchdog.Tick += OnWatchdog;
                watchdog.Start();
                characterId = core.CharacterFilter.Id;
                restore.Add(RestoreClientPanels);
                HideClientPanels();
                HideOverlays();
            }
            catch (Exception ex)
            {
                Show();
                Util.Chat("Could not hide the UI: " + ex.Message, Util.ColorPink);
            }
        }

        public static void Show()
        {
            restoring = true;
            if (inRestore) return;
            inRestore = true;
            try { restore.Restore(ex => Util.Log(ex)); }
            finally
            {
                try
                {
                    // Normal recovery can retry; unloading must release all callbacks and
                    // closures even if an overlay is already disposed and cannot restore.
                    if (shutdownRequested || restore.Count == 0) ReleaseResources();
                }
                finally { inRestore = false; }
            }
        }

        public static void Shutdown()
        {
            // A shutdown triggered inside a restore callback is completed by Show's finally.
            shutdownRequested = true;
            Show();
        }

        private static void ReleaseResources()
        {
            var oldTimer = watchdog;
            var oldCore = core;
            watchdog = null;
            core = null;
            Cleanup(() => oldTimer?.Stop());
            Cleanup(() => { if (oldTimer != null) oldTimer.Tick -= OnWatchdog; });
            Cleanup(() => oldTimer?.Dispose());
            Cleanup(() => { if (oldCore != null) oldCore.WindowMessage -= OnWindowMessage; });
            Cleanup(() => { if (oldCore != null) oldCore.CharacterFilter.Logoff -= OnLogoff; });
            Cleanup(() => { if (oldCore != null) Service.DeviceLost -= OnDeviceLost; });
            restore.Clear();
            panelStates.Clear();
            notificationPanels.Clear();
            characterId = 0;
            restoring = false;
            shutdownRequested = false;
        }

        private static void Cleanup(Action action)
        {
            try { action(); }
            catch (Exception ex) { Util.Log(ex); }
        }

        public static void TakeScreenshot(bool vista)
        {
            if (inRestore || shutdownRequested) return;
            Show();
            if (restore.Count != 0) return;
            if (vista) Screenshot.TakeVista();
            else Screenshot.Take();
        }

        private static void OnLogoff(object sender, LogoffEventArgs e) => Show();
        private static void OnDeviceLost(object sender, EventArgs e) => Show();
        private static void OnWatchdog(object sender, EventArgs e)
        {
            if (!ReferenceEquals(sender, watchdog)) return;
            try
            {
                if (restoring || core == null || core.CharacterFilter.LoginStatus < 1 || core.CharacterFilter.Id != characterId) Show();
                else if (!Nearby.IsInPortal) HideClientPanels();
            }
            catch (Exception ex) { Util.Log(ex); Show(); }
        }

        internal static bool IsEscape(int message, int key) =>
            (message == 0x100 || message == 0x104) && key == (int)Keys.Escape;

        private static void OnWindowMessage(object sender, WindowMessageEventArgs e)
        {
            if (IsEscape(e.Msg, e.WParam))
            {
                Show();
                e.Eat = true;
            }
            // Restore before close, focus loss, or resizing can tear down/rebuild UI objects.
            else if (e.Msg == 0x10 || e.Msg == 0x5 || (e.Msg == 0x1c && e.WParam == 0)) Show();
        }

        private static void ValidateClient()
        {
            using var process = Process.GetCurrentProcess();
            var module = process.MainModule;
            if (IntPtr.Size != 4 || !string.Equals(module.ModuleName, "acclient.exe", StringComparison.OrdinalIgnoreCase)
                || module.BaseAddress != new IntPtr(0x400000) || module.ModuleMemorySize <= 0x2A0E00)
                throw new InvalidOperationException("This client does not support UI hiding.");
            // Retail UIRegion::SetVisible prologue, including its m_Flags offset.
            byte[] expected = { 0x83, 0xec, 0x10, 0x53, 0x56, 0x57, 0x8b, 0xf9, 0x8b, 0x87, 0xa4, 0, 0, 0 };
            for (int i = 0; i < expected.Length; i++)
                if (Marshal.ReadByte(new IntPtr(0x6A0D50 + i)) != expected[i])
                    throw new InvalidOperationException("This client does not support UI hiding.");
            byte[] chatExpected = { 0x51, 0x53, 0x56, 0x57, 0x8b, 0x3d, 0x3c, 0xe0, 0x83, 0x00, 0x8b, 0xf1 };
            for (int i = 0; i < chatExpected.Length; i++)
                if (Marshal.ReadByte(new IntPtr(0x462390 + i)) != chatExpected[i])
                    throw new InvalidOperationException("This client does not support UI visibility notifications.");
        }

        private static void HideClientPanels()
        {
            notificationPanels.Clear();
            foreach (var element in notifiedElements)
            {
                IntPtr address = core.Actions.UIElementLookup(element);
                if (address != IntPtr.Zero) notificationPanels.Add(address);
            }
            panelStates.Hide(panels, element => core.Actions.UIElementLookup(element),
                address => (Marshal.ReadInt32(address, 0xA4) & 2) != 0, SetRegionVisible);
        }

        private static void RestoreClientPanels()
        {
            if (core == null || core.CharacterFilter.LoginStatus < 1 || core.CharacterFilter.Id != characterId)
            {
                panelStates.Clear();
                return;
            }
            panelStates.Restore(element => core.Actions.UIElementLookup(element), SetRegionVisible);
        }

        private static unsafe void SetRegionVisible(IntPtr address, bool visible)
        {
            if (notificationPanels.Contains(address))
            {
                // Chat, the paper doll and examination need their own visibility handler
                // notified, including when an appraisal reopens the examination window.
                var setElementVisible = (delegate* unmanaged[Thiscall]<void*, byte, void>)0x462390;
                setElementVisible((void*)address, visible ? (byte)1 : (byte)0);
                return;
            }
            // Retail UIRegion::SetVisible (UB.Service 3.0.11 entrypoint + client disassembly).
            // Use the base region operation, not UIElement::SetVisible: no top-level UI
            // visibility notification or layout mutation. The transient region dies on logout.
            var setVisible = (delegate* unmanaged[Thiscall]<void*, byte, void>)0x6A0D50;
            setVisible((void*)address, visible ? (byte)1 : (byte)0);
        }

        private static void HideOverlays()
        {
            const BindingFlags hiddenStatic = BindingFlags.NonPublic | BindingFlags.Static;
            var field = typeof(DxHud).GetField("h", hiddenStatic);
            var render = typeof(DxHud).GetMethod("a", hiddenStatic, null, Type.EmptyTypes, null);
            byte[] il = render?.GetMethodBody()?.GetILAsByteArray();
            if (field?.FieldType != typeof(bool) || il == null || il.Length < 8 ||
                il[0] != 0x7e || BitConverter.ToInt32(il, 1) != field.MetadataToken ||
                il[5] != 0x2d || il[6] != 1 || il[7] != 0x2a)
                throw new InvalidOperationException("This Virindi Views version does not support UI hiding.");
            SetField(field, null, false);

            var assembly = LoadedAssemblies.Find("UtilityBelt.Service");
            var manager = assembly?.GetType("UtilityBelt.Service.UBService")
                ?.GetField("Huds", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
            if (manager != null)
            {
                var type = manager.GetType();
                var disabled = type.GetField("DisableAllRendering", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                // Older versions have only didInit; use the same verified guard as vistashot.
                if (disabled?.FieldType == typeof(bool)) SetField(disabled, manager, true);
                else
                {
                    var initialized = type.GetField("didInit", BindingFlags.NonPublic | BindingFlags.Instance);
                    il = type.GetMethod("DoRender", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                        ?.GetMethodBody()?.GetILAsByteArray();
                    int offset = il != null && il.Length >= 8 ? 8 + unchecked((sbyte)il[7]) : -1;
                    if (initialized?.FieldType != typeof(bool) || il == null || il.Length < 8 ||
                        il[0] != 0x02 || il[1] != 0x7b || BitConverter.ToInt32(il, 2) != initialized.MetadataToken ||
                        il[6] != 0x2c || offset < 8 || offset >= il.Length || il[offset] != 0x2a)
                        throw new InvalidOperationException("This UtilityBelt version does not support UI hiding.");
                    SetField(initialized, manager, false);
                }
            }

            var plugins = typeof(CoreManager).GetField("myPlugins", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.GetValue(core) as Dictionary<string, PluginBase>;
            if (plugins == null) return;
            foreach (var plugin in plugins.Values)
            {
                if (plugin.GetType().FullName != "GoArrow.PluginCore") continue;
                var arrow = plugin.GetType().GetField("mArrowHud", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(plugin);
                if (arrow == null) continue;
                var alpha = arrow.GetType().GetField("mAlpha", BindingFlags.NonPublic | BindingFlags.Instance);
                if (alpha?.FieldType != typeof(int)) throw new InvalidOperationException("This GoArrow version does not support UI hiding.");
                int original = (int)alpha.GetValue(arrow);
                Action<int> setAlpha = value =>
                {
                    alpha.SetValue(arrow, value);
                    var hud = arrow.GetType().GetField("mHud", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(arrow)
                        as Decal.Adapter.Wrappers.Hud;
                    if (hud != null) hud.Alpha = value;
                };
                restore.Add(() => setAlpha(original));
                setAlpha(0);
            }
        }

        private static void SetField(FieldInfo field, object instance, object value)
        {
            object original = field.GetValue(instance);
            restore.Add(() => field.SetValue(instance, original));
            field.SetValue(instance, value);
        }
    }

    internal sealed class UiRestoreActions
    {
        private readonly List<Action> actions = new();
        private bool running;
        public int Count => actions.Count;
        public void Add(Action action) => actions.Add(action);
        public void Clear() => actions.Clear();
        public void Restore(Action<Exception> report)
        {
            if (running) return;
            running = true;
            try
            {
                for (int i = actions.Count - 1; i >= 0; i--)
                {
                    try { actions[i](); actions.RemoveAt(i); }
                    catch (Exception ex) { report(ex); }
                }
            }
            finally { running = false; }
        }
    }

    // Keeps at most one snapshot per panel across arbitrarily many portal transitions.
    // Callers provide fresh lookups; this bookkeeping never dereferences native pointers.
    internal sealed class UiPanelStates
    {
        private readonly Dictionary<UIElementType, (IntPtr Address, bool Visible)> states = new();
        internal int Count => states.Count;
        internal void Clear() => states.Clear();
        internal void Hide(IEnumerable<UIElementType> elements, Func<UIElementType, IntPtr> lookup,
            Func<IntPtr, bool> isVisible, Action<IntPtr, bool> setVisible)
        {
            int found = 0;
            foreach (var element in elements)
            {
                IntPtr address = lookup(element);
                if (address == IntPtr.Zero) { states.Remove(element); continue; }
                found++;
                bool visible = isVisible(address);
                if (!states.TryGetValue(element, out var saved) || saved.Address != address)
                    states[element] = (address, visible);
                else if (visible && !saved.Visible)
                    states[element] = (address, true);
                if (visible) setVisible(address, false);
            }
            if (found == 0) throw new InvalidOperationException("The game interface is not ready.");
        }

        internal void Restore(Func<UIElementType, IntPtr> lookup, Action<IntPtr, bool> setVisible)
        {
            var errors = new List<Exception>();
            foreach (var entry in new Dictionary<UIElementType, (IntPtr Address, bool Visible)>(states))
            {
                try
                {
                    if (lookup(entry.Key) == entry.Value.Address)
                        setVisible(entry.Value.Address, entry.Value.Visible);
                    states.Remove(entry.Key);
                }
                catch (Exception ex) { errors.Add(ex); }
            }
            if (errors.Count != 0) throw new AggregateException(errors);
        }
    }
}
