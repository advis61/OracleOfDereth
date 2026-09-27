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
        private static Action unregisterUiOff;
        private static Action unregisterUiOn;

        public static void Init()
        {
            if (unregisterVistaShot != null || unregisterScreenshot != null) return;
            try
            {
                if (LoadedAssemblies.Find("VirindiHotkeySystem") != null)
                {
                    unregisterScreenshot = RegisterScreenshot();
                    unregisterVistaShot = RegisterVistaShot();
                    unregisterUiOff = RegisterUi(false);
                    unregisterUiOn = RegisterUi(true);
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
                InterfaceVisibility.TakeScreenshot(true);
            }
            catch (Exception ex) { Util.Log(ex); }
        }

        private static void OnScreenshot(object sender, VHotkeyInfo.cEatableFiredEventArgs e)
        {
            try
            {
                if (CoreManager.Current == null || CoreManager.Current.CharacterFilter.LoginStatus < 1) return;
                e.Eat = true;
                InterfaceVisibility.TakeScreenshot(false);
            }
            catch (Exception ex) { Util.Log(ex); }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static Action RegisterUi(bool visible)
        {
            if (!VHotkeySystem.Running) return null;
            var system = VHotkeySystem.InstanceReal;
            var hotkey = new VHotkeyInfo("OracleDereth", true, visible ? "UI On" : "UI Off",
                visible ? "Restore UI (/od ui on)" : "Hide UI; Escape restores it (/od ui off)", 0, false, false, false);
            if (visible) hotkey.Fired2 += OnUiOn;
            else hotkey.Fired2 += OnUiOff;
            Action unregister = () =>
            {
                if (visible) hotkey.Fired2 -= OnUiOn;
                else hotkey.Fired2 -= OnUiOff;
                system.RemoveHotkey(hotkey);
            };
            try { system.AddHotkey(hotkey); }
            catch { unregister(); throw; }
            return unregister;
        }

        private static void OnUiOn(object sender, VHotkeyInfo.cEatableFiredEventArgs e)
        {
            e.Eat = true;
            try { InterfaceVisibility.Show(); }
            catch (Exception ex) { Util.Log(ex); }
        }

        private static void OnUiOff(object sender, VHotkeyInfo.cEatableFiredEventArgs e)
        {
            e.Eat = true;
            try { InterfaceVisibility.Hide(); }
            catch (Exception ex) { Util.Log(ex); }
        }

        public static void Shutdown()
        {
            try { unregisterUiOff?.Invoke(); }
            catch (Exception ex) { Util.Log(ex); }
            finally { unregisterUiOff = null; }

            try { unregisterUiOn?.Invoke(); }
            catch (Exception ex) { Util.Log(ex); }
            finally { unregisterUiOn = null; }

            try { unregisterVistaShot?.Invoke(); }
            catch (Exception ex) { Util.Log(ex); }
            finally { unregisterVistaShot = null; }

            try { unregisterScreenshot?.Invoke(); }
            catch (Exception ex) { Util.Log(ex); }
            finally { unregisterScreenshot = null; }
        }
    }
}
