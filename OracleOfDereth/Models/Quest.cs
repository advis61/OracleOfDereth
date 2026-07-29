using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace OracleOfDereth
{
    // Every known quest flag, loaded from quests.csv — the raw database behind the "Flags" tab,
    // as opposed to the curated lists (FlagQuest, JohnQuest, FacilityQuest, ...) that each cover
    // one questline. There's no bitmask here and most rows have no curated name, so completion is
    // simply "the character has this flag at all", the same test FlagQuest uses. Ready/Solves come
    // from the tracked QuestFlag, the same way JohnQuest reads them.
    //
    // Rows naming a Server are kept only on that world; a blank Server means the quest exists
    // everywhere. That differs from CustomQuest, which drops every row that isn't an exact match.
    public class Quest
    {
        // Collection of Quests loaded from quests.csv (current server only)
        public static List<Quest> Quests = new List<Quest>();

        // Properties
        public string Flag = "";
        public string Server = "";
        public string Name = "";
        private string _url = "";
        public string Url { get => Util.WikiUrl(_url); set => _url = value; }
        public string Info = "";
        public string Hint = "";

        public static void Init()
        {
            Quests.Clear();
            LoadQuestsCSV();
        }

        public static void LoadQuestsCSV()
        {
            var quests = new List<Quest>();

            var assembly = Assembly.GetExecutingAssembly();

            // Match the full resource path, not just "quests.csv" — johnquests.csv, customquests.csv,
            // augquests.csv, creditquests.csv, facilityquests.csv and flagquests.csv all end with it.
            string resourceName = assembly.GetManifestResourceNames().FirstOrDefault(n => n.EndsWith(".Resources.quests.csv", StringComparison.OrdinalIgnoreCase));
            if (resourceName == null) throw new FileNotFoundException("Embedded resource quests.csv not found.");

            using (var stream = assembly.GetManifestResourceStream(resourceName))
            using (var reader = new StreamReader(stream))
            {
                string headerLine = reader.ReadLine();
                if (headerLine == null) throw new InvalidDataException("CSV file is empty.");

                // Assume columns: QuestFlag,Server,Quest,Url,Info,Hint
                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine();
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    var fields = line.Split(',');
                    if (fields.Length < 6) continue;

                    var quest = new Quest
                    {
                        Flag = fields[0].Trim().ToLower(),
                        Server = fields[1].Trim(),
                        Name = fields[2].Trim(),
                        Url = fields[3].Trim(),
                        Info = fields[4].Trim(),
                        Hint = fields[5].Trim()
                    };

                    // A blank Server means every world; otherwise keep only this character's. The
                    // static Server helper is qualified because the instance property shadows it here.
                    if (quest.Server.Length > 0 &&
                        !string.Equals(quest.Server, OracleOfDereth.Server.Name, StringComparison.OrdinalIgnoreCase)) continue;

                    quests.Add(quest);
                }
            }

            Quests.AddRange(quests);
        }

        public override string ToString()
        {
            return $"{Flag}: {Name}";
        }

        // A one-time quest is a permanent stamp with no repeat timer; a repeatable one carries a
        // RepeatTime cooldown in /myquests. A flag the character has never earned is unknowable
        // either way — /myquests only reports what you've done — so it counts as repeatable.
        public bool IsOneTime()
        {
            QuestFlag.QuestFlags.TryGetValue(Flag, out QuestFlag questFlag);
            if (questFlag == null) { return false; }

            return questFlag.RepeatTime == TimeSpan.Zero;
        }

        public bool IsComplete()
        {
            QuestFlag.QuestFlags.TryGetValue(Flag, out QuestFlag questFlag);
            if (questFlag == null) { return false; }

            // One-time quests are complete once the stamp is present. Repeatable quests count as
            // complete only while on cooldown — once the repeat timer elapses they're available
            // again, so they show as not-completed. Mirrors CustomQuest / SocietyQuest.
            if (questFlag.RepeatTime == TimeSpan.Zero) { return questFlag.Solves > 0; }

            return !questFlag.Ready();
        }

        // The Info blurb and the coordinate walkthrough, as one chat line. Either can be empty.
        public string Details()
        {
            if (Info.Length == 0) { return Hint; }
            if (Hint.Length == 0) { return Info; }

            return $"{Info} {Hint}";
        }
    }

    // Filter state for the Flags tab: the search box plus the two status checkboxes. Mirrors
    // ItemFilter on the Items tab, which drives the same style of filter bar.
    public class QuestFilter
    {
        public string Text = "";
        public bool Completed = false;
        public bool Incomplete = false;

        public bool Matches(Quest quest)
        {
            if (!MatchesStatus(quest)) return false;

            return MatchesText(quest);
        }

        // The checkboxes are a whitelist: with neither ticked (or both) there's no status
        // filtering at all, so the list stays whole until you actually pick a side.
        private bool MatchesStatus(Quest quest)
        {
            if (Completed == Incomplete) return true;

            return quest.IsComplete() ? Completed : Incomplete;
        }

        // Space-separated terms, all of which must appear in the quest's flag or name — the two
        // columns actually on screen. Info and hint are deliberately not searched: they're long
        // prose, and matching them turns up rows whose visible text has nothing to do with the term.
        private bool MatchesText(Quest quest)
        {
            string trimmed = (Text ?? "").Trim();
            if (trimmed.Length == 0) return true;

            string combined = $"{quest.Flag} {quest.Name}";

            foreach (string term in trimmed.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (combined.IndexOf(term, StringComparison.OrdinalIgnoreCase) < 0) return false;
            }

            return true;
        }
    }
}
