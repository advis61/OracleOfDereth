using System.Reflection;
using System.Runtime.CompilerServices;

namespace OracleOfDereth
{
    public static class VTank
    {
        // True when VTank is installed and has finished loading. Not cacheable at startup — plugin
        // load order isn't guaranteed, so VTank may well come up after we do. Callers should re-ask
        // rather than hold onto the answer.
        public static bool IsLoaded
        {
            get
            {
                try { return ProbeLoaded(); }
                catch { return false; }   // no VTank installed, or an incompatible version
            }
        }

        // True when VTank is loaded AND its macro is currently switched on — the same state as the
        // Enable button on VTank's own main window. False whenever VTank isn't there at all, so a
        // caller can treat this as the single "is the macro running" question.
        public static bool IsMacroEnabled
        {
            get
            {
                try { return ProbeMacroEnabled(); }
                catch { return false; }
            }
        }

        // Resolved once on first use and reused. Null means "look it up again" rather than "absent",
        // so a lookup attempted before VTank loaded doesn't get cached as a permanent failure.
        private static PropertyInfo _macroEnabled;

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool ProbeLoaded()
        {
            return uTank2.PluginCore.PC != null;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool ProbeMacroEnabled()
        {
            if (uTank2.PluginCore.PC == null) return false;

            if (_macroEnabled == null)
            {
                _macroEnabled = typeof(uTank2.PluginCore).GetProperty(
                    "MacroEnabled", BindingFlags.NonPublic | BindingFlags.Instance);
            }

            // A rename or removal in some future VTank lands here rather than throwing.
            if (_macroEnabled == null) return false;

            return (bool)_macroEnabled.GetValue(uTank2.PluginCore.PC, null);
        }
    }
}
