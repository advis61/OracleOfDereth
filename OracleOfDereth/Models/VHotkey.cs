using Decal.Adapter;
using System;
using System.Runtime.CompilerServices;
using VirindiHotkeySystem;

namespace OracleOfDereth
{
    internal static class VHotkey
    {
        // Keep VHS types behind a non-inlined method so the plugin can load without VHS.
        private static Action unregisterVistaShot;
        private static Action unregisterScreenshot;

        public static void Init()
        {
            if (unregisterVistaShot != null || unregisterScreenshot != null) return;
            try
            {
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    if (assembly.GetName().Name != "VirindiHotkeySystem") continue;
                    unregisterScreenshot = RegisterScreenshot();
                    unregisterVistaShot = RegisterVistaShot();
                    return;
                }
            }
            catch (Exception ex)
            {
                Util.Log(ex);
                Shutdown();
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static Action RegisterVistaShot()
        {
            if (!VHotkeySystem.Running) return null;
            var system = VHotkeySystem.InstanceReal;
            var hotkey = new VHotkeyInfo("OracleDereth", true, "Vistashot", "Save screenshot with UI hidden (/od vistashot)", 0, false, false, false);
            hotkey.Fired2 += OnVistaShot;
            try { system.AddHotkey(hotkey); }
            catch
            {
                hotkey.Fired2 -= OnVistaShot;
                throw;
            }
            return () =>
            {
                hotkey.Fired2 -= OnVistaShot;
                system.RemoveHotkey(hotkey);
            };
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static Action RegisterScreenshot()
        {
            if (!VHotkeySystem.Running) return null;
            var system = VHotkeySystem.InstanceReal;
            var hotkey = new VHotkeyInfo("OracleDereth", true, "Screenshot", "Save screenshot (/od screenshot)", 0, false, false, false);
            hotkey.Fired2 += OnScreenshot;
            try { system.AddHotkey(hotkey); }
            catch
            {
                hotkey.Fired2 -= OnScreenshot;
                throw;
            }
            return () =>
            {
                hotkey.Fired2 -= OnScreenshot;
                system.RemoveHotkey(hotkey);
            };
        }

        private static void OnVistaShot(object sender, VHotkeyInfo.cEatableFiredEventArgs e)
        {
            try
            {
                if (CoreManager.Current == null || CoreManager.Current.CharacterFilter.LoginStatus < 1) return;
                e.Eat = true;
                Screenshot.TakeVista();
            }
            catch (Exception ex) { Util.Log(ex); }
        }

        private static void OnScreenshot(object sender, VHotkeyInfo.cEatableFiredEventArgs e)
        {
            try
            {
                if (CoreManager.Current == null || CoreManager.Current.CharacterFilter.LoginStatus < 1) return;
                e.Eat = true;
                Screenshot.Take();
            }
            catch (Exception ex) { Util.Log(ex); }
        }

        public static void Shutdown()
        {
            try { unregisterVistaShot?.Invoke(); }
            catch (Exception ex) { Util.Log(ex); }
            finally { unregisterVistaShot = null; }

            try { unregisterScreenshot?.Invoke(); }
            catch (Exception ex) { Util.Log(ex); }
            finally { unregisterScreenshot = null; }
        }
    }
}
