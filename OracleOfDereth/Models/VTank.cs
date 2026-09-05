using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace OracleOfDereth
{
    // Read-only window onto Virindi Tank, the separate macro plugin. Central place to ask what VTank
    // is doing, so no other file has to touch the uTank2 API — or carry the isolation and reflection
    // this one does. Nothing he
    public static class VTank
    {
        // True when VTank is installed and has finished loading. Not cacheable at startup — plugin
        // load order isn't guaranteed, so VTank may well come up after we do. Callers should re-ask
        // rather than hold onto the answer.
        public static bool IsLoaded => Guard(ProbeLoaded);

        // The Enable button on VTank's own main window — the master switch. False whenever VTank
        // isn't there at all, so a caller can treat this as the single "is the macro running"
        // question. Read from PluginCore's internal MacroEnabled property.
        public static bool IsMacroEnabled => Guard(ProbeMacroEnabled);

        // The four subsystem toggles, as shown on VTank's main window. Independent of the master
        // switch: these can all read true while the macro itself is off.
        public static bool CombatEnabled => Setting("EnableCombat");
        public static bool LootingEnabled => Setting("EnableLooting");
        public static bool NavEnabled => Setting("EnableNav");
        public static bool MetaEnabled => Setting("EnableMeta");

        // Currently loaded profile names — "" when VTank isn't loaded or nothing is loaded.
        public static string NavProfile => Guard(() => ProbeStringMethod(NavProfileMethod));
        public static string LootProfile => Guard(() => ProbeStringMethod(LootProfileMethod));
        public static string MetaProfile => Guard(() => ProbeStringMethod(MetaProfileMethod));

        // "/od vtank" — dump current VTank state. Goes through the public members above rather than
        // poking uTank2 directly, so what it prints is exactly what the rest of the plugin sees:
        // a line reading off or blank while VTank's own window says otherwise means this class is
        // what's wrong, not VTank.
        public static void Debug()
        {
            if (!IsLoaded)
            {
                Util.Chat("VTank: not loaded", Util.ColorPink);
                return;
            }

            Util.Chat($"VTank: macro {OnOff(IsMacroEnabled)}   combat {OnOff(CombatEnabled)}   loot {OnOff(LootingEnabled)}   nav {OnOff(NavEnabled)}   meta {OnOff(MetaEnabled)}", Util.ColorPink);
            Util.Chat($"  nav {Blank(NavProfile)}   loot {Blank(LootProfile)}   meta {Blank(MetaProfile)}", Util.ColorPink);
        }

        private static string OnOff(bool on) => on ? "ON" : "off";

        private static string Blank(string s) => string.IsNullOrEmpty(s) ? "-" : s;

        // Obfuscated PluginCore method names, found by disassembling what VTank's own relay calls:
        // GetNavProfile() -> ar(), GetLootProfile() -> al(), GetMetaProfile() -> av(). All three are
        // internal instance methods returning a string field, with no permission check of their own.
        // ('at'() reaches the settings profile too, if that's ever wanted.)
        private const string NavProfileMethod = "ar";
        private const string LootProfileMethod = "al";
        private const string MetaProfileMethod = "av";

        // Settings don't hang off PluginCore. The relay's GetSetting(string) calls a static lookup
        // f3.e(name) that returns a gy, whose instance c() yields the boxed value. Both types are
        // internal to the uTank2 assembly, so they're fetched by name off that assembly rather than
        // referenced. Only the bool settings are exposed here; the values are boxed, hence the cast.
        private const string SettingLookupType = "f3";
        private const string SettingLookupMethod = "e";
        private const string SettingValueMethod = "c";

        // Resolved on first use and reused. A null entry means "look it up again" rather than
        // "absent", so a lookup attempted before VTank loaded isn't cached as a permanent failure.
        private static PropertyInfo _macroEnabled;
        private static readonly Dictionary<string, MethodInfo> _methods = new Dictionary<string, MethodInfo>();

        // The single place the optional dependency is absorbed. Every public member goes through one
        // of these, so no caller ever sees a missing VTank as an exception.
        private static bool Guard(System.Func<bool> probe)
        {
            try { return probe(); }
            catch { return false; }
        }

        private static string Guard(System.Func<string> probe)
        {
            try { return probe(); }
            catch { return ""; }
        }

        private static bool Setting(string name) => Guard(() => ProbeBoolSetting(name));

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
                _macroEnabled = typeof(uTank2.PluginCore).GetProperty("MacroEnabled", BindingFlags.NonPublic | BindingFlags.Instance);
            }

            // A rename or removal in some future VTank lands here rather than throwing.
            if (_macroEnabled == null) return false;

            return (bool)_macroEnabled.GetValue(uTank2.PluginCore.PC, null);
        }

        // Call one of PluginCore's internal no-arg string getters by name.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static string ProbeStringMethod(string name)
        {
            if (uTank2.PluginCore.PC == null) return "";

            MethodInfo m;
            if (!_methods.TryGetValue(name, out m) || m == null)
            {
                m = typeof(uTank2.PluginCore).GetMethod(name, BindingFlags.NonPublic | BindingFlags.Instance, null, System.Type.EmptyTypes, null);
                _methods[name] = m;
            }

            if (m == null) return "";

            return m.Invoke(uTank2.PluginCore.PC, null) as string ?? "";
        }

        // Read one of VTank's named bool settings. Anything unexpected — unknown key, non-bool value,
        // renamed internals — reads as false rather than throwing.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool ProbeBoolSetting(string name)
        {
            if (uTank2.PluginCore.PC == null) return false;

            System.Type lookup = typeof(uTank2.PluginCore).Assembly.GetType(SettingLookupType);
            if (lookup == null) return false;

            MethodInfo find = lookup.GetMethod(SettingLookupMethod, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(string) }, null);
            if (find == null) return false;

            object setting = find.Invoke(null, new object[] { name });
            if (setting == null) return false;

            MethodInfo read = setting.GetType().GetMethod(SettingValueMethod, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance, null, System.Type.EmptyTypes, null);
            if (read == null) return false;

            return read.Invoke(setting, null) is bool value && value;
        }
    }
}
