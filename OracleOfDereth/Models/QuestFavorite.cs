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

        // Lazily loaded so this never races SettingsFile.Init at startup. Flags are stored
        // lower-cased, matching Quest.Flag, so a favourite always compares equal to its row.
        private static HashSet<string> flags;

        private static HashSet<string> Flags()
        {
            if (flags != null) { return flags; }

            flags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            try
            {
                foreach (string entry in SettingsFile.GetSetting(SettingKey, "").Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    string flag = entry.Trim().ToLower();
                    if (flag.Length > 0) { flags.Add(flag); }
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
        // a save that didn't happen.
        public static bool Add(string flag)
        {
            if (string.IsNullOrWhiteSpace(flag)) { return false; }
            if (!Flags().Add(flag.Trim().ToLower())) { return false; }

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
            flags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            Save();
        }

        private static void Save()
        {
            SettingsFile.PutSetting(SettingKey, string.Join(",", Flags().ToArray()));
        }

        // The favourites that this server's quest list actually carries, in whatever order the
        // Flags tab is currently sorted — Quest.Quests is the one collection both tabs read, so
        // sorting there is reflected here without the Favorites tab tracking its own sort.
        public static List<Quest> Quests()
        {
            HashSet<string> favorites = Flags();
            if (favorites.Count == 0) { return new List<Quest>(); }

            return Quest.Quests.Where(q => favorites.Contains(q.Flag)).ToList();
        }
    }
}
