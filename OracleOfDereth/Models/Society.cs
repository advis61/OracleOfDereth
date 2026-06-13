using Decal.Adapter;


// societyribbonsperdaycounter: "Limiter for amount of ribbons a player has turned in per day CompletedOn:5/16/2026 2:59:00 AM Solves:30 MaxSolves:200 RepeatTime:0s
// societyribbonsperdaytimer:  "Timer for how often a player can turn in the per rank, per day limit of ribbons CompletedOn:5/16/2026 2:58:37 AM Solves:1 MaxSolves:-1 RepeatTime:20h
// https://github.com/ACEmulator/ACE-World-16PY-Patches/blob/master/Database/Patches/9%20WeenieDefaults/Creature/Human/38233.es
//
// Then I handed in 4, but it took 5 from my inventory
// 
// societyribbonsperdaycounter: "Limiter for amount of ribbons a player has turned in per day CompletedOn:5/23/2026 1:12:35 AM Solves:5 MaxSolves:200 RepeatTime:0s
// societyribbonsperdaytimer: "Timer for how often a player can turn in the per rank, per day limit of ribbons CompletedOn:5/23/2026 1:12:35 AM Solves:2 MaxSolves:-1 RepeatTime:20h
//
// "Sorry! I only deal in transactions in lots of 5, and you have less than that, so you'll need to get more before you come back here."
//
// Turned in 2 more stacks.
// societyribbonsperdaycounter: "Limiter for amount of ribbons a player has turned in per day CompletedOn:5/23/2026 1:24:18 AM Solves:15 MaxSolves:200 RepeatTime:0s
// societyribbonsperdaytimer:  "Timer for how often a player can turn in the per rank, per day limit of ribbons CompletedOn:5/23/2026 1:12:35 AM Solves:2 MaxSolves:-1 RepeatTime:20h
//
// Turned in 1 more stack:
// societyribbonsperdaycounter:  "Limiter for amount of ribbons a player has turned in per day CompletedOn:5/23/2026 1:25:44 AM Solves:20 MaxSolves:200 RepeatTime:0s





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

        // The three Initiation quest flags grant society membership. Membership is
        // proven by the society rank character property (> 0), which is reliable
        // regardless of whether the member quest flag is present in /myquests.
        public static bool IsMembershipFlag(string flag)
        {
            return flag == "celestialhandmember" || flag == "eldrytchwebmember" || flag == "radiantbloodmember";
        }

        public static bool IsMember(string memberFlag)
        {
            switch (memberFlag)
            {
                case "celestialhandmember": return CoreManager.Current.CharacterFilter.GetCharProperty(PropCelhan) > 0;
                case "eldrytchwebmember": return CoreManager.Current.CharacterFilter.GetCharProperty(PropEldweb) > 0;
                case "radiantbloodmember": return CoreManager.Current.CharacterFilter.GetCharProperty(PropRadblo) > 0;
                default: return false;
            }
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

        // True at the floor of a rank — initiation (1) or just-promoted (101, 301, 601).
        // The +1 came from the promotion itself; no ribbons turned in this tier yet.
        public static bool IsFreshlyPromoted()
        {
            int value = GetRankValue();
            return value == 1 || value == 101 || value == 301 || value == 601;
        }

        // Rank value minus the +1 grant when IsFreshlyPromoted, so the displayed
        // number reflects ribbons turned in. Use GetRankValue for rank detection.
        public static int GetRankProgress()
        {
            int value = GetRankValue();
            return IsFreshlyPromoted() ? value - 1 : value;
        }

        public static int GetRibbonsToNextRank()
        {
            int threshold = GetNextRankThreshold();
            if (threshold == 0) return 0;
            return threshold - GetRankProgress();
        }


        // counter.Solves is frozen at the last turn-in batch; the server only
        // resets it on the *next* turn-in. So once the 20h timer is Ready, the
        // counter is stale and the player effectively has 0 ribbons turned in today.
        // Also force 0 at tier floors for visual consistency with GetRankProgress.
        public static int GetRibbonsToday()
        {
            if (IsFreshlyPromoted()) return 0;

            QuestFlag.QuestFlags.TryGetValue("societyribbonsperdaytimer", out QuestFlag timer);
            if (timer == null || timer.Ready()) return 0;

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
