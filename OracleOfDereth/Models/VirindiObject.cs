using Decal.Adapter.Wrappers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace OracleOfDereth
{
    // The property surface used by ItemInfo. Live objects delegate to Decal; VGI objects
    // read the same keys from its saved snapshot, without touching the WorldFilter.
    public sealed class VirindiObject
    {
        private readonly WorldObject live;
        private readonly Dictionary<int, int> integers;
        private readonly Dictionary<int, string> strings;
        private readonly Dictionary<int, bool> booleans;
        private readonly Dictionary<int, double> doubles;
        private readonly Dictionary<int, long> int64s;
        private readonly List<int> spells;
        private readonly int id;
        private readonly string name;
        private readonly ObjectClass objectClass;

        public bool IsSnapshot => live == null;
        public string OwnerServer { get; } = "";
        public string OwnerCharName { get; } = "";
        public int Id => live?.Id ?? id;
        public string Name => live?.Name ?? name;
        public ObjectClass ObjectClass => live?.ObjectClass ?? objectClass;
        public int Icon => live?.Icon ?? Values((LongValueKey)218103809);
        public int Container => live?.Container ?? Values((LongValueKey)218103810);
        public bool HasIdData => live?.HasIdData ?? true;
        public IEnumerable<int> LongKeys => live != null ? (IEnumerable<int>)live.LongKeys : integers.Keys;
        public IEnumerable<int> DoubleKeys => live != null ? (IEnumerable<int>)live.DoubleKeys : doubles.Keys;
        public int SpellCount => live?.SpellCount ?? spells.Count;
        public int Spell(int index) => live != null ? live.Spell(index) : spells[index];
        // VGI serializes five property dictionaries, innate spells and a trailing marker.
        // It does not serialize the active-spell list.
        public int ActiveSpellCount => live?.ActiveSpellCount ?? 0;
        public int ActiveSpell(int index) => live != null ? live.ActiveSpell(index) : throw new ArgumentOutOfRangeException(nameof(index));

        public VirindiObject(WorldObject worldObject)
        {
            live = worldObject ?? throw new ArgumentNullException(nameof(worldObject));
        }

        public static implicit operator VirindiObject(WorldObject worldObject) => worldObject == null ? null : new VirindiObject(worldObject);

        public VirindiObject(string server, string character, int objectId, string objectName, ObjectClass category, byte[] data)
        {
            integers = new Dictionary<int, int>();
            strings = new Dictionary<int, string>();
            booleans = new Dictionary<int, bool>();
            doubles = new Dictionary<int, double>();
            int64s = new Dictionary<int, long>();
            spells = new List<int>();
            OwnerServer = server;
            OwnerCharName = character;
            id = objectId;
            name = objectName;
            objectClass = category;
            if (data == null) throw new ArgumentNullException(nameof(data));
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
                reader.ReadInt32(); // VGI's trailing object marker (usually -1).
                if (stream.Position != stream.Length) throw new InvalidDataException("Unsupported VGI item data.");
            }
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

        public int Values(LongValueKey key, int fallback = 0) => live != null ? live.Values(key, fallback) : integers.TryGetValue((int)key, out int value) ? value : fallback;
        public double Values(DoubleValueKey key, double fallback = 0) => live != null ? live.Values(key, fallback) : doubles.TryGetValue((int)key, out double value) ? value : fallback;
        public string Values(StringValueKey key, string fallback = "") => live != null ? live.Values(key, fallback) : strings.TryGetValue((int)key, out string value) ? value : fallback;
        public bool Values(BoolValueKey key, bool fallback = false) => live != null ? live.Values(key, fallback) : booleans.TryGetValue((int)key, out bool value) ? value : fallback;
    }
}
