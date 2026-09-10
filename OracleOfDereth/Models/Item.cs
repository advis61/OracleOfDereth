using Decal.Adapter;
using DecalWorldObject = Decal.Adapter.Wrappers.WorldObject;
using Decal.Adapter.Wrappers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OracleOfDereth
{
    // A single observation of an item. Caller-owned property collections are copied
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

        // Capture live properties and ownership on the game thread without retaining Decal objects.
        public Item(DecalWorldObject worldObject)
        {
            if (worldObject == null) throw new ArgumentNullException(nameof(worldObject));
            var integers = new Dictionary<int, int>();
            var doubles = new Dictionary<int, double>();
            var strings = new Dictionary<int, string>();
            var booleans = new Dictionary<int, bool>();
            var spells = new List<int>();
            var activeSpells = new List<int>();
            foreach (int key in worldObject.LongKeys) integers[key] = worldObject.Values((LongValueKey)key);
            foreach (int key in worldObject.DoubleKeys) doubles[key] = worldObject.Values((DoubleValueKey)key);
            foreach (int key in worldObject.StringKeys) strings[key] = worldObject.Values((StringValueKey)key);
            foreach (int key in worldObject.BoolKeys) booleans[key] = worldObject.Values((BoolValueKey)key);

            // Some quest weapons expose these values without enumerating their keys.
            foreach (LongValueKey key in new[] { LongValueKey.MaxDamage, LongValueKey.ElementalDmgBonus, (LongValueKey)353 })
            {
                int value = worldObject.Values(key, 0);
                if (!integers.ContainsKey((int)key) && value != 0) integers[(int)key] = value;
            }
            foreach (DoubleValueKey key in new[] { DoubleValueKey.DamageBonus, DoubleValueKey.AttackBonus, DoubleValueKey.MeleeDefenseBonus, DoubleValueKey.ElementalDamageVersusMonsters })
            {
                double value = worldObject.Values(key, 0);
                if (!doubles.ContainsKey((int)key) && value != 0) doubles[(int)key] = value;
            }
            for (int i = 0; i < worldObject.SpellCount; i++) spells.Add(worldObject.Spell(i));
            for (int i = 0; i < worldObject.ActiveSpellCount; i++) activeSpells.Add(worldObject.ActiveSpell(i));

            string server = CoreManager.Current?.CharacterFilter?.Server ?? "";
            string character = "";
            int? holderLevel = null;
            // Capture ownership and equipment context now. Calculations must never
            // resolve a snapshot's container ID against a later/different world.
            var visited = new HashSet<int>();
            int container = worldObject.Container;
            var worldFilter = CoreManager.Current?.WorldFilter;
            while (worldFilter != null && container != 0 && visited.Add(container))
            {
                DecalWorldObject owner = worldFilter[container];
                if (owner == null) break;
                if (owner.ObjectClass == ObjectClass.Player)
                {
                    character = owner.Name;
                    if (container == worldObject.Container)
                    {
                        int level = owner.Values((LongValueKey)25, 0);
                        if (level > 0) holderLevel = level;
                    }
                    break;
                }
                container = owner.Container;
            }
            Server = server;
            Character = character;
            Id = worldObject.Id;
            Name = worldObject.Name ?? "";
            ObjectClass = worldObject.ObjectClass;
            this.integers = integers;
            this.strings = strings;
            this.booleans = booleans;
            this.doubles = doubles;
            int64s = new Dictionary<int, long>();
            this.spells = spells.ToArray();
            this.activeSpells = worldObject.HasIdData ? activeSpells.ToArray() : null;
            HasIdData = worldObject.HasIdData;
            HolderLevel = holderLevel;
            Icon = worldObject.Icon;
            Container = worldObject.Container;
        }

        // An omitted property or spell list means unavailable, not a guessed zero/empty
        // appraisal. Callers supply the metadata they actually know about the owner.
        public Item(string server, string character, int id, string name, ObjectClass category,
            IDictionary<int, int> integers = null, IDictionary<int, string> strings = null,
            IDictionary<int, bool> booleans = null, IDictionary<int, double> doubles = null,
            IDictionary<int, long> int64s = null, IEnumerable<int> spells = null,
            IEnumerable<int> activeSpells = null, bool hasIdData = false,
            int? holderLevel = null, int? icon = null, int? container = null)
            : this(
                integers == null ? new Dictionary<int, int>() : new Dictionary<int, int>(integers),
                strings == null ? new Dictionary<int, string>() : new Dictionary<int, string>(strings),
                booleans == null ? new Dictionary<int, bool>() : new Dictionary<int, bool>(booleans),
                doubles == null ? new Dictionary<int, double>() : new Dictionary<int, double>(doubles),
                int64s == null ? new Dictionary<int, long>() : new Dictionary<int, long>(int64s),
                spells?.ToArray() ?? Array.Empty<int>(), server, character, id, name, category, hasIdData)
        {
            this.activeSpells = activeSpells?.ToArray();
            HolderLevel = holderLevel;
            Icon = icon ?? Icon;
            Container = container ?? Container;
        }

        // Takes ownership of freshly built collections. Callers must not retain or mutate them.
        // VGI can hand over its decoded properties without allocating a second set of dictionaries.
        internal Item(Dictionary<int, int> integers, Dictionary<int, string> strings,
            Dictionary<int, bool> booleans, Dictionary<int, double> doubles,
            Dictionary<int, long> int64s, int[] spells,
            string server, string character, int id, string name, ObjectClass category, bool hasIdData)
        {
            Server = server ?? "";
            Character = character ?? "";
            Id = id;
            Name = name ?? "";
            ObjectClass = category;
            this.integers = integers;
            this.strings = strings;
            this.booleans = booleans;
            this.doubles = doubles;
            this.int64s = int64s;
            this.spells = spells;
            HasIdData = hasIdData;
            Icon = Values((LongValueKey)218103809);
            Container = Values((LongValueKey)218103810);
        }

        public bool TryGetValue(LongValueKey key, out int value) => integers.TryGetValue((int)key, out value);
        public bool TryGetValue(DoubleValueKey key, out double value) => doubles.TryGetValue((int)key, out value);

        // Enumerate without exposing the underlying arrays to mutation.
        public IEnumerable<int> Spells
        {
            get { foreach (int spell in spells) yield return spell; }
        }

        // HasActiveSpellData still distinguishes unknown buffs from a known empty list.
        public IEnumerable<int> ActiveSpells
        {
            get
            {
                if (activeSpells == null) yield break;
                foreach (int spell in activeSpells) yield return spell;
            }
        }

        public int Values(LongValueKey key, int fallback = 0) => integers.TryGetValue((int)key, out int value) ? value : fallback;
        public double Values(DoubleValueKey key, double fallback = 0) => doubles.TryGetValue((int)key, out double value) ? value : fallback;
        public string Values(StringValueKey key, string fallback = "") => strings.TryGetValue((int)key, out string value) ? value : fallback;
        public bool Values(BoolValueKey key, bool fallback = false) => booleans.TryGetValue((int)key, out bool value) ? value : fallback;
    }
}
