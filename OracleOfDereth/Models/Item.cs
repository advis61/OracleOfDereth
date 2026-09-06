using Decal.Adapter.Wrappers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OracleOfDereth
{
    // A single observation of an item. Property collections are copied on construction
    // and never mutated or exposed. No WorldObject, database, or UI references survive here.
    public sealed class Item
    {
        private readonly Dictionary<int, int> integers;
        private readonly Dictionary<int, string> strings;
        private readonly Dictionary<int, bool> booleans;
        private readonly Dictionary<int, double> doubles;
        private readonly Dictionary<int, long> int64s;
        private readonly int[] spells;
        private readonly int[] activeSpells;

        public string Server { get; }
        public string Character { get; }
        public int Id { get; }
        public string Name { get; }
        public ObjectClass ObjectClass { get; }
        public int Icon { get; }
        public int Container { get; }
        public bool HasIdData { get; }
        public int? HolderLevel { get; }
        public bool HasActiveSpellData => activeSpells != null;
        public IEnumerable<int> LongKeys => integers.Keys;
        public IEnumerable<int> DoubleKeys => doubles.Keys;
        public int SpellCount => spells.Length;
        public int Spell(int index) => spells[index];
        public int ActiveSpellCount => activeSpells?.Length ?? 0;
        public int ActiveSpell(int index) => activeSpells != null ? activeSpells[index] : throw new InvalidOperationException("Active spells were not captured.");

        // An omitted property or spell list means unavailable, not a guessed zero/empty
        // appraisal. Callers supply the metadata they actually know about the owner.
        public Item(string server, string character, int id, string name, ObjectClass category,
            IDictionary<int, int> integers = null, IDictionary<int, string> strings = null,
            IDictionary<int, bool> booleans = null, IDictionary<int, double> doubles = null,
            IDictionary<int, long> int64s = null, IEnumerable<int> spells = null,
            IEnumerable<int> activeSpells = null, bool hasIdData = false,
            int? holderLevel = null, int? icon = null, int? container = null)
        {
            Server = server ?? "";
            Character = character ?? "";
            Id = id;
            Name = name ?? "";
            ObjectClass = category;
            this.integers = integers == null ? new Dictionary<int, int>() : new Dictionary<int, int>(integers);
            this.strings = strings == null ? new Dictionary<int, string>() : new Dictionary<int, string>(strings);
            this.booleans = booleans == null ? new Dictionary<int, bool>() : new Dictionary<int, bool>(booleans);
            this.doubles = doubles == null ? new Dictionary<int, double>() : new Dictionary<int, double>(doubles);
            this.int64s = int64s == null ? new Dictionary<int, long>() : new Dictionary<int, long>(int64s);
            this.spells = spells?.ToArray() ?? new int[0];
            this.activeSpells = activeSpells?.ToArray();
            HasIdData = hasIdData;
            HolderLevel = holderLevel;
            Icon = icon ?? Values((LongValueKey)218103809);
            Container = container ?? Values((LongValueKey)218103810);
        }

        public int Values(LongValueKey key, int fallback = 0) => integers.TryGetValue((int)key, out int value) ? value : fallback;
        public double Values(DoubleValueKey key, double fallback = 0) => doubles.TryGetValue((int)key, out double value) ? value : fallback;
        public string Values(StringValueKey key, string fallback = "") => strings.TryGetValue((int)key, out string value) ? value : fallback;
        public bool Values(BoolValueKey key, bool fallback = false) => booleans.TryGetValue((int)key, out bool value) ? value : fallback;
    }
}
