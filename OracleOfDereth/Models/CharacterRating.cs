using System.Collections.Generic;

using Decal.Adapter;
using Decal.Adapter.Wrappers;

namespace OracleOfDereth
{
    public static class CharacterRating
    {
        public const int Damage = 307;
        public const int DamageResist = 308;
        public const int CritDamage = 314;
        public const int CritResist = 315;
        public const int CritDamageResist = 316;
        public const int Vitality = 379;

        public static string Summary()
        {
            if (CoreManager.Current.CharacterFilter.LoginStatus < 1) return "";

            WorldObject self = CoreManager.Current.WorldFilter[CoreManager.Current.CharacterFilter.Id];
            if (self == null) return "";

            var parts = new List<string>();
            AddPair(parts, self, "Dam", Damage, CritDamage);
            AddPair(parts, self, "Def", DamageResist, CritDamageResist);

            int vitality = Value(self, Vitality);
            if (vitality != 0) parts.Add($"{vitality}V");

            return string.Join(", ", parts);
        }

        private static void AddPair(List<string> parts, WorldObject self, string label, int plainKey, int critKey)
        {
            int plain = Value(self, plainKey);
            int crit = Value(self, critKey);

            if (plain == 0 && crit == 0) return;

            parts.Add($"{label}: {plain}/{crit}");
        }

        private static int Value(WorldObject self, int key) => self.Values((LongValueKey)key, 0);
    }
}
