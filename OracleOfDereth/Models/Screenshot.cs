using Decal.Adapter;
using Decal.Adapter.Wrappers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using VirindiViewService;

namespace OracleOfDereth
{
    public static class Screenshot
    {
        private static Timer timer;
        private static CoreManager core;
        private static FieldInfo hudRendering;
        private static bool restoreHuds;
        private static bool hudWasRendering;
        private static object imguiManager;
        private static FieldInfo imguiRendering;
        private static object goArrow;
        private static FieldInfo goArrowAlpha;
        private static int arrowAlpha;
        private static readonly List<(UIElementType Element, IntPtr Address, Point Position, uint Clamp)> clientPanels = new();
        // Same movable panels as UtilityBelt's Client tool. Smartbox is the 3D scene,
        // so it must stay put. Side-by-side vitals is missing from Decal's older enum.
        private static readonly UIElementType[] clientElements =
        {
            UIElementType.Chat, UIElementType.FloatChat1, UIElementType.FloatChat2,
            UIElementType.FloatChat3, UIElementType.FloatChat4, UIElementType.Examination,
            UIElementType.Vitals, (UIElementType)0x100006D5, UIElementType.EnvPack,
            UIElementType.Panels, UIElementType.TBar, UIElementType.Indicators,
            UIElementType.ProgressBar, UIElementType.Combat, UIElementType.Radar
        };
        private static int frames;
        private static readonly Stopwatch elapsed = new Stopwatch();

        public static void Take() => Start(false);
        public static void TakeVista() => Start(true);

        private static void Start(bool hideInterface)
        {
            if (timer != null) return;
            try
            {
                core = CoreManager.Current;
                if (IntPtr.Size != 4 || core == null || core.CharacterFilter.LoginStatus < 1)
                    throw new InvalidOperationException("Enter the game before taking a screenshot.");
                using (new PhysicalPixels()) CaptureBounds(); // Check before hiding anything.

                if (hideInterface) HideInterface();

                frames = 0;
                elapsed.Restart();
                core.RenderFrame += OnFrame;
                Service.DeviceLost += OnDeviceLost;
                timer = new Timer { Interval = 100 };
                timer.Tick += Capture;
                timer.Start();
            }
            catch (Exception ex)
            {
                Cancel();
                Util.Log(ex);
                Util.Chat("Screenshot: " + ex.Message, Util.ColorPink);
            }
        }

        private static void HideInterface()
        {
            // VVS 1.0.0.47: DxHud.a() checks static 'h' before drawing any overlays.
            // This leaves view visibility, positions, and saved layouts untouched.
            hudRendering = typeof(DxHud).GetField("h", BindingFlags.NonPublic | BindingFlags.Static);
            var render = typeof(DxHud).GetMethod("a", BindingFlags.NonPublic | BindingFlags.Static,
                null, Type.EmptyTypes, null);
            byte[] il = render?.GetMethodBody()?.GetILAsByteArray();
            if (hudRendering?.FieldType != typeof(bool) || il == null || il.Length < 8 ||
                il[0] != 0x7e || BitConverter.ToInt32(il, 1) != hudRendering.MetadataToken ||
                il[5] != 0x2d || il[6] != 1 || il[7] != 0x2a)
                throw new InvalidOperationException("This Virindi Views version does not support screenshot hiding.");

            HideClientUi();
            hudWasRendering = (bool)hudRendering.GetValue(null);
            restoreHuds = true;
            hudRendering.SetValue(null, false);
            HideImGui();
            HideGoArrow();
        }

        private static void HideGoArrow()
        {
            var plugins = typeof(CoreManager).GetField("myPlugins", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.GetValue(core) as Dictionary<string, PluginBase>;
            if (plugins == null) return;
            foreach (var plugin in plugins.Values)
            {
                if (plugin.GetType().FullName != "GoArrow.PluginCore") continue;
                var arrow = plugin.GetType().GetField("mArrowHud", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(plugin);
                if (arrow == null) return;
                var alpha = arrow.GetType().GetField("mAlpha", BindingFlags.NonPublic | BindingFlags.Instance);
                if (alpha?.FieldType != typeof(int))
                    throw new InvalidOperationException("This GoArrow version does not support screenshot hiding.");

                // The legacy Decal HUD repaints independently of VVS. Suppress its alpha
                // during repaints too, without firing GoArrow's setting change events.
                arrowAlpha = (int)alpha.GetValue(arrow);
                goArrowAlpha = alpha;
                goArrow = arrow;
                SetGoArrowAlpha(0);
                return;
            }
        }

        private static void SetGoArrowAlpha(int alpha)
        {
            goArrowAlpha.SetValue(goArrow, alpha);
            var hud = goArrow.GetType().GetField("mHud", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(goArrow)
                as Decal.Adapter.Wrappers.Hud;
            if (hud != null) hud.Alpha = alpha;
        }

        private static void HideImGui()
        {
            // Optional: use the loaded service, without requiring UtilityBelt to be installed.
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.GetName().Name != "UtilityBelt.Service") continue;
                var service = assembly.GetType("UtilityBelt.Service.UBService");
                var manager = service?.GetField("Huds", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
                if (manager == null) return;

                // DoRender returns immediately when didInit is false. Newer builds add
                // DisableAllRendering to that guard, moving the return instruction.
                // Pause drawing only; keep window visibility and saved layouts intact.
                var type = manager.GetType();
                var field = type.GetField("didInit", BindingFlags.NonPublic | BindingFlags.Instance);
                var render = type.GetMethod("DoRender", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                byte[] il = render?.GetMethodBody()?.GetILAsByteArray();
                int returnOffset = il != null && il.Length >= 8 ? 8 + unchecked((sbyte)il[7]) : -1;
                if (field?.FieldType != typeof(bool) || il == null || il.Length < 8 ||
                    il[0] != 0x02 || il[1] != 0x7b || BitConverter.ToInt32(il, 2) != field.MetadataToken ||
                    il[6] != 0x2c || returnOffset < 8 || returnOffset >= il.Length || il[returnOffset] != 0x2a)
                    throw new InvalidOperationException("This UtilityBelt version does not support screenshot hiding.");

                if (!(bool)field.GetValue(manager)) return;
                imguiRendering = field;
                imguiManager = manager;
                field.SetValue(manager, false);
                return;
            }
        }

        private static void OnFrame(object sender, EventArgs e) => frames++;
        private static void OnDeviceLost(object sender, EventArgs e) => Cancel();

        private static void Capture(object sender, EventArgs e)
        {
            // Allow complete frames to reach the screen before copying the window.
            if (elapsed.ElapsedMilliseconds < 500) return;
            if (frames < 2 && elapsed.ElapsedMilliseconds < 3000) return;

            string path = null;
            Exception error = null;
            try
            {
                if (frames < 2) throw new InvalidOperationException("The game stopped rendering; try again.");
                if (core.CharacterFilter.LoginStatus < 1) throw new InvalidOperationException("The character logged out.");
                using var dpi = new PhysicalPixels();
                Rectangle bounds = CaptureBounds();
                string directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Asheron's Call");
                Directory.CreateDirectory(directory);
                path = NextScreenshotPath(directory);
                using (var bitmap = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format24bppRgb))
                {
                    using (var graphics = Graphics.FromImage(bitmap))
                        graphics.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size);
                    using var file = new FileStream(path, FileMode.CreateNew, FileAccess.Write);
                    bitmap.Save(file, ImageFormat.Jpeg);
                }
            }
            catch (Exception ex) { error = ex; }
            finally { Cancel(); }

            if (error != null)
            {
                Util.Log(error);
                Util.Chat("Screenshot: " + error.Message, Util.ColorPink);
            }
            else
            {
                Util.Chat("Screenshot saved: " + path, Util.ColorPink);
                try { Clipboard.SetText(path); }
                catch (Exception ex)
                {
                    Util.Log(ex);
                    Util.Chat("Could not copy the screenshot filepath to the clipboard.", Util.ColorPink);
                }
            }
        }

        internal static string NextScreenshotPath(string directory)
        {
            int next = 0;
            foreach (string file in Directory.EnumerateFiles(directory, "ScreenShot*.jpg"))
            {
                string number = Path.GetFileNameWithoutExtension(file).Substring("ScreenShot".Length);
                if (int.TryParse(number, out int index) && index >= next) next = checked(index + 1);
            }
            return Path.Combine(directory, "ScreenShot" + next.ToString("D5") + ".jpg");
        }

        // Also called on portal transitions and plugin shutdown, so a pending capture cannot
        // leave the interface hidden. Restore each system even if the other restore fails.
        public static void Cancel()
        {
            timer?.Stop();
            timer?.Dispose();
            timer = null;
            elapsed.Stop();
            if (core != null) core.RenderFrame -= OnFrame;
            Service.DeviceLost -= OnDeviceLost;
            try { RestoreClientUi(); }
            catch (Exception ex) { Util.Log(ex); }
            finally
            {
                try { if (goArrow != null) SetGoArrowAlpha(arrowAlpha); }
                catch (Exception ex) { Util.Log(ex); }
                goArrow = null;
                goArrowAlpha = null;
                try { if (imguiManager != null) imguiRendering.SetValue(imguiManager, true); }
                catch (Exception ex) { Util.Log(ex); }
                imguiManager = null;
                imguiRendering = null;
                try { if (restoreHuds) hudRendering.SetValue(null, hudWasRendering); }
                catch (Exception ex) { Util.Log(ex); }
                restoreHuds = false;
                core = null;
            }
        }

        private static Rectangle CaptureBounds()
        {
            IntPtr window = core.Decal.Hwnd;
            if (GetForegroundWindow() != window || IsIconic(window))
                throw new InvalidOperationException("Keep the game window in the foreground for the screenshot.");
            Point origin = Point.Empty;
            if (!GetClientRect(window, out Rect rect) || !ClientToScreen(window, ref origin))
                throw new InvalidOperationException("Could not locate the game window.");
            var bounds = new Rectangle(origin, new Size(rect.Right - rect.Left, rect.Bottom - rect.Top));
            return VisibleBounds(bounds, SystemInformation.VirtualScreen);
        }

        internal static Rectangle VisibleBounds(Rectangle window, Rectangle desktop)
        {
            Rectangle visible = Rectangle.Intersect(window, desktop);
            if (visible.Width <= 0 || visible.Height <= 0)
                throw new InvalidOperationException("The game window has no visible area to capture.");
            return visible;
        }

        // Query bounds and copy screen pixels in the same coordinate system on scaled monitors.
        // Only change this thread during capture, and restore AC's original DPI context afterward.
        private sealed class PhysicalPixels : IDisposable
        {
            private readonly IntPtr previous;
            public PhysicalPixels()
            {
                try { previous = SetThreadDpiAwarenessContext(new IntPtr(-3)); } // Per-monitor aware
                catch (EntryPointNotFoundException) { } // Windows before 10 version 1607
            }
            public void Dispose()
            {
                if (previous != IntPtr.Zero) SetThreadDpiAwarenessContext(previous);
            }
        }

        private static unsafe void HideClientUi()
        {
            var actions = core.Actions;
            // Snapshot every position before moving any panel; rollback also covers partial failure.
            foreach (var element in clientElements)
            {
                IntPtr address = actions.UIElementLookup(element);
                if (address != IntPtr.Zero)
                    clientPanels.Add((element, address, actions.UIElementRegion(element).Location,
                        *(uint*)((byte*)address + 0x554)));
            }
            if (clientPanels.Count == 0)
                throw new InvalidOperationException("The game interface is not ready.");
            if (!GetClientRect(core.Decal.Hwnd, out Rect rect))
                throw new InvalidOperationException("Could not locate the game window.");
            foreach (var panel in clientPanels)
                MovePanel(panel.Address, rect.Right + 4096, rect.Bottom + 4096, 0);
        }

        private static void RestoreClientUi()
        {
            if (clientPanels.Count == 0) return;
            var actions = core.Actions;
            foreach (var panel in clientPanels)
            {
                try
                {
                    // A portal/logout can recreate panels. Never restore an obsolete instance.
                    if (actions.UIElementLookup(panel.Element) == panel.Address)
                        MovePanel(panel.Address, panel.Position.X, panel.Position.Y, panel.Clamp);
                }
                catch (Exception ex) { Util.Log(ex); } // Still restore every other panel.
            }
            clientPanels.Clear();
        }

        private static unsafe void MovePanel(IntPtr address, int x, int y, uint restoreClamp)
        {
            // Retail x86 UIElement bindings from UtilityBelt.Service 3.0.11.
            // MoveTo is also what UBHelper.Core.MoveElement uses. Disable edge clamping
            // during the move, then restore the requested policy without changing size.
            var setClamp = (delegate* unmanaged[Thiscall]<void*, uint, void>)0x460370;
            var moveTo = (delegate* unmanaged[Thiscall]<void*, int, int, void>)0x4634C0;
            setClamp((void*)address, 0);
            try { moveTo((void*)address, x, y); }
            finally { setClamp((void*)address, restoreClamp); }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct Rect { public int Left, Top, Right, Bottom; }
        [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")] private static extern bool IsIconic(IntPtr window);
        [DllImport("user32.dll")] private static extern bool GetClientRect(IntPtr window, out Rect rect);
        [DllImport("user32.dll")] private static extern bool ClientToScreen(IntPtr window, ref Point point);
        [DllImport("user32.dll")] private static extern IntPtr SetThreadDpiAwarenessContext(IntPtr context);
    }
}
