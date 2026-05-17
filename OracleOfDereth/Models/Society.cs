using Decal.Adapter;

namespace OracleOfDereth
{
    public static class Society
    {
        private const int PropCelhan = 287;
        private const int PropEldweb = 288;
        private const int PropRadblo = 289;

        public static int GetRankValue()
        {
            int celhan = CoreManager.Current.CharacterFilter.GetCharProperty(PropCelhan);
            if (celhan > 0) return celhan;

            int eldweb = CoreManager.Current.CharacterFilter.GetCharProperty(PropEldweb);
            if (eldweb > 0) return eldweb;

            int radblo = CoreManager.Current.CharacterFilter.GetCharProperty(PropRadblo);
            if (radblo > 0) return radblo;

            return 0;
        }

        public static string GetSocietyName()
        {
            if (CoreManager.Current.CharacterFilter.GetCharProperty(PropCelhan) > 0) return "Celestial Hand";
            if (CoreManager.Current.CharacterFilter.GetCharProperty(PropEldweb) > 0) return "Eldrytch Web";
            if (CoreManager.Current.CharacterFilter.GetCharProperty(PropRadblo) > 0) return "Radiant Blood";
            return "None";
        }

        public static string GetRankName()
        {
            int value = GetRankValue();
            if (value >= 995) return "Master";
            if (value >= 601) return "Lord";
            if (value >= 301) return "Knight";
            if (value >= 101) return "Adept";
            if (value >= 1) return "Initiate";
            return "None";
        }

        public static int GetDailyLimit()
        {
            int value = GetRankValue();
            if (value >= 601) return 200;
            if (value >= 301) return 150;
            if (value >= 101) return 100;
            if (value >= 1) return 50;
            return 0;
        }

        public static int GetNextRankThreshold()
        {
            int value = GetRankValue();
            if (value >= 995) return 0;
            if (value >= 601) return 995;
            if (value >= 301) return 595;
            if (value >= 101) return 295;
            if (value >= 1) return 95;
            return 0;
        }

        public static string GetNextRankName()
        {
            int value = GetRankValue();
            if (value >= 995) return "";
            if (value >= 601) return "Master";
            if (value >= 301) return "Lord";
            if (value >= 101) return "Knight";
            if (value >= 1) return "Adept";
            return "";
        }

        public static int GetRibbonsToNextRank()
        {
            int threshold = GetNextRankThreshold();
            if (threshold == 0) return 0;
            return threshold - GetRankValue();
        }

        public static int GetRibbonsToday()
        {
            QuestFlag.QuestFlags.TryGetValue("societyribbonsperdaycounter", out QuestFlag counter);
            if (counter == null) return 0;
            return counter.Solves;
        }

        public static bool HasReachedRank(string rankName)
        {
            int value = GetRankValue();
            switch (rankName)
            {
                case "Initiate": return value >= 1;
                case "Adept": return value >= 101;
                case "Knight": return value >= 301;
                case "Lord": return value >= 601;
                case "Master": return value >= 995;
                default: return false;
            }
        }

        // Displayed ribbon cap. Ribbons are turned in 5 at a time, so the in-game
        // cap lands on the nicer test-threshold numbers (95/295/595/995) rather
        // than 94/294/etc. Used only for the "Rank Progress" display.
        public static int GetRankMax()
        {
            int value = GetRankValue();
            if (value >= 995) return 1000;
            if (value >= 601) return 995;
            if (value >= 301) return 595;
            if (value >= 101) return 295;
            if (value >= 1) return 95;
            return 0;
        }
    }
}
