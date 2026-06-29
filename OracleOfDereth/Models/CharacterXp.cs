using System;
using Decal.Adapter;

namespace OracleOfDereth
{
    // Character level + "XP to next level" for the Conquest (Custom) tab. The stock client only
    // knows the retail XP table (levels <= 275), so CharacterFilter.XPToNextLevel is correct up to
    // 275 but reads wrong for Conquest's custom 276-300 range. Past 275 we reproduce Conquest-ACE's
    // dynamic XP curve (Player_Xp.GenerateDynamicLevelPostMax): each level past 275 costs the prior
    // delta grown by LevelRatio, accumulated onto the level-275 total. Level/TotalXP come straight
    // from Decal — TotalXP is Int64, so it holds the full past-275 value the client still tracks.
    public static class CharacterXp
    {
        public const int MaxLevel = 300;                  // Conquest max level
        private const long Xp275 = 191226310247L;         // total XP to reach level 275
        private const long Xp274To275Delta = 3390451400L; // XP from 274 -> 275
        private const double LevelRatio = 0.014234603;    // ~1.42% delta growth per level past 275

        public static int Level => CoreManager.Current.CharacterFilter.Level;

        // Cumulative total XP required to reach targetLevel past 275 (mirrors Conquest-ACE).
        public static double TotalXpForLevel(int targetLevel)
        {
            if (targetLevel <= 275) return Xp275;

            double prevDelta = Xp274To275Delta;
            double total = Xp275;
            for (int i = 275; i < targetLevel; i++)
            {
                double delta = prevDelta + (prevDelta * LevelRatio);
                total += delta;
                prevDelta = delta;
            }
            return total;
        }

        // XP remaining to the next level. Uses the client's own value up to 275 (it matches the
        // retail table Conquest also uses there); computes it past 275 where the client can't.
        public static long XpToNextLevel
        {
            get
            {
                var cf = CoreManager.Current.CharacterFilter;
                int level = cf.Level;
                if (level >= MaxLevel) return 0;
                if (level < 275) return cf.XPToNextLevel;

                long remaining = (long)(TotalXpForLevel(level + 1) - cf.TotalXP);
                return remaining < 0 ? 0 : remaining;
            }
        }

        // e.g. "275 (1,344,555 xp to level)" or "300 (max level)". Never throws — if a Decal
        // member is unavailable at runtime it falls back to whatever level it can read.
        public static string LevelSummary()
        {
            try
            {
                int level = Level;
                if (level >= MaxLevel) return $"{level} (max level)";
                return $"{level} ({XpToNextLevel:N0} xp to level)";
            }
            catch (Exception ex)
            {
                Util.Log(ex);
                return "-";
            }
        }
    }
}
