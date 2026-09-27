using System;
using System.Diagnostics;
using System.Reflection;
using OracleOfDereth;

internal static class ClientMemoryMonitorTests
{
    private const ulong MiB = 1024 * 1024;

    public static void Run()
    {
        // Independently exercise exhaustion, fragmentation, and commit pressure.
        foreach (ulong total in new[] { 2048 * MiB, 4096 * MiB })
        {
            AssertPressure(total, 512, 128, 1024, 0);
            AssertPressure(total, 256, 128, 1024, 1);
            AssertPressure(total, 128, 128, 1024, 2);
            AssertPressure(total, 512, 64, 1024, 1);
            AssertPressure(total, 512, 32, 1024, 2);
            AssertPressure(total, 512, 128, 256, 1);
            AssertPressure(total, 512, 128, 128, 2);
            AssertPressure(total, 0, 0, 0, 2);
            AssertPressure(total, 257, 65, 257, 0);
        }

        var shouldWarn = typeof(ClientMemoryMonitor).GetMethod("ShouldWarn", BindingFlags.NonPublic | BindingFlags.Static);
        DateTime now = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        ClientMemoryMonitor.Reset();
        try
        {
            AssertWarning(shouldWarn, 0, now, false);
            AssertWarning(shouldWarn, 1, now, true);
            AssertWarning(shouldWarn, 1, now.AddMinutes(1), false);
            AssertWarning(shouldWarn, 2, now.AddMinutes(2), true);
            AssertWarning(shouldWarn, 0, now.AddMinutes(3), false);
            AssertWarning(shouldWarn, 1, now.AddMinutes(4), false);
            AssertWarning(shouldWarn, 2, now.AddMinutes(6), false);
            AssertWarning(shouldWarn, 2, now.AddMinutes(7), true);
        }
        finally { ClientMemoryMonitor.Reset(); }

        // Exercise the real Windows ABI and address-map traversal in the x86 test
        // process, including unsigned addresses above 0x7fffffff on LAA processes.
        var timer = Stopwatch.StartNew();
        ClientMemoryMonitor.Snapshot sample = ClientMemoryMonitor.Capture();
        if (sample.PrivateBytes <= 0 || sample.WorkingSet <= 0 || sample.ManagedBytes <= 0 || sample.TotalVirtual == 0 ||
            sample.TotalVirtual > (1UL << 32) || sample.FreeVirtual > sample.TotalVirtual ||
            sample.LargestFreeBlock == 0 || sample.LargestFreeBlock > sample.TotalVirtual)
            throw new InvalidOperationException("Windows returned an invalid client-memory snapshot.");
        Console.WriteLine($"Memory sampler smoke test: {timer.ElapsedMilliseconds} ms; address space {sample.TotalVirtual / MiB} MiB.");

        var format = typeof(ClientMemoryMonitor).GetMethod("FormatStatus", BindingFlags.NonPublic | BindingFlags.Static);
        var baseline = new ClientMemoryMonitor.Snapshot
        {
            Time = now, PrivateBytes = 100 * (long)MiB, ManagedBytes = 50 * (long)MiB
        };
        var later = new ClientMemoryMonitor.Snapshot
        {
            Time = now.AddMinutes(10), PrivateBytes = 125 * (long)MiB, ManagedBytes = 40 * (long)MiB,
            TotalVirtual = 2048 * MiB, FreeVirtual = 1024 * MiB
        };
        string status = (string)format.Invoke(null, new object[] { later, baseline });
        if (!status.StartsWith("Memory limit: 50% used | Game total: 125 MiB | Decal plugins: 40 MiB") ||
            !status.Contains("Change (10 min): game +25 MiB (+25%), plugins -10 MiB (-20%)") ||
            status.Contains("resident") || status.Contains("commit") || status.Contains("\n"))
            throw new InvalidOperationException("Memory status did not distinguish managed and private growth on one line.");
        baseline.ManagedBytes = 0;
        status = (string)format.Invoke(null, new object[] { later, baseline });
        if (status.Contains(", plugins"))
            throw new InvalidOperationException("Memory status calculated a managed trend without a valid baseline.");
    }

    private static void AssertPressure(ulong total, ulong free, ulong largest, ulong commit, int expected)
    {
        var sample = new ClientMemoryMonitor.Snapshot
        {
            TotalVirtual = total, FreeVirtual = free * MiB,
            LargestFreeBlock = largest * MiB, AvailableCommit = commit * MiB
        };
        if (ClientMemoryMonitor.Pressure(sample) != expected)
            throw new InvalidOperationException("Client memory pressure threshold was not detected correctly.");
    }

    private static void AssertWarning(MethodInfo method, int level, DateTime now, bool expected)
    {
        if ((bool)method.Invoke(null, new object[] { level, now }) != expected)
            throw new InvalidOperationException("Memory warnings did not respect cooldown or escalation.");
    }
}
