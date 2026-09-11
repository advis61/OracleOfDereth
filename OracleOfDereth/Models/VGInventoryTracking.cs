using Decal.Adapter;
using System;
using System.Linq;
using System.Reflection;
using System.Data.Common;
using System.IO;
using System.Threading.Tasks;
using System.Threading;

namespace OracleOfDereth
{
    // VGI remains optional. Enable tracking once per login through its own routine,
    // which updates its controls, queues the saved setting, and scans the inventory.
    internal static class VGInventoryTracking
    {
        private enum Phase { Dll, Database, Finished }
        private static Phase phase;
        private static DateTime? phaseStarted;
        private static DateTime? requestedAt;
        private static bool currentCharacterEnabled;
        private static Task<(bool Changed, Exception Error)> databaseWrite;
        private static CancellationTokenSource databaseCancellation;
        private const BindingFlags StaticMembers = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;

        private enum Result { Waiting, AlreadyEnabled, Requested, Enabled, Unsupported }

        public static bool ToggleView()
        {
            var hud = GetView();
            if (hud == null) return false;
            hud.Visible = !hud.Visible;
            return true;
        }

        public static string OpenButtonText()
        {
            // Mirror VGI's own pending-ID title, including while its window is hidden.
            // If the VGI window is unavailable, keep the ordinary Open VGI label.
            try
            {
                string title = GetView()?.Title;
                const string prefix = "Virindi Global Inventory";
                if (title != null && title.StartsWith(prefix + " (", StringComparison.Ordinal)
                    && title.EndsWith(" to read)", StringComparison.Ordinal))
                    return "VGI" + title.Substring(prefix.Length);
            }
            catch { } // Optional plugin may be initializing or shutting down.
            return "Open VGI";
        }

        private static VirindiViewService.HudView GetView()
        {
            // VVS exposes live views, including hidden windows. Resolve the current
            // window so character changes cannot leave us holding a disposed view.
            // No VGI assembly lookup or obfuscated member names are needed here.
            foreach (var hud in VirindiViewService.HudView.GetAllViews())
            {
                string title = hud.Title;
                if (title == "Virindi Global Inventory" ||
                    (title != null && title.StartsWith("Virindi Global Inventory (", StringComparison.Ordinal)
                        && title.EndsWith(" to read)", StringComparison.Ordinal)))
                    return hud;
            }
            return null;
        }

        public static void Init()
        {
            Shutdown();
            databaseCancellation = new CancellationTokenSource();
            phase = Phase.Dll;
            phaseStarted = null;
            requestedAt = null;
            currentCharacterEnabled = false;
            databaseWrite = null;
        }

        public static void Shutdown()
        {
            // Cancel pending writes without blocking plugin teardown on a SQLite lock.
            databaseCancellation?.Cancel();
            databaseCancellation?.Dispose();
            databaseCancellation = null;
            databaseWrite = null;
            phase = Phase.Finished;
        }

        public static void Tick()
        {
            if (Setting.SetVGITrackAllItems?.IsYes != true)
            {
                if (phase != Phase.Finished) Shutdown();
                return;
            }
            if (phase == Phase.Finished || CoreManager.Current.CharacterFilter.LoginStatus < 1) return;
            if (!phaseStarted.HasValue) phaseStarted = DateTime.UtcNow;
            try
            {
                if (phase == Phase.Database) TickDatabase();
                else TickDll();
            }
            catch (Exception ex)
            {
                Util.Log(ex);
                if (phase == Phase.Dll) BeginDatabase();
                else
                {
                    Shutdown();
                    ReportUnavailable();
                }
            }
        }

        private static void TickDll()
        {
            Assembly assembly = LoadedAssemblies.Find("VirindiGlobalInventory");
            if (assembly == null)
            {
                if (DateTime.UtcNow - phaseStarted.Value > TimeSpan.FromMinutes(1)) Shutdown();
                return;
            }

            // Obfuscation changes the type names between releases. Only use
            // mappings verified against the corresponding plugin binary.
            var names = IntegrationTypes(assembly.GetName().Version);
            Result result = names.Tracking != null
                ? AdvanceTracking(assembly.GetType(names.Tracking), assembly.GetType(names.View), DateTime.UtcNow)
                : Result.Unsupported;
            if (result == Result.Waiting && (requestedAt.HasValue || DateTime.UtcNow - phaseStarted.Value <= TimeSpan.FromMinutes(1))) return;
            currentCharacterEnabled = result == Result.Enabled || result == Result.AlreadyEnabled;
            if (result == Result.Enabled)
                Util.Chat("VGI: Enabled Track All Items for " + CoreManager.Current.CharacterFilter.Name + ".", Util.ColorPink);
            BeginDatabase();
        }

        private static void BeginDatabase()
        {
            // Start the database work on the next tick, outside DLL error handling.
            phase = Phase.Database;
            phaseStarted = DateTime.UtcNow;
        }

        private static void TickDatabase()
        {
            if (databaseWrite != null)
            {
                if (!databaseWrite.IsCompleted) return;
                var result = databaseWrite.Result;
                databaseWrite = null;
                if (result.Error == null)
                {
                    Shutdown();
                    if (result.Changed)
                        Util.Chat(currentCharacterEnabled
                            ? "VGI: Saved Track All Items for all known characters on this server. Other characters will activate tracking when they log in."
                            : "VGI: Saved Track All Items for all known characters on this server. Relog to activate tracking.", Util.ColorPink);
                    else if (!currentCharacterEnabled && requestedAt.HasValue)
                        Util.Chat("VGI: Track All Items is saved for this server. Relog to activate tracking.", Util.ColorPink);
                    return;
                }
                if (DateTime.UtcNow - phaseStarted.Value >= TimeSpan.FromMinutes(1))
                {
                    Shutdown();
                    Util.Log(result.Error);
                    ReportUnavailable();
                }
                return; // Retry on a later tick if VGI is still creating or locking its database.
            }

            string server = CoreManager.Current.CharacterFilter.Server;
            string character = CoreManager.Current.CharacterFilter.Name;
            CancellationToken cancellation = databaseCancellation.Token;
            // SQLite lock waits must not stall the game's UI thread. Capture the owner
            // before dispatch so a later character login cannot redirect this write.
            databaseWrite = Task.Run(() =>
            {
                try { return (SaveTracking(VGInventory.FindDirectory(), server, character, cancellation), (Exception)null); }
                catch (Exception ex) { return (false, ex); }
            });
        }

        private static bool SaveTracking(string folder, string server, string character, CancellationToken cancellation)
        {
            cancellation.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(server) || server.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0
                || string.IsNullOrWhiteSpace(character)) throw new ArgumentException("Invalid VGI character or server.");
            if (string.IsNullOrEmpty(folder)) throw new DirectoryNotFoundException("VGI installation was not found.");
            string path = Path.Combine(folder, "_" + server + ".db");
            if (!File.Exists(path)) throw new FileNotFoundException("VGI has not created this server's database yet.", path);

            using (DbConnection connection = VGInventory.OpenConnection(folder, path, readOnly: false))
            using (DbTransaction transaction = connection.BeginTransaction())
            using (DbCommand command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                cancellation.ThrowIfCancellationRequested();
                command.CommandTimeout = 2;
                foreach (var value in new[] { ("@server", server), ("@character", character) })
                {
                    DbParameter parameter = command.CreateParameter();
                    parameter.ParameterName = value.Item1;
                    parameter.Value = value.Item2;
                    command.Parameters.Add(parameter);
                }
                // Enable every existing tracking record for this server, and enroll
                // known characters that have no tracking record yet. Item data stays intact.
                command.CommandText = "UPDATE CharactersEnabled SET Tracking=3 WHERE CharServer=@server AND (Tracking IS NULL OR Tracking<>3)";
                int changed = command.ExecuteNonQuery();
                command.CommandText = "INSERT INTO CharactersEnabled (CharServer,CharName,Tracking) SELECT DISTINCT @server,CharName,3 FROM CharacterInfo WHERE CharServer=@server AND NOT EXISTS (SELECT 1 FROM CharactersEnabled WHERE CharServer=@server AND CharName=CharacterInfo.CharName)";
                changed += command.ExecuteNonQuery();
                // Include this login even if VGI has not saved its CharacterInfo row yet.
                command.CommandText = "INSERT INTO CharactersEnabled (CharServer,CharName,Tracking) SELECT @server,@character,3 WHERE NOT EXISTS (SELECT 1 FROM CharactersEnabled WHERE CharServer=@server AND CharName=@character)";
                changed += command.ExecuteNonQuery();
                // Confirm the stored value, including when a trigger ignored or
                // rewrote an update. A failed verification rolls back the transaction.
                command.CommandText = "SELECT CASE WHEN COUNT(*)>0 AND MIN(CASE WHEN Tracking=3 THEN 1 ELSE 0 END)=1 THEN 1 ELSE 0 END FROM CharactersEnabled WHERE CharServer=@server";
                if (Convert.ToInt32(command.ExecuteScalar()) != 1)
                    throw new InvalidOperationException("VGI did not retain Track All Items in its database.");
                command.CommandText = "SELECT COUNT(*) FROM CharacterInfo WHERE CharServer=@server AND NOT EXISTS (SELECT 1 FROM CharactersEnabled WHERE CharServer=@server AND CharName=CharacterInfo.CharName AND Tracking=3)";
                if (Convert.ToInt64(command.ExecuteScalar()) != 0)
                    throw new InvalidOperationException("VGI did not enroll every known character on this server.");
                command.CommandText = "SELECT COUNT(*) FROM CharactersEnabled WHERE CharServer=@server AND CharName=@character AND Tracking=3";
                if (Convert.ToInt64(command.ExecuteScalar()) == 0)
                    throw new InvalidOperationException("VGI did not enroll the current character.");
                cancellation.ThrowIfCancellationRequested();
                transaction.Commit();
                return changed > 0;
            }
        }

        private static void ReportUnavailable()
        {
            Util.Chat("VGI: Could not finish enabling Track All Items for this server. Enable it in Virindi Global Inventory.", Util.ColorPink);
        }

        private static (string Tracking, string View) IntegrationTypes(Version version)
        {
            if (version == new Version(1, 0, 0, 8)) return ("b5", "a9");
            if (version == new Version(1, 0, 0, 9)) return ("b6", "a6");
            return (null, null);
        }

        private static Result AdvanceTracking(Type tracking, Type view, DateTime now)
        {
            Result result = EnableTracking(tracking, view, !requestedAt.HasValue);
            if (result == Result.Requested)
            {
                requestedAt = now;
                return Result.Waiting;
            }
            if (!requestedAt.HasValue) return result;
            if (result == Result.AlreadyEnabled) return Result.Enabled;
            if (result == Result.Waiting && now - requestedAt.Value >= TimeSpan.FromSeconds(10))
                return Result.Unsupported;
            return result;
        }

        private static Result EnableTracking(Type tracking, Type view, bool allowRequest)
        {
            // In both verified versions, f becomes true after VGI has read this
            // character's saved mode; b(eCharacterTrackMode) handles its checkboxes.
            Type mode = tracking?.GetNestedType("eCharacterTrackMode", BindingFlags.Public | BindingFlags.NonPublic);
            FieldInfo ready = tracking?.GetField("f", StaticMembers);
            FieldInfo current = tracking?.GetField("a", StaticMembers);
            if (mode == null || !mode.IsEnum || Enum.GetUnderlyingType(mode) != typeof(int)
                || ready?.FieldType != typeof(bool) || current?.FieldType != mode || view == null)
                return Result.Unsupported;
            if (!Enum.IsDefined(mode, "Everything")) return Result.Unsupported;
            object everything = Enum.Parse(mode, "Everything");
            if (Convert.ToInt32(everything) != 3) return Result.Unsupported;
            MethodInfo enable = tracking.GetMethod("b", StaticMembers, null, new[] { mode }, null);
            if (enable == null || enable.ReturnType != typeof(void)) return Result.Unsupported;
            if (!(bool)ready.GetValue(null)) return Result.Waiting;
            // The routine synchronizes all three checkboxes, so wait for the view too.
            foreach (string name in new[] { "h", "i", "j" })
            {
                FieldInfo checkbox = view.GetField(name, StaticMembers);
                if (checkbox == null) return Result.Unsupported;
                if (checkbox.GetValue(null) == null) return Result.Waiting;
            }
            if (everything.Equals(current.GetValue(null))) return Result.AlreadyEnabled;
            if (!allowRequest) return Result.Waiting;
            enable.Invoke(null, new[] { everything });
            // VGI changes its mode asynchronously after processing its database queue.
            // The next ticks must observe that change before announcing success.
            return Result.Requested;
        }
    }
}
