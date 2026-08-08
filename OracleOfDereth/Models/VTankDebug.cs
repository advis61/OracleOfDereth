using System;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace OracleOfDereth
{
    // "/od vtank" — proves out the VTank integration on a real client.
    //
    // The point of this command is everything VTank.cs deliberately hides. That class answers every
    // question with a bool and swallows the reason, which is right for callers but useless when the
    // answer is a surprising one: "IsMacroEnabled false" reads identically whether VTank isn't
    // installed, is too old, or refused us on a permission check. So the probes below are repeated
    // here WITHOUT the swallow, and each one reports the exception type and message it actually hit.
    //
    // Same NoInlining discipline as VTank.cs, and for the same reason — see the comment there.
    public static class VTankDebug
    {
        public static void Run()
        {
            Util.Chat("--- VTank ---", Util.ColorPink);

            // What the rest of the plugin sees. If these disagree with the raw probes below, the
            // wrapper is swallowing something it shouldn't.
            Util.Chat($"IsLoaded {VTank.IsLoaded}   IsMacroEnabled {VTank.IsMacroEnabled}", Util.ColorPink);
            Report("assembly", ProbeAssembly);

            Report("PluginCore.PC", ProbePC);

            // The internal properties VTank.cs reads by reflection — the route that actually works.
            // Reported by name so a future VTank that renames or removes one shows up here as
            // "(no such property)" instead of silently turning IsMacroEnabled into a permanent false.
            Report("MacroEnabled (reflected)", () => ProbeProperty("MacroEnabled"));
            Report("NavType (reflected)", () => ProbeProperty("NavType"));
            Report("NavCurrent (reflected)", () => ProbeProperty("NavCurrent"));
            Report("NavNumPoints (reflected)", () => ProbeProperty("NavNumPoints"));
            Report("CurrentRulePriority (refl)", () => ProbeProperty("CurrentRulePriority"));
            Report("InterfaceAllowed (refl)", () => ProbeProperty("InterfaceAllowed"));

            // The sanctioned API, kept only to document that it is a dead end. VTank's shipped
            // parameterless GetExternalInterface() is "ldnull; ret" — it returns null for everyone,
            // always, and the only live overload wants credentials issued by Virindi. Expect NULL
            // here forever; it is not a symptom of anything being wrong on this machine.
            Report("GetExternalInterface()", ProbeInterface);
        }

        // Print one probe's result, or the exception it died on. Catching Exception rather than a
        // narrower type on purpose: a missing utank2-i.dll surfaces as FileNotFoundException or
        // TypeLoadException depending on how far the JIT got, and a refused call could be anything
        // VTank feels like throwing. All of them are the answer here.
        private static void Report(string label, Func<string> probe)
        {
            string value;

            try { value = probe(); }
            catch (Exception ex) { Util.Chat($"{label,-30} !! {ex.GetType().Name}: {ex.Message}", Util.ColorRed); return; }

            Util.Chat($"{label,-30} {Blank(value)}", Util.ColorPink);
        }

        private static string Blank(string s) => string.IsNullOrEmpty(s) ? "(empty)" : s;

        // Which uTank2 assembly actually got loaded into the client, straight from the AppDomain.
        // Distinguishes "VTank isn't running" from "VTank is running but a build whose external
        // interface behaves differently" — and unlike everything else here it needs no relay.
        private static string ProbeAssembly()
        {
            foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
            {
                var n = a.GetName();
                if (n.Name == "uTank2") return $"{n.Name} {n.Version}";
            }

            return "(uTank2 assembly not loaded)";
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static string ProbePC() => uTank2.PluginCore.PC == null ? "NULL" : "ok";

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static string ProbeInterface()
        {
            if (uTank2.PluginCore.PC == null) return "n/a (PC is null)";

            return uTank2.PluginCore.PC.GetExternalInterface() == null ? "NULL (expected - stubbed overload)" : "ok";
        }

        // Read one of PluginCore's internal properties by name, the same way VTank.cs does. Shows
        // the value with its type, so a bool that came back as something else is obvious.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static string ProbeProperty(string name)
        {
            if (uTank2.PluginCore.PC == null) return "n/a (PC is null)";

            PropertyInfo p = typeof(uTank2.PluginCore).GetProperty(
                name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (p == null) return "(no such property)";

            object value = p.GetValue(uTank2.PluginCore.PC, null);

            return value == null ? "(null)" : $"{value} [{value.GetType().Name}]";
        }
    }
}
