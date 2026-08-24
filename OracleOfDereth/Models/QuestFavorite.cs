using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace OracleOfDereth
{
    // The player's own shortlist of quest flags: picked on the Flags tab, shown on the Favorites
    // tab. Held as bare flag strings rather than Quest objects — the quest collection is rebuilt
    // on every login and rows come and go as quests.csv changes, so the flag is the only stable
    // handle to a row.
    //
    // Persisted as one ordered CSV per server. The row order is the custom
    // order shown by the # column.
    public static class QuestFavorite
    {
        private const string SettingKey = "FavoriteQuestFlags";

        // A list, not a set: the player orders this by hand with the tab's arrows, so position is
        // data and the stored order IS the display order. Lazily loaded so this never races
        // SettingsFile.Init at startup. Flags are stored lower-cased, matching Quest.Flag, so a
        // favourite always compares equal to its row.
        private static List<string> flags;
        private static string filePath;

        private static List<string> Flags()
        {
            if (flags != null) { return flags; }

            flags = new List<string>();
            filePath = QuestDataFile.ServerPath("-favorites");

            try
            {
                QuestDataFile.Recover(filePath);
                if (File.Exists(filePath))
                {
                    foreach (string line in File.ReadAllLines(filePath).Skip(1)) AddLoaded(Util.CsvParseLine(line).FirstOrDefault());
                }
                else
                {
                    foreach (string entry in SettingsFile.GetSetting(SettingKey, "").Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                        AddLoaded(entry);

                    if (flags.Count > 0 && Save()) SettingsFile.PutSetting(SettingKey, "");
                }
            }
            catch (Exception ex) { Util.Log(ex); }

            return flags;
        }

        public static int Count => Flags().Count;

        public static int Position(string flag)
        {
            if (string.IsNullOrWhiteSpace(flag)) return 0;
            return Flags().IndexOf(flag.Trim().ToLowerInvariant()) + 1;
        }

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
            Flags().Clear();
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

        private static bool Save()
        {
            try
            {
                QuestDataFile.Write(filePath, new[] { "Flag" }.Concat(Flags().Select(Util.CsvEscape)));
                return true;
            }
            catch (Exception ex)
            {
                Util.Log(ex);
                return false;
            }
        }

        private static void AddLoaded(string value)
        {
            string flag = (value ?? "").Trim().ToLowerInvariant();
            if (flag.Length > 0 && !flags.Contains(flag)) flags.Add(flag);
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
