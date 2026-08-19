using System;
using System.Collections.Generic;
using System.Linq;

namespace OracleOfDereth
{
    // The player's own shortlist of quest flags: picked on the Flags tab, shown on the Favorites
    // tab. Held as bare flag strings rather than Quest objects — the quest collection is rebuilt
    // on every login and rows come and go as quests.csv changes, so the flag is the only stable
    // handle to a row.
    //
    // Persisted in settings.xml, which is per-install rather than per-character: a shortlist of
    // quests worth doing is the player's, not one character's, and every character on the account
    // gets the same list. A favourited flag that this server's list doesn't carry stays in the
    // file and simply doesn't resolve to a row until you're back on a server that has it.
    public static class QuestFavorite
    {
        private const string SettingKey = "FavoriteQuestFlags";

        // A list, not a set: the player orders this by hand with the tab's arrows, so position is
        // data and the stored order IS the display order. Lazily loaded so this never races
        // SettingsFile.Init at startup. Flags are stored lower-cased, matching Quest.Flag, so a
        // favourite always compares equal to its row.
        private static List<string> flags;

        private static List<string> Flags()
        {
            if (flags != null) { return flags; }

            flags = new List<string>();

            try
            {
                foreach (string entry in SettingsFile.GetSetting(SettingKey, "").Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    string flag = entry.Trim().ToLower();
                    // Guard the duplicate here rather than trusting the file: two entries for one
                    // flag would render two rows that the arrows could never separate.
                    if (flag.Length > 0 && !flags.Contains(flag)) { flags.Add(flag); }
                }
            }
            catch (Exception ex) { Util.Log(ex); }

            return flags;
        }

        public static int Count => Flags().Count;

        public static bool Contains(string flag)
        {
            return flag != null && Flags().Contains(flag.Trim().ToLower());
        }

        // True when the flag wasn't already there, so the caller can say so rather than reporting
        // a save that didn't happen. New favourites land at the bottom — the newest pick is the
        // least likely to belong at the top of a list the player has already ordered.
        public static bool Add(string flag)
        {
            if (string.IsNullOrWhiteSpace(flag)) { return false; }

            string key = flag.Trim().ToLower();
            if (Flags().Contains(key)) { return false; }

            Flags().Add(key);
            Save();
            return true;
        }

        public static bool Remove(string flag)
        {
            if (string.IsNullOrWhiteSpace(flag)) { return false; }
            if (!Flags().Remove(flag.Trim().ToLower())) { return false; }

            Save();
            return true;
        }

        public static void Clear()
        {
            flags = new List<string>();
            Save();
        }

        // Swap with the neighbour above / below. False when the flag isn't a favourite or is
        // already at the end it's being moved toward, which the view reads as "nothing happened"
        // rather than an error.
        public static bool MoveUp(string flag) => Move(flag, -1);
        public static bool MoveDown(string flag) => Move(flag, +1);

        private static bool Move(string flag, int delta)
        {
            if (string.IsNullOrWhiteSpace(flag)) { return false; }

            List<string> current = Flags();
            int from = current.IndexOf(flag.Trim().ToLower());
            int to = from + delta;

            if (from < 0 || to < 0 || to >= current.Count) { return false; }

            string moved = current[from];
            current[from] = current[to];
            current[to] = moved;

            Save();
            return true;
        }

        private static void Save()
        {
            SettingsFile.PutSetting(SettingKey, string.Join(",", Flags().ToArray()));
        }

        // The favourites that this server's quest list actually carries, in the player's own
        // order. Deliberately NOT the Browse tab's sort: the arrows on the Favorites tab are the
        // only thing that orders this list, and a sort applied over there must not reshuffle it.
        //
        // One pass over QuestCatalog.Quests to collect the matches, then emitted in stored order — the
        // same cost as a Where() over that collection, rather than a scan per favourite. A stored
        // flag this server's list doesn't carry is skipped; it stays in the file.
        public static List<Quest> Quests()
        {
            List<string> favorites = Flags();
            if (favorites.Count == 0) { return new List<Quest>(); }

            var wanted = new HashSet<string>(favorites, StringComparer.OrdinalIgnoreCase);
            var found = new Dictionary<string, Quest>(StringComparer.OrdinalIgnoreCase);

            foreach (Quest quest in QuestCatalog.Quests)
            {
                if (wanted.Contains(quest.Flag) && !found.ContainsKey(quest.Flag)) { found[quest.Flag] = quest; }
            }

            var ordered = new List<Quest>(favorites.Count);
            foreach (string flag in favorites)
            {
                if (found.TryGetValue(flag, out Quest quest)) { ordered.Add(quest); }
            }

            return ordered;
        }
    }
}
