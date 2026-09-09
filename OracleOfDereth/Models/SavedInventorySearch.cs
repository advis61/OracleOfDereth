using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Xml;
using System.Xml.Serialization;

namespace OracleOfDereth
{
    public sealed class SavedInventorySearch
    {
        public string Token { get; set; } = Guid.NewGuid().ToString("N");
        public string Server { get; set; }
        public string SavedBy { get; set; }
        public ItemFilter Filter { get; set; } = new ItemFilter();
        public string[] CategoryOrder { get; set; } = new string[0];
        public ItemList.SortType Sort { get; set; } = ItemList.SortType.NameAscending;
        public SavedInventorySelection Selection { get; set; }

        private static readonly XmlSerializer Serializer = new XmlSerializer(typeof(SavedInventorySearch));

        // One file per server. All clients use the same lock for reads, writes, and consumption.
        public static SavedInventorySearch Peek(string server, string character, string directory = null) => Locked(server, directory, path =>
        {
            var saved = Read(path, server);
            return saved?.SavedBy == character ? null : saved;
        });

        public void Save(string directory = null)
        {
            Validate(this, Server);
            Locked(Server, directory, path =>
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                using (var stream = File.Create(path)) Serializer.Serialize(stream, this);
                return true;
            });
        }

        // Check the token under the lock so a stale button cannot take a newer save.
        public static SavedInventorySearch Take(string server, string character, string token, string directory = null) => Locked(server, directory, path =>
        {
            var saved = Read(path, server);
            if (saved == null || saved.SavedBy == character || saved.Token != token) return null;
            File.Delete(path);
            return saved;
        });

        private static SavedInventorySearch Read(string path, string server)
        {
            if (!File.Exists(path)) return null;
            using (var reader = XmlReader.Create(path, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null }))
            {
                var saved = (SavedInventorySearch)Serializer.Deserialize(reader);
                Validate(saved, server);
                return saved;
            }
        }

        private static void Validate(SavedInventorySearch saved, string server)
        {
            if (saved == null || string.IsNullOrWhiteSpace(server) || saved.Server != server ||
                string.IsNullOrWhiteSpace(saved.SavedBy) || !Guid.TryParse(saved.Token, out _) ||
                saved.Filter == null || saved.CategoryOrder == null || !Enum.IsDefined(typeof(ItemList.SortType), saved.Sort) ||
                (saved.Selection != null && (saved.Selection.Id == 0 || string.IsNullOrWhiteSpace(saved.Selection.Owner) || string.IsNullOrWhiteSpace(saved.Selection.Name))))
                throw new InvalidDataException("The saved inventory search is invalid.");
        }

        private static T Locked<T>(string server, string directory, Func<string, T> action)
        {
            if (string.IsNullOrWhiteSpace(server)) throw new ArgumentException("A server is required.");
            directory = Path.GetFullPath(directory ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Personal),
                @"Decal Plugins\Oracle of Dereth\saved-inventory-search"));
            string path = Path.Combine(directory, Hash(server) + ".xml");
            using (var mutex = new Mutex(false, @"Local\OracleOfDereth.SavedSearch." + Hash(path.ToUpperInvariant())))
            {
                bool acquired;
                try { acquired = mutex.WaitOne(100); }
                catch (AbandonedMutexException) { acquired = true; }
                if (!acquired) throw new IOException("Saved search is busy; try again in a moment.");
                try { return action(path); }
                finally { mutex.ReleaseMutex(); }
            }
        }

        private static string Hash(string value)
        {
            using (var hash = SHA256.Create())
                return BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(value))).Replace("-", "");
        }
    }

    public sealed class SavedInventorySelection
    {
        public int Id { get; set; }
        public string Owner { get; set; }
        public string Name { get; set; }
        public int ObjectClass { get; set; }
        public int Icon { get; set; }

        public SavedInventorySelection() { }
        public SavedInventorySelection(Item item)
        {
            Id = item.Id; Owner = item.Character; Name = item.Name;
            ObjectClass = (int)item.ObjectClass; Icon = item.Icon;
        }

        public bool Matches(Item item, string server) => item != null && item.Server == server &&
            item.Id == Id && item.Character == Owner && item.Name == Name &&
            (int)item.ObjectClass == ObjectClass && (Icon == 0 || item.Icon == Icon);
    }
}
