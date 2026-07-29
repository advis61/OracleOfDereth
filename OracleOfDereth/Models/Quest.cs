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
        // Collection of Quests loaded from quests.csv (current server only), plus any flag the
        // server reported that the CSV didn't know about — see MergeQuestFlags().
        public static List<Quest> Quests = new List<Quest>();

        // Every flag in Quests, for O(1) "do we already have this one?" during the merge.
        private static readonly HashSet<string> KnownFlags = new HashSet<string>();

        // quests.csv is already flag-ascending, so that's the order the tab opens in — nothing
        // is sorted until a header is clicked.
        public static SortType CurrentSortType = SortType.FlagAscending;

        public enum SortType
        {
            CompleteAscending,
            CompleteDescending,
            FlagAscending,
            FlagDescending,
            NameAscending,
            NameDescending,
            ReadyAscending,
            ReadyDescending,
            SolvesAscending,
            SolvesDescending,
        }

        // Properties
        public string Flag = "";
        public string Server = "";
        public string Name = "";
        private string _url = "";
        public string Url { get => Util.WikiUrl(_url); set => _url = value; }
        public string Info = "";
        public string Hint = "";
        // From the CSV's Repeatable column: maxSolves of -1 or >1, or a non-zero repeat timer.
        // Unlike anything derived from /myquests this is known for every quest, earned or not,
        // which is what lets the One Time / Repeatable filters cover the whole list.
        public bool Repeatable = false;

        // True for a flag discovered in /myquests rather than loaded from quests.csv. The game
        // grows new quest flags over time, so the curated list is always a little behind.
        public bool IsNew = false;

        public static void Init()
        {
            Quests.Clear();
            KnownFlags.Clear();
            LoadQuestsCSV();
        }

        // Fold anything the server reported but quests.csv doesn't list into the collection,
        // tagged IsNew. QuestFlag is the source of truth for what a character actually has;
        // this is the only way a brand-new flag ever shows up on the Flags tab.
        //
        // A discovered flag has no url, info or hint — but /myquests does carry the game's own
        // description for it, which is the closest thing to a name we get for free, so that
        // becomes Name. It can still be empty: the description is an optional part of the line.
        // New rows are appended rather than sorted in, which keeps them together at the bottom
        // of an unfiltered list.
        public static void MergeQuestFlags()
        {
            foreach (KeyValuePair<string, QuestFlag> pair in QuestFlag.QuestFlags)
            {
                if (KnownFlags.Contains(pair.Key)) continue;

                Quests.Add(new Quest
                {
                    Flag = pair.Key,
                    Name = CleanDescription(pair.Value.Description),
                    // No CSV row to read Repeatable from, but the server just told us: a flag
                    // carrying a repeat timer is repeatable.
                    Repeatable = pair.Value.RepeatTime != TimeSpan.Zero,
                    IsNew = true
                });
                KnownFlags.Add(pair.Key);
            }
        }

        // The description in a /myquests line is wrapped in double quotes, and for a flag whose
        // description is itself empty or quoted the quote characters survive the parse — uberbellas2
        // arrives as a bare ". They're never part of the real name, so drop both quote forms; a
        // description that was nothing but quotes cleans down to empty, same as no description.
        private static string CleanDescription(string description)
        {
            if (string.IsNullOrEmpty(description)) return "";

            return new string(description.Where(c => c != '"' && c != '\'').ToArray()).Trim();
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

                // Assume columns: QuestFlag,Server,Quest,Url,Info,Hint,Repeatable
                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine();
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    var fields = line.Split(',');
                    if (fields.Length < 7) continue;

                    var quest = new Quest
                    {
                        Flag = fields[0].Trim().ToLower(),
                        Server = fields[1].Trim(),
                        Name = fields[2].Trim(),
                        Url = fields[3].Trim(),
                        Info = fields[4].Trim(),
                        Hint = fields[5].Trim(),
                        // TRUE / FALSE in the file; anything that isn't "true" reads as one-time.
                        Repeatable = string.Equals(fields[6].Trim(), "true", StringComparison.OrdinalIgnoreCase)
                    };

                    // A blank Server means every world; otherwise keep only this character's. The
                    // static Server helper is qualified because the instance property shadows it here.
                    if (quest.Server.Length > 0 &&
                        !string.Equals(quest.Server, OracleOfDereth.Server.Name, StringComparison.OrdinalIgnoreCase)) continue;

                    quests.Add(quest);
                }
            }

            Quests.AddRange(quests);

            foreach (Quest quest in quests) { KnownFlags.Add(quest.Flag); }
        }

        // Reorders the collection in place, the same way JohnQuest and Title do. Every sort falls
        // back to Flag: it's the only column guaranteed unique and non-empty, so rows that tie on
        // the clicked column keep a stable, predictable order instead of shuffling between clicks.
        public static void Sort(SortType sortType)
        {
            CurrentSortType = sortType;
            switch (sortType)
            {
                case SortType.CompleteAscending:
                    Quests = Quests.OrderBy(q => q.IsComplete()).ThenBy(q => q.Flag).ToList();
                    break;
                case SortType.CompleteDescending:
                    Quests = Quests.OrderByDescending(q => q.IsComplete()).ThenBy(q => q.Flag).ToList();
                    break;
                case SortType.FlagAscending:
                    Quests = Quests.OrderBy(q => q.Flag).ToList();
                    break;
                case SortType.FlagDescending:
                    Quests = Quests.OrderByDescending(q => q.Flag).ToList();
                    break;
                case SortType.NameAscending:
                    Quests = Quests.OrderBy(q => q.Name).ThenBy(q => q.Flag).ToList();
                    break;
                case SortType.NameDescending:
                    Quests = Quests.OrderByDescending(q => q.Name).ThenBy(q => q.Flag).ToList();
                    break;
                case SortType.ReadyAscending:
                    Quests = Quests.OrderBy(q => q.Ready()).ThenBy(q => q.NextAvailableTime()).ThenBy(q => q.Flag).ToList();
                    break;
                case SortType.ReadyDescending:
                    Quests = Quests.OrderByDescending(q => q.NextAvailableTime()).ThenBy(q => q.Flag).ToList();
                    break;
                case SortType.SolvesAscending:
                    Quests = Quests.OrderBy(q => q.Solves()).ThenBy(q => q.Flag).ToList();
                    break;
                case SortType.SolvesDescending:
                    Quests = Quests.OrderByDescending(q => q.Solves()).ThenBy(q => q.Flag).ToList();
                    break;
            }
        }

        public override string ToString()
        {
            return $"{Flag}: {Name}";
        }

        // Is this quest repeatable? The server is the source of truth wherever it has spoken: if
        // the character holds the flag, its RepeatTime is what actually governs the cooldown, and
        // it beats whatever the CSV claims. For everything never earned there's no server data,
        // so the master list's Repeatable column stands in — and anything not marked repeatable
        // is treated as one-time. Between them every quest is classified, which is what lets the
        // two filter boxes partition the whole list without an Unknown bucket.
        public bool IsRepeatable()
        {
            QuestFlag.QuestFlags.TryGetValue(Flag, out QuestFlag questFlag);
            if (questFlag != null) { return questFlag.RepeatTime != TimeSpan.Zero; }

            return Repeatable;
        }

        public bool IsOneTime()
        {
            return !IsRepeatable();
        }

        // Having the flag at all is the whole test. /myquests only reports flags the character
        // has actually earned, so its presence is the fact; the solve count is not consulted
        // because custom and server-specific quests keep odd counts (zero solves on a stamp
        // that's been spent, counters that reset) that say nothing about whether it was done.
        //
        // Note this is the opposite of CustomQuest / SocietyQuest, which treat a repeatable
        // that's off cooldown as not-complete. Those tabs are worklists; this one is a record
        // of what a character has done, and the Ready column carries the cooldown state.
        public bool IsComplete()
        {
            return QuestFlag.QuestFlags.ContainsKey(Flag);
        }

        // The Ready column's text, and the single definition of it: a flag never earned reads
        // "ready", a one-time stamp reads "completed", and a repeatable reads its countdown —
        // or "ready" again once that has elapsed. The tab, the clipboard and both exports all
        // come through here so they can't drift apart.
        public string Status()
        {
            QuestFlag.QuestFlags.TryGetValue(Flag, out QuestFlag questFlag);
            if (questFlag == null) { return "ready"; }
            if (IsOneTime()) { return "completed"; }

            return questFlag.NextAvailable();
        }

        // The Solves column. Blank unless the quest is repeatable: a one-time stamp sits at 1
        // (or 0 once spent), which says nothing the completed icon hasn't already said.
        public string SolvesText()
        {
            QuestFlag.QuestFlags.TryGetValue(Flag, out QuestFlag questFlag);
            if (questFlag == null || IsOneTime()) { return ""; }

            return questFlag.Solves.ToString();
        }

        // Sort keys. The display columns are strings ("2h 15m" vs "ready"), so the Ready and
        // Solves sorts run off the underlying values instead — same as JohnQuest.
        public TimeSpan? NextAvailableTime()
        {
            QuestFlag.QuestFlags.TryGetValue(Flag, out QuestFlag questFlag);
            if (questFlag == null) { return null; }

            return questFlag.NextAvailableTime();
        }

        public bool Ready()
        {
            QuestFlag.QuestFlags.TryGetValue(Flag, out QuestFlag questFlag);
            if (questFlag == null) { return true; }

            return questFlag.Ready();
        }

        // The numeric twin of SolvesText, and it has to apply the same rule: zero wherever the
        // column renders blank. Sorting on the raw count instead scatters one-time quests —
        // whose cell is empty — up among the repeatables that actually show a number.
        public int Solves()
        {
            QuestFlag.QuestFlags.TryGetValue(Flag, out QuestFlag questFlag);
            if (questFlag == null || IsOneTime()) { return 0; }

            return questFlag.Solves;
        }

        // The Info blurb and the coordinate walkthrough, as one chat line. Either can be empty,
        // and for the rows with no walkthrough to give — counters, pickups, most Conquest-only
        // flags — the two columns hold the same sentence, so print it once rather than twice.
        public string Details()
        {
            if (Info.Length == 0) { return Hint; }
            if (Hint.Length == 0) { return Info; }
            if (string.Equals(Info, Hint, StringComparison.OrdinalIgnoreCase)) { return Info; }

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

        // Not a status — an extra AND condition: only flags the server reported that aren't in
        // quests.csv. Mirrors how ItemFilter.Doubles narrows on top of the category boxes.
        public bool New = false;

        // Another extra AND condition: only rows quests.csv tied to a named world. LoadQuestsCSV
        // has already dropped every other world's rows, so what's left is the content specific to
        // the one you're logged into, as opposed to the everywhere-rows with a blank Server.
        public bool Server = false;

        public bool OneTime = false;
        public bool Repeatable = false;

        // True when the filter actually narrows the list — some box ticked or text typed.
        public bool IsActive => New || Server || Completed || Incomplete || OneTime || Repeatable || !string.IsNullOrWhiteSpace(Text);

        public bool Matches(Quest quest)
        {
            if (New && !quest.IsNew) return false;
            if (Server && quest.Server.Length == 0) return false;
            if (!MatchesStatus(quest)) return false;
            if (!MatchesRepeat(quest)) return false;

            return MatchesText(quest);
        }

        // The checkboxes are a whitelist: with neither ticked (or both) there's no status
        // filtering at all, so the list stays whole until you actually pick a side.
        private bool MatchesStatus(Quest quest)
        {
            if (Completed == Incomplete) return true;

            return quest.IsComplete() ? Completed : Incomplete;
        }

        // Same whitelist behaviour for the one-time / repeatable pair. Quest.IsRepeatable()
        // classifies every row — server data where the character holds the flag, the CSV's
        // master list everywhere else — so the two boxes partition the whole list.
        private bool MatchesRepeat(Quest quest)
        {
            if (OneTime == Repeatable) return true;

            return Repeatable ? quest.IsRepeatable() : quest.IsOneTime();
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
