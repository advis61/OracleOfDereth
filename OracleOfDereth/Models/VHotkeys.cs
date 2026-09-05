using Decal.Adapter;
using System;
using System.Runtime.CompilerServices;
using VirindiHotkeySystem;

namespace OracleOfDereth
{
    internal static class VHotkeys
    {
        // Keep VHS types behind a non-inlined method so the plugin can load without VHS.
        private static Action unregister;

        public static void Init()
        {
            if (unregister != null) return;
            try
            {
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    if (assembly.GetName().Name != "VirindiHotkeySystem") continue;
                    unregister = Register();
                    return;
                }
            }
            catch (Exception ex)
            {
                Util.Log(ex);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static Action Register()
        {
            if (!VHotkeySystem.Running) return null;
            var system = VHotkeySystem.InstanceReal;
            var hotkey = new VHotkeyInfo("OracleDereth", true, "Vista Shot", "Save screenshot with UI hidden (/od vistashot)", 0, false, false, false);

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

        public static void Shutdown()
        {
            try { unregister?.Invoke(); }
            finally
            {
                unregister = null;
            }
        }
    }
}
