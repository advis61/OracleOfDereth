using Decal.Adapter.Wrappers;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Reflection;

namespace OracleOfDereth
{
    // Owns the saved inventory for one server. Connections are short-lived and read-only;
    // filtering/sorting happens in memory so typing never holds VGI's database open.
    public sealed class VGInventory
    {
        private const string PluginKey = @"SOFTWARE\Decal\Plugins\{EB071330-DC65-4302-9CF9-6104B5B4C73B}";
        private readonly string directory;
        public ItemList List { get; } = new ItemList();
        public string ServerName { get; private set; }
        public string Error { get; private set; } = "";
        public int UnreadableCount { get; private set; }
        public DateTime? LoadedAt { get; private set; }

        // Explicit directory is also useful for reading a copied database in diagnostics.
        public VGInventory(string directory = null) { this.directory = directory; }

        public List<Item> Search(ItemFilter filter) => List.Items.Where(filter.Matches).ToList();

        public bool Refresh(string server)
        {
            if (ServerName != server)
            {
                List.Clear();
                LoadedAt = null;
                UnreadableCount = 0;
            }
            ServerName = server;
            Error = "";
            try
            {
                if (string.IsNullOrWhiteSpace(server)) throw new InvalidOperationException("Log in to view this server's inventory.");
                if (server.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0) throw new InvalidOperationException("Invalid server name.");
                string folder = directory ?? FindDirectory();
                if (folder == null) throw new InvalidOperationException("VGI is not installed. Use Items to add and identify items.");
                string path = Path.Combine(folder, "_" + server + ".db");
                if (!File.Exists(path)) throw new InvalidOperationException("No saved VGI inventory for " + server + ". Use Items to add and identify items.");

                var objects = new List<VirindiObject>();
                var unreadable = new List<Item>();
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
                            try
                            {
                                objects.Add(new VirindiObject(ownerServer, character, id, name, category, (byte[])reader.GetValue(5)));
                            }
                            catch (Exception ex) when (ex is IOException || ex is InvalidDataException)
                            {
                                // Keep the owner/name visible even if an old or truncated blob
                                // cannot supply details. Never queue an offline item for ID.
                                unreadable.Add(new Item { Id = id, Name = name, Character = character, Server = ownerServer,
                                    SortCategory = 9, Description = name + " (saved details unavailable)" });
                            }
                        }
                    }
                }
                List.Load(objects);
                List.Items.AddRange(unreadable);
                List.Sort(List.CurrentSortType);
                UnreadableCount = unreadable.Count;
                LoadedAt = DateTime.Now;
                return true;
            }
            catch (Exception ex)
            {
                // Keep the last successful snapshot on a transient lock/read error for the
                // same server. A server change clears it above, before any attempted read.
                Error = "VGI: " + ex.GetBaseException().Message;
                return false;
            }
        }

        private static DbConnection OpenConnection(string folder, string path)
        {
            // VGI supplies its SQLite provider. Late binding keeps it optional for users
            // without VGI and avoids shipping a competing mixed-mode SQLite DLL.
            Assembly provider = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == "System.Data.SQLite")
                ?? Assembly.LoadFrom(Path.Combine(folder, "System.Data.SQLite.dll"));
            var connection = (DbConnection)Activator.CreateInstance(provider.GetType("System.Data.SQLite.SQLiteConnection", true));
            var settings = new DbConnectionStringBuilder
            {
                ["Data Source"] = path,
                ["Read Only"] = true,
                ["FailIfMissing"] = true,
                ["Pooling"] = false,
                ["Default Timeout"] = 2
            };
            connection.ConnectionString = settings.ConnectionString;
            try { connection.Open(); return connection; }
            catch { connection.Dispose(); throw; }
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
    }
}
