using OracleOfDereth;
using System;
using System.Reflection;
using System.Data.Common;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

internal static class VGInventoryTrackingTests
{
    private static readonly MethodInfo Enable = typeof(VGInventory).Assembly
        .GetType("OracleOfDereth.VGInventoryTracking", true)
        .GetMethod("EnableTracking", BindingFlags.NonPublic | BindingFlags.Static);

    public static void Run()
    {
        Check(Setting.SetVGITrackAllItems.Name == "Set VGI Track All Items"
            && Setting.SetVGITrackAllItems.DefaultValue == "Yes"
            && Setting.All.Contains(Setting.SetVGITrackAllItems), "Automatic VGI setup must have a visible default-Yes setting.");
        var originalSetting = Setting.SetVGITrackAllItems;
        try
        {
            Setting.SetVGITrackAllItems = new Setting { Key = "DisabledVgiTest_" + Guid.NewGuid().ToString("N"), DefaultValue = "No" };
            // No Decal session is available here: an off setting must exit before
            // touching the game, probing VGI, or scheduling a database write.
            Enable.DeclaringType.GetMethod("Tick", BindingFlags.Public | BindingFlags.Static).Invoke(null, null);
        }
        finally { Setting.SetVGITrackAllItems = originalSetting; }
        var mappings = Enable.DeclaringType.GetMethod("IntegrationTypes", BindingFlags.NonPublic | BindingFlags.Static);
        (string Tracking, string View) Names(Version version) =>
            ((string, string))mappings.Invoke(null, new object[] { version });
        Check(Names(new Version(1, 0, 0, 8)) == ("b5", "a9"), "VGI 1.0.0.8 must use its verified integration types.");
        Check(Names(new Version(1, 0, 0, 9)) == ("b6", "a6"), "VGI 1.0.0.9 uses different obfuscated type names.");
        Check(Names(new Version(1, 0, 0, 10)).Tracking == null && Names(null).Tracking == null,
            "Unverified versions must not use a guessed integration mapping.");
        Check(Apply(null, null) == "Unsupported", "Missing VGI must not be invoked.");
        Check(Apply(typeof(string), typeof(View)) == "Unsupported", "An incompatible integration must not be invoked.");
        Tracking.f = false;
        Tracking.Calls = 0;
        Check(Apply() == "Waiting" && Tracking.Calls == 0, "Wait until VGI loads the character's saved tracking mode.");
        Tracking.f = true;
        View.j = null;
        Check(Apply() == "Waiting" && Tracking.Calls == 0, "Wait until VGI's tracking controls are initialized.");
        View.j = new object();
        foreach (var initial in new[] { Tracking.eCharacterTrackMode.None, Tracking.eCharacterTrackMode.Equipment, Tracking.eCharacterTrackMode.Played })
        {
            Tracking.a = initial;
            int before = Tracking.Calls;
            Check(Apply() == "Requested" && Tracking.a == Tracking.eCharacterTrackMode.Everything && Tracking.Calls == before + 1,
                "Enable Track All Items through VGI's tracking routine from " + initial + ".");
            Check(Apply() == "AlreadyEnabled" && Tracking.Calls == before + 1,
                "An already-enabled character must not restart its inventory scan.");
        }
        var init = Enable.DeclaringType.GetMethod("Init", BindingFlags.Public | BindingFlags.Static);
        var advance = Enable.DeclaringType.GetMethod("AdvanceTracking", BindingFlags.NonPublic | BindingFlags.Static);
        DateTime now = new DateTime(2026, 1, 1);
        string Advance(int seconds) => advance.Invoke(null, new object[] { typeof(Tracking), typeof(View), now.AddSeconds(seconds) }).ToString();
        init.Invoke(null, null);
        Tracking.CompleteImmediately = false;
        Tracking.a = Tracking.eCharacterTrackMode.None;
        int calls = Tracking.Calls;
        Check(Advance(0) == "Waiting" && Advance(1) == "Waiting" && Tracking.Calls == calls + 1,
            "An asynchronous enable request must be sent once and remain unconfirmed.");
        Tracking.a = Tracking.eCharacterTrackMode.Everything;
        Check(Advance(2) == "Enabled", "Success must be confirmed by VGI's actual mode.");
        init.Invoke(null, null);
        Tracking.a = Tracking.eCharacterTrackMode.None;
        calls = Tracking.Calls;
        Check(Advance(0) == "Waiting" && Advance(10) == "Unsupported" && Tracking.Calls == calls + 1,
            "A request that never takes effect must time out for database fallback without being repeated.");
        init.Invoke(null, null);
        Tracking.CompleteImmediately = true;
        var cancellation = new CancellationTokenSource();
        CancellationToken token = cancellation.Token;
        Enable.DeclaringType.GetField("databaseCancellation", BindingFlags.NonPublic | BindingFlags.Static).SetValue(null, cancellation);
        Enable.DeclaringType.GetMethod("Shutdown", BindingFlags.Public | BindingFlags.Static).Invoke(null, null);
        Check(token.IsCancellationRequested
            && Enable.DeclaringType.GetField("phase", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null).ToString() == "Finished",
            "Shutdown must cancel pending database work and stop subsequent tracking attempts.");
        init.Invoke(null, null);
        var beginDatabase = Enable.DeclaringType.GetMethod("BeginDatabase", BindingFlags.NonPublic | BindingFlags.Static);
        var databaseTick = Enable.DeclaringType.GetMethod("TickDatabase", BindingFlags.NonPublic | BindingFlags.Static);
        var phase = Enable.DeclaringType.GetField("phase", BindingFlags.NonPublic | BindingFlags.Static);
        var pending = Enable.DeclaringType.GetField("databaseWrite", BindingFlags.NonPublic | BindingFlags.Static);
        beginDatabase.Invoke(null, null);
        Check(phase.GetValue(null).ToString() == "Database" && pending.GetValue(null) == null,
            "Entering database setup must defer work to the next tick.");
        pending.SetValue(null, Task.FromResult((false, (Exception)new IOException("Busy"))));
        databaseTick.Invoke(null, null);
        Check(phase.GetValue(null).ToString() == "Database" && pending.GetValue(null) == null,
            "A transient failure must remain in database setup and release the completed attempt.");
        pending.SetValue(null, Task.FromResult((false, (Exception)null)));
        databaseTick.Invoke(null, null);
        Check(phase.GetValue(null).ToString() == "Finished" && pending.GetValue(null) == null,
            "Confirmed database completion must finish the attempt and release the task.");
        init.Invoke(null, null);
    }

    public static void RunDatabase(string providerFolder)
    {
        Assembly provider = Assembly.LoadFrom(Path.Combine(providerFolder, "System.Data.SQLite.dll"));
        string folder = Path.Combine(Path.GetTempPath(), "Oracle-vgi-tracking-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        string path = Path.Combine(folder, "_Conquest.db");
        var save = Enable.DeclaringType.GetMethod("SaveTracking", BindingFlags.NonPublic | BindingFlags.Static);
        bool Save(string character, string server = "Conquest") => (bool)save.Invoke(null, new object[] { folder, server, character, CancellationToken.None });
        DbConnection Open()
        {
            var connection = (DbConnection)Activator.CreateInstance(provider.GetType("System.Data.SQLite.SQLiteConnection", true));
            connection.ConnectionString = new DbConnectionStringBuilder { ["Data Source"] = path, ["Pooling"] = false }.ConnectionString;
            connection.Open();
            return connection;
        }
        void Execute(string sql)
        {
            using (var connection = Open())
            using (var command = connection.CreateCommand()) { command.CommandText = sql; command.ExecuteNonQuery(); }
        }
        long Number(string sql)
        {
            using (var connection = Open())
            using (var command = connection.CreateCommand()) { command.CommandText = sql; return Convert.ToInt64(command.ExecuteScalar()); }
        }
        void Fails(Action action, string message)
        {
            try { action(); }
            catch (TargetInvocationException) { return; }
            throw new InvalidOperationException(message);
        }
        try
        {
            Fails(() => Save("Atlas"), "Missing database must not be created.");
            Check(!File.Exists(path), "Fallback created an empty VGI database.");
            Execute("CREATE TABLE CharactersEnabled (CharServer TEXT NOT NULL, CharName TEXT NOT NULL, Tracking BOOLEAN NOT NULL)");
            Execute("CREATE TABLE CharacterInfo (CharServer TEXT NOT NULL, CharName TEXT NOT NULL)");
            Execute("INSERT INTO CharacterInfo VALUES ('Conquest','Untracked')");
            Execute("INSERT INTO CharacterInfo VALUES ('OtherServer','UntrackedElsewhere')");
            Execute("CREATE TABLE ObjectData (ObjectName TEXT)");
            Execute("INSERT INTO ObjectData VALUES ('Keep inventory intact')");
            Execute("INSERT INTO CharactersEnabled VALUES ('Conquest','Atlas',1)");
            Execute("INSERT INTO CharactersEnabled VALUES ('Conquest','Other',2)");
            Execute("INSERT INTO CharactersEnabled VALUES ('OtherServer','Atlas',1)");
            Fails(() => save.Invoke(null, new object[] { folder, "Conquest", "Cancelled", new CancellationToken(true) }),
                "Cancelled setup must not write tracking preferences.");
            Check(Number("SELECT COUNT(*) FROM CharactersEnabled WHERE Tracking=3") == 0,
                "Cancelled setup changed tracking preferences.");
            Check(Save("Atlas") && !Save("Atlas"), "Fallback must update once and recognize an already-enabled character.");
            Check(Number("SELECT CAST(Tracking AS INTEGER) FROM CharactersEnabled WHERE CharServer='Conquest' AND CharName='Atlas'") == 3,
                "Fallback did not persist Track All Items.");
            Check(Number("SELECT CAST(Tracking AS INTEGER) FROM CharactersEnabled WHERE CharName='Other'") == 3
                && Number("SELECT CAST(Tracking AS INTEGER) FROM CharactersEnabled WHERE CharServer='OtherServer'") == 1,
                "Server setup must enable other characters on this server and leave other servers alone.");
            Check(Number("SELECT COUNT(*) FROM CharactersEnabled WHERE CharName='Untracked' AND Tracking=3") == 1
                && Number("SELECT COUNT(*) FROM CharactersEnabled WHERE CharName='UntrackedElsewhere'") == 0,
                "Known but untracked characters must be enrolled only on the selected server.");
            Check(Save("O'Brien") && !Save("O'Brien"), "A new character with punctuation must be inserted exactly once.");
            Check(Number("SELECT COUNT(*) FROM CharactersEnabled") == 5 && Number("SELECT COUNT(*) FROM ObjectData") == 1,
                "Fallback duplicated tracking rows or changed inventory data.");
            Fails(() => Save("Atlas", "../Conquest"), "Invalid server paths must be rejected.");
            using (var locked = Open())
            using (var command = locked.CreateCommand())
            {
                command.CommandText = "BEGIN EXCLUSIVE";
                command.ExecuteNonQuery();
                Fails(() => Save("Locked"), "Locked database must fail for retry.");
                command.CommandText = "ROLLBACK";
                command.ExecuteNonQuery();
            }
            Check(Save("Locked"), "Fallback did not succeed after the database lock was released.");
            Execute("UPDATE CharactersEnabled SET Tracking=1 WHERE CharName='Other'");
            Execute("CREATE TRIGGER RefuseTracking BEFORE UPDATE ON CharactersEnabled WHEN OLD.CharName='Other' BEGIN SELECT RAISE(IGNORE); END");
            Fails(() => Save("MustRollBack"), "An ignored update must not be reported as success.");
            Check(Number("SELECT COUNT(*) FROM CharactersEnabled WHERE CharName='MustRollBack'") == 0,
                "Failed verification must roll back enrollment of new characters.");
            Execute("DROP TRIGGER RefuseTracking");
            Check(Save("Atlas"), "Tracking must recover after the interfering trigger is removed.");
            Execute("CREATE TRIGGER RefuseEnrollment BEFORE INSERT ON CharactersEnabled BEGIN SELECT RAISE(IGNORE); END");
            Fails(() => Save("NotEnrolled"), "A silently ignored current-character enrollment must fail verification.");
            Execute("INSERT INTO CharacterInfo VALUES ('Conquest','NewKnownCharacter')");
            Fails(() => Save("Atlas"), "A silently ignored known-character enrollment must fail verification.");
            Execute("DROP TRIGGER RefuseEnrollment");
            Check(Save("Atlas"), "Known-character enrollment must recover once inserts are allowed.");
            Execute("DROP TABLE CharactersEnabled");
            Fails(() => Save("Atlas"), "Fallback must not invent a missing tracking schema.");
            Check(Number("SELECT COUNT(*) FROM sqlite_master WHERE name='CharactersEnabled'") == 0,
                "Fallback recreated VGI's schema.");
            Console.WriteLine("VGI tracking database tests passed.");
        }
        finally
        {
            foreach (string file in Directory.GetFiles(folder)) File.Delete(file);
            Directory.Delete(folder);
        }
    }

    private static string Apply() => Apply(typeof(Tracking), typeof(View));
    private static string Apply(Type tracking, Type view) => Enable.Invoke(null, new object[] { tracking, view, true }).ToString();
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    // Match the inspected VGI contract without loading or running another plugin.
    private static class Tracking
    {
        public enum eCharacterTrackMode { None, Equipment, Played, Everything }
        public static bool f;
        public static eCharacterTrackMode a;
        public static int Calls;
        public static bool CompleteImmediately = true;
        public static void b(eCharacterTrackMode mode) { Calls++; if (CompleteImmediately) a = mode; }
    }

    private static class View
    {
        public static object h = new object();
        public static object i = new object();
        public static object j = new object();
    }
}
