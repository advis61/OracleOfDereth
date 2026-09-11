using Decal.Adapter.Wrappers;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace OracleOfDereth
{
    // Owns the saved inventory for one server. Connections are short-lived and read-only;
    // scans retain only the best matching rows, using the same ordering as live lists.
    public sealed class VGInventory
    {
        private const string PluginKey = @"SOFTWARE\Decal\Plugins\{EB071330-DC65-4302-9CF9-6104B5B4C73B}";
        private readonly string directory;
        public const int ResultLimit = 5000;
        public int MatchCount { get; private set; }
        public int TotalCount { get; private set; }
        public ItemList List { get; } = new ItemList { CurrentSortType = ItemList.SortType.CurrentCharacterFirst };
        public string ServerName { get; private set; }
        public string Error { get; private set; } = "";
        public int UnreadableCount { get; private set; }
        public DateTime? LoadedAt { get; private set; }

        // Explicit directory is also useful for reading a copied database in diagnostics.
        public VGInventory(string directory = null) { this.directory = directory; }

        private IEnumerator<bool> scan;
        public bool IsSearching => scan != null;
        public int ScannedCount { get; private set; }

        public void CancelSearch()
        {
            scan?.Dispose();
            scan = null;
        }

        // Keep search ordering, but release observations and their backing array.
        public void ReleaseResults()
        {
            CancelSearch();
            List.Items.Clear();
            List.Items = new List<ItemListRow>();
            LoadedAt = null;
            UnreadableCount = MatchCount = TotalCount = ScannedCount = 0;
            Error = "";
        }

        // Run on the game thread: row calculations use Decal's spell metadata.
        public void BeginRefresh(string server, ItemFilter filter = null)
        {
            ReleaseResults();
            ServerName = server;
            Error = "";
            ScannedCount = 0;
            scan = Scan(server, filter ?? new ItemFilter(), List.CurrentSortType, List.PriorityCharacter).GetEnumerator();
        }

        // Each step processes at most 32 records; disposing cancels and closes SQLite.
        public bool AdvanceSearch()
        {
            if (scan == null) return false;
            try
            {
                if (scan.MoveNext()) return true;
            }
            catch (Exception ex)
            {
                Error = "VGI: " + (ex is SQLiteProviderException ? ex.Message : ex.GetBaseException().Message);
            }
            CancelSearch();
            return false;
        }

        // Synchronous entry point for diagnostics and regression tests.
        public bool Refresh(string server, ItemFilter filter = null)
        {
            BeginRefresh(server, filter);
            while (AdvanceSearch()) { }
            return string.IsNullOrEmpty(Error);
        }

        private IEnumerable<bool> Scan(string server, ItemFilter filter, ItemList.SortType sort, string priorityCharacter)
        {
            if (filter.SearchError != null) throw new InvalidOperationException(filter.SearchError);
            if (string.IsNullOrWhiteSpace(server)) throw new InvalidOperationException("Log in to view this server's inventory.");
            if (server.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0) throw new InvalidOperationException("Invalid server name.");
            string folder = directory ?? FindDirectory();
            if (folder == null) throw new InvalidOperationException("Virindi Global Inventory decal plugin is not installed. Use items to add and identify items.");
            string path = Path.Combine(folder, "_" + server + ".db");
            if (!File.Exists(path)) throw new InvalidOperationException("No saved VGI inventory for " + server + ". Use Items to add and identify items.");

            var rows = new List<ItemListRow>();
            int matches = 0, total = 0;
            int unreadable = 0;
            using (DbConnection connection = OpenConnection(folder, path))
            using (DbCommand command = connection.CreateCommand())
            {
                command.CommandTimeout = 2;
                command.CommandText = "SELECT OwnerServer, OwnerCharName, ObjectID, ObjectName, ObjectClass, SerializedData FROM ObjectData WHERE OwnerServer = @server";
                DbParameter parameter = command.CreateParameter();
                parameter.ParameterName = "@server";
                parameter.Value = server;
                command.Parameters.Add(parameter);
                using (DbDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string ownerServer = reader.GetString(0);
                        string character = reader.GetString(1);
                        int id = unchecked((int)reader.GetInt64(2));
                        string name = reader.GetString(3);
                        var category = (ObjectClass)reader.GetInt32(4);
                        total++;
                        Item item;
                        try
                        {
                            item = DecodeItem(ownerServer, character, id, name, category, (byte[])reader.GetValue(5));
                        }
                        catch (Exception ex) when (ex is IOException || ex is InvalidDataException)
                        {
                            // Keep the owner/name visible even if an old or truncated blob
                            // cannot supply details. Never queue an offline item for ID.
                            item = new Item(ownerServer, character, id, name, category);
                            unreadable++;
                        }
                        var row = new ItemListRow(item);
                        row.Populate();
                        if (filter.Matches(row))
                        {
                            matches++;
                            rows.Add(row);
                            // Amortize sorting in small batches, retaining the global best results.
                            if (rows.Count >= ResultLimit + 256)
                                rows = ItemList.OrderRows(rows, sort, priorityCharacter).Take(ResultLimit).ToList();
                        }
                        if (filter.SearchError != null) throw new InvalidOperationException(filter.SearchError);
                        ScannedCount = total;
                        if (total % 32 == 0) yield return true;
                    }
                }
            }
            List.Items = ItemList.OrderRows(rows, sort, priorityCharacter).Take(ResultLimit).ToList();
            MatchCount = matches;
            TotalCount = total;
            UnreadableCount = unreadable;
            LoadedAt = DateTime.Now;
        }

        private static DbConnection OpenConnection(string folder, string path)
        {
            DbConnection connection = null;
            try
            {
                // VGI supplies its SQLite provider. Keep it optional and reuse it if already loaded.
                Assembly provider = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == "System.Data.SQLite")
                    ?? Assembly.LoadFrom(Path.Combine(folder, "System.Data.SQLite.dll"));
                connection = (DbConnection)Activator.CreateInstance(provider.GetType("System.Data.SQLite.SQLiteConnection", true));
                var settings = new DbConnectionStringBuilder
                {
                    ["Data Source"] = path,
                    ["Read Only"] = true,
                    ["FailIfMissing"] = true,
                    ["Pooling"] = false,
                    ["Default Timeout"] = 2
                };
                connection.ConnectionString = settings.ConnectionString;
                connection.Open();
                return connection;
            }
            catch (Exception ex)
            {
                connection?.Dispose();
                Exception cause = ex.GetBaseException();
                if (cause is FileNotFoundException || cause is FileLoadException || cause is BadImageFormatException ||
                    cause is DllNotFoundException || cause is EntryPointNotFoundException || cause is TypeLoadException)
                    throw new SQLiteProviderException(ex);
                throw;
            }
        }

        private sealed class SQLiteProviderException : Exception
        {
            public SQLiteProviderException(Exception inner) : base("Virindi Global Inventory's SQLite component is missing or could not load. Reinstall the VGI decal plugin", inner) { } 
        }

        private static string FindDirectory()
        {
            Assembly vgi = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == "VirindiGlobalInventory");
            if (vgi != null && !string.IsNullOrEmpty(vgi.Location)) return Path.GetDirectoryName(vgi.Location);
            foreach (RegistryHive hive in new[] { RegistryHive.LocalMachine, RegistryHive.CurrentUser })
            {
                using (RegistryKey root = RegistryKey.OpenBaseKey(hive, RegistryView.Registry32))
                using (RegistryKey key = root.OpenSubKey(PluginKey))
                {
                    string path = key?.GetValue("Path") as string;
                    if (!string.IsNullOrEmpty(path) && Directory.Exists(path)) return path;
                }
            }
            return null;
        }
        // VGI's five property dictionaries, innate spells and trailing marker. The
        // active-spell list is absent, so the resulting Item explicitly leaves it unknown.
        public static Item DecodeItem(string server, string character, int id, string name, ObjectClass category, byte[] data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            var integers = new Dictionary<int, int>();
            var strings = new Dictionary<int, string>();
            var booleans = new Dictionary<int, bool>();
            var doubles = new Dictionary<int, double>();
            var int64s = new Dictionary<int, long>();
            var spells = new List<int>();
            using (var stream = new MemoryStream(data, false))
            using (var reader = new BinaryReader(stream))
            {
                ReadProperties(reader, integers, 8, r => r.ReadInt32());
                ReadProperties(reader, strings, 5, ReadString);
                ReadProperties(reader, booleans, 5, r => r.ReadBoolean());
                ReadProperties(reader, doubles, 12, r => r.ReadDouble());
                ReadProperties(reader, int64s, 12, r => r.ReadInt64());
                int count = ReadCount(reader, 4);
                for (int i = 0; i < count; i++) spells.Add(reader.ReadInt32());
                reader.ReadInt32();
                if (stream.Position != stream.Length) throw new InvalidDataException("Unsupported VGI item data.");
            }
            return new Item(integers, strings, booleans, doubles, int64s, spells.ToArray(),
                server, character, id, name, category, hasIdData: true);
        }

        private static void ReadProperties<T>(BinaryReader reader, Dictionary<int, T> values, int minimumSize, Func<BinaryReader, T> read)
        {
            int count = ReadCount(reader, minimumSize);
            for (int i = 0; i < count; i++)
            {
                int key = reader.ReadInt32();
                values[key] = read(reader);
            }
        }

        private static int ReadCount(BinaryReader reader, int minimumSize)
        {
            int count = reader.ReadInt32();
            if (count < 0 || count > (reader.BaseStream.Length - reader.BaseStream.Position) / minimumSize)
                throw new InvalidDataException("Invalid VGI property count.");
            return count;
        }

        private static string ReadString(BinaryReader reader)
        {
            // VGI uses ASCII with a variable-length prefix: three 7-bit groups, then
            // an optional full fourth byte. Do not use BinaryReader.ReadString (UTF-8).
            uint length = 0;
            for (int group = 0; group < 4; group++)
            {
                byte next = reader.ReadByte();
                length |= (uint)(group == 3 ? next : next & 127) << (group * 7);
                if (group == 3 || (next & 128) == 0) break;
            }
            if (length > reader.BaseStream.Length - reader.BaseStream.Position)
                throw new InvalidDataException("Invalid VGI string length.");
            return Encoding.ASCII.GetString(reader.ReadBytes((int)length));
        }

    }
}
