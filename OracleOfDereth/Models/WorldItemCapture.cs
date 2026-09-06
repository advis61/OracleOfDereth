using Decal.Adapter;
using Decal.Adapter.Wrappers;
using System;
using System.Collections.Generic;
using DecalWorldObject = Decal.Adapter.Wrappers.WorldObject;

namespace OracleOfDereth
{
    // The live-source boundary. Decal owns WorldObject; this helper only reads it and
    // returns our immutable Item. All calls happen on the plugin's game thread.
    public static class WorldItemCapture
    {
        public static Item Capture(DecalWorldObject worldObject)
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
            return new Item(server, character, worldObject.Id, worldObject.Name, worldObject.ObjectClass,
                integers, strings, booleans, doubles, spells: spells,
                activeSpells: worldObject.HasIdData ? activeSpells : null, hasIdData: worldObject.HasIdData,
                holderLevel: holderLevel, icon: worldObject.Icon, container: worldObject.Container);
        }
    }
}
