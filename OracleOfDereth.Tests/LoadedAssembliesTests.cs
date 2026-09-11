using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading.Tasks;
using OracleOfDereth;

internal static class LoadedAssembliesTests
{
    public static void Run()
    {
        Type cache = typeof(VGInventory).Assembly.GetType("OracleOfDereth.LoadedAssemblies", true);
        MethodInfo find = cache.GetMethod("Find");
        MethodInfo shutdown = cache.GetMethod("Shutdown");
        Assembly Find(string name, Version version = null) => (Assembly)find.Invoke(null, new object[] { name, version });
        string name = "LatePlugin_" + Guid.NewGuid().ToString("N");
        try
        {
            if (Find(typeof(VGInventory).Assembly.GetName().Name) != typeof(VGInventory).Assembly)
                throw new Exception("Already-loaded assemblies must be discovered.");
            if (Find(name) != null) throw new Exception("Missing plugin should be optional.");
            AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName(name) { Version = new Version(1, 0, 0, 0) }, AssemblyBuilderAccess.Run);
            if (Find(name)?.GetName().Version != new Version(1, 0, 0, 0)) throw new Exception("Late load must invalidate an earlier miss.");
            if (Find(name, new Version(2, 0, 0, 0)) != null) throw new Exception("Minimum version must be respected.");
            AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName(name) { Version = new Version(2, 0, 0, 0) }, AssemblyBuilderAccess.Run);
            Parallel.For(0, 100, _ =>
            {
                if (Find(name, new Version(2, 0, 0, 0))?.GetName().Version != new Version(2, 0, 0, 0))
                    throw new Exception("Multiple versions and concurrent readers must be supported.");
            });
            shutdown.Invoke(null, null);
            if (Find(name, new Version(2, 0, 0, 0)) == null) throw new Exception("Cache must rebuild after shutdown.");
        }
        finally { shutdown.Invoke(null, null); }
    }
}
