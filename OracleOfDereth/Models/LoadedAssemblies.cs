using System;
using System.Collections.Generic;
using System.Reflection;

namespace OracleOfDereth
{
    // Discover once, then track late-loading optional plugins through AssemblyLoad.
    // Only cache assemblies: plugin instances and windows can change between logins.
    internal static class LoadedAssemblies
    {
        private static readonly object sync = new object();
        private static readonly Dictionary<string, List<Assembly>> assemblies =
            new Dictionary<string, List<Assembly>>(StringComparer.OrdinalIgnoreCase);
        private static bool initialized;

        public static Assembly Find(string name, Version minimumVersion = null)
        {
            lock (sync)
            {
                if (!initialized)
                {
                    AppDomain.CurrentDomain.AssemblyLoad += OnAssemblyLoad;
                    foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies()) Add(assembly);
                    initialized = true;
                }
                if (assemblies.TryGetValue(name, out var matches))
                    foreach (Assembly assembly in matches)
                        if (minimumVersion == null || assembly.GetName().Version >= minimumVersion) return assembly;
                return null;
            }
        }

        private static void OnAssemblyLoad(object sender, AssemblyLoadEventArgs e)
        {
            lock (sync)
            {
                if (initialized) Add(e.LoadedAssembly);
            }
        }

        private static void Add(Assembly assembly)
        {
            string name = assembly.GetName().Name;
            if (!assemblies.TryGetValue(name, out var matches))
                assemblies[name] = matches = new List<Assembly>();
            if (!matches.Contains(assembly)) matches.Add(assembly);
        }

        public static void Shutdown()
        {
            lock (sync)
            {
                AppDomain.CurrentDomain.AssemblyLoad -= OnAssemblyLoad;
                initialized = false;
                assemblies.Clear();
            }
        }
    }
}
