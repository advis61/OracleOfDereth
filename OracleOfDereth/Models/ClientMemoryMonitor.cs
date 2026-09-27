using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace OracleOfDereth
{
    // These Windows queries inspect our process's memory map, not AC's native objects.
    public static class ClientMemoryMonitor
    {
        private const ulong MiB = 1024 * 1024;
        private static readonly Queue<Snapshot> History = new Queue<Snapshot>();
        private static DateTime nextSample;
        private static DateTime lastWarning;
        private static int lastWarningLevel;

        public sealed class Snapshot
        {
            public DateTime Time;
            public long PrivateBytes;
            public long WorkingSet;
            public long ManagedBytes;
            public ulong TotalVirtual;
            public ulong FreeVirtual;
            public ulong LargestFreeBlock;
            public ulong AvailableCommit;
            public ulong TotalCommit;
        }

        public static void Reset()
        {
            History.Clear();
            nextSample = DateTime.MinValue;
            lastWarning = DateTime.MinValue;
            lastWarningLevel = 0;
        }

        public static void Tick()
        {
            if (Setting.WarnAboutClientMemory?.IsYes != true) return;
            DateTime now = DateTime.UtcNow;
            if (now < nextSample) return;
            nextSample = now.AddMinutes(1);
            try
            {
                Snapshot sample = Capture();
                History.Enqueue(sample);
                while (History.Count > 31) History.Dequeue();
                int level = Pressure(sample);
                if (ShouldWarn(level, now))
                {
                    string urgency = level == 2 ? "Critically low memory headroom" : "Low memory headroom";
                    Util.Chat($"{urgency}: {Headroom(sample)}. Restart the game client soon. /od memory shows details.", Util.ColorPink);
                }
            }
            catch (Exception ex)
            {
                // A failed reading must not interrupt the rest of the plugin's tick.
                // Retry next minute without flooding chat if the OS query keeps failing.
                Debug.WriteLine("Oracle memory monitor: " + ex.Message);
            }
        }

        public static void ShowStatus()
        {
            try
            {
                Snapshot sample = Capture();
                Util.Chat(FormatStatus(sample, History.Count > 0 ? History.Peek() : null));
            }
            catch (Exception ex) { Util.Chat("Unable to read client memory: " + ex.Message, Util.ColorPink); }
        }

        private static string FormatStatus(Snapshot sample, Snapshot baseline)
        {
            // Game Total includes plugins. The shared managed heap is only an estimate of
            // plugin memory: it includes runtime objects and excludes native resources.
            ulong used = sample.TotalVirtual - Math.Min(sample.FreeVirtual, sample.TotalVirtual);
            string text = $"Memory limit: {Percent(used, sample.TotalVirtual)} used"
                + $" | Game total: {sample.PrivateBytes / (double)MiB:0} MiB"
                + $" | Decal plugins: {sample.ManagedBytes / (double)MiB:0} MiB";
            if (baseline != null && baseline.PrivateBytes > 0)
            {
                double minutes = (sample.Time - baseline.Time).TotalMinutes;
                if (minutes >= 1)
                {
                    double change = 100.0 * (sample.PrivateBytes - baseline.PrivateBytes) / baseline.PrivateBytes;
                    double changeMiB = (sample.PrivateBytes - baseline.PrivateBytes) / (double)MiB;
                    text += $" | Change ({minutes:0} min): game {changeMiB:+0.#;-0.#;0} MiB ({change:+0.#;-0.#;0}%)";
                    if (baseline.ManagedBytes > 0)
                    {
                        double managedChange = 100.0 * (sample.ManagedBytes - baseline.ManagedBytes) / baseline.ManagedBytes;
                        double managedChangeMiB = (sample.ManagedBytes - baseline.ManagedBytes) / (double)MiB;
                        text += $", plugins {managedChangeMiB:+0.#;-0.#;0} MiB ({managedChange:+0.#;-0.#;0}%)";
                    }
                }
            }
            return text;
        }

        private static string Percent(ulong value, ulong total) => total == 0 ? "n/a" : $"{100.0 * value / total:0.#}%";

        private static string Headroom(Snapshot sample) =>
            $"Address space free {sample.FreeVirtual / MiB} / {sample.TotalVirtual / MiB} MiB; largest free block {sample.LargestFreeBlock / MiB} MiB; commit headroom {sample.AvailableCommit / MiB} MiB";

        // Conservative starting thresholds, not AC-specific allocation guarantees.
        // Fragmentation and system commit pressure can matter even with lots of free RAM.
        public static int Pressure(Snapshot sample)
        {
            if (sample.FreeVirtual <= 128 * MiB || sample.LargestFreeBlock <= 32 * MiB || sample.AvailableCommit <= 128 * MiB) return 2;
            if (sample.FreeVirtual <= 256 * MiB || sample.LargestFreeBlock <= 64 * MiB || sample.AvailableCommit <= 256 * MiB) return 1;
            return 0;
        }

        private static bool ShouldWarn(int level, DateTime now)
        {
            if (level == 0) return false;
            // Escalation is immediate; otherwise at most one warning every five minutes,
            // including when memory fluctuates across a threshold.
            if (level <= lastWarningLevel && now - lastWarning < TimeSpan.FromMinutes(5)) return false;
            lastWarningLevel = level;
            lastWarning = now;
            return true;
        }

        public static Snapshot Capture()
        {
            if (IntPtr.Size != 4) throw new PlatformNotSupportedException("This monitor targets the 32-bit game client.");
            var status = new MemoryStatus { Length = (uint)Marshal.SizeOf(typeof(MemoryStatus)) };
            if (!GlobalMemoryStatusEx(ref status)) throw new Win32Exception(Marshal.GetLastWin32Error());
            using (Process process = Process.GetCurrentProcess())
            {
                process.Refresh();
                return new Snapshot
                {
                    Time = DateTime.UtcNow,
                    PrivateBytes = process.PrivateMemorySize64,
                    WorkingSet = process.WorkingSet64,
                    // Shared CLR heap, not Oracle-only usage. Observe without forcing a collection
                    // or disturbing the allocation pattern we are trying to diagnose.
                    ManagedBytes = GC.GetTotalMemory(false),
                    TotalVirtual = status.TotalVirtual,
                    FreeVirtual = status.AvailableVirtual,
                    AvailableCommit = status.AvailablePageFile,
                    TotalCommit = status.TotalPageFile,
                    LargestFreeBlock = FindLargestFreeBlock()
                };
            }
        }

        private static ulong FindLargestFreeBlock()
        {
            ulong address = 0, largest = 0;
            var size = new UIntPtr((uint)Marshal.SizeOf(typeof(MemoryRegion32)));
            while (address < (1UL << 32))
            {
                if (VirtualQuery(new UIntPtr((uint)address), out MemoryRegion32 region, size) == UIntPtr.Zero)
                {
                    int error = Marshal.GetLastWin32Error();
                    // ERROR_INVALID_PARAMETER marks the end of accessible user address
                    // space, whether this client has a 2, 3, or 4 GiB configuration.
                    if (error == 87 && address > 0) break;
                    throw new Win32Exception(error);
                }
                ulong next = (ulong)region.BaseAddress + region.RegionSize;
                if (next <= address) throw new InvalidOperationException("Memory map did not advance.");
                if (region.State == 0x10000) largest = Math.Max(largest, region.RegionSize); // MEM_FREE
                address = next;
            }
            return largest;
        }

        // MEMORY_BASIC_INFORMATION32 has seven DWORDs, including a 32-bit SIZE_T.
        [StructLayout(LayoutKind.Sequential)]
        private struct MemoryRegion32
        {
            public uint BaseAddress, AllocationBase, AllocationProtect, RegionSize, State, Protect, Type;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MemoryStatus
        {
            public uint Length, MemoryLoad;
            public ulong TotalPhysical, AvailablePhysical, TotalPageFile, AvailablePageFile;
            public ulong TotalVirtual, AvailableVirtual, AvailableExtendedVirtual;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GlobalMemoryStatusEx(ref MemoryStatus status);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern UIntPtr VirtualQuery(UIntPtr address, out MemoryRegion32 buffer, UIntPtr length);
    }
}
