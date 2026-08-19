using System;

namespace OracleOfDereth
{
    // One row in QuestCatalog. Metadata comes from quests.csv when known; runtime discoveries
    // are thinner IsNew rows until they are curated into that file.
    public class Quest
    {
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
        // Imported from the verification column for the current server.
        public bool Verified = false;

        // True for a flag discovered in /myquests or /myqstlist rather than loaded from quests.csv.
        public bool IsNew = false;

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

        // Verified is server-specific catalog metadata, never inferred from quest state.
        public bool IsVerified()
        {
            return !IsNew && Verified;
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

        // Only the Quests tab combines the current /myquests flags with durable quest history.
        public bool IsCompleteInQuestView()
        {
            return IsComplete() || QuestHistory.Contains(Flag);
        }

        // The Name column's text, and the single definition of it. Plenty of rows carry no
        // curated quest name — most Conquest-only flags, and anything whose questline the wiki
        // doesn't cover — but nearly all of them do carry the one-line Info describing what the
        // flag marks, which beats an empty cell. The tab, the sort, the search and both exports
        // come through here so they can't drift apart.
        public string DisplayName()
        {
            return Name.Length > 0 ? Name : Info;
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

        public string StatusInQuestView()
        {
            if (!IsComplete() && QuestHistory.Contains(Flag) && IsOneTime()) return "completed";
            return Status();
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

        // A third status pair based only on the catalog's verified metadata.
        public bool Verified = false;
        public bool Unverified = false;

        // A shortcut for the search everyone types by hand. Deliberately implemented as that
        // search rather than as its own rule, so the box and the checkbox can't disagree.
        public bool KillTask = false;
        private const string KillTaskTerms = "kill task";

        public bool OneTime = false;
        public bool Repeatable = false;

        // True when the filter actually narrows the list — some box ticked or text typed.
        public bool IsActive => New || Server || KillTask || Completed || Incomplete || Verified || Unverified || OneTime || Repeatable || !string.IsNullOrWhiteSpace(Text);

        public bool Matches(Quest quest)
        {
            if (New && !quest.IsNew) return false;
            if (Server && quest.Server.Length == 0) return false;
            if (KillTask && !MatchesTerms(quest, KillTaskTerms)) return false;
            if (!MatchesStatus(quest)) return false;
            if (!MatchesVerified(quest)) return false;
            if (!MatchesRepeat(quest)) return false;

            return MatchesText(quest);
        }

        // The checkboxes are a whitelist: with neither ticked (or both) there's no status
        // filtering at all, so the list stays whole until you actually pick a side.
        private bool MatchesStatus(Quest quest)
        {
            if (Completed == Incomplete) return true;

            return quest.IsCompleteInQuestView() ? Completed : Incomplete;
        }

        // Same whitelist behaviour again for verified / unverified.
        private bool MatchesVerified(Quest quest)
        {
            if (Verified == Unverified) return true;

            return Verified ? quest.IsVerified() : !quest.IsVerified();
        }

        // Same whitelist behaviour for the one-time / repeatable pair. Quest.IsRepeatable()
        // classifies every row — server data where the character holds the flag, the CSV's
        // master list everywhere else — so the two boxes partition the whole list.
        private bool MatchesRepeat(Quest quest)
        {
            if (OneTime == Repeatable) return true;

            return Repeatable ? quest.IsRepeatable() : quest.IsOneTime();
        }

        private bool MatchesText(Quest quest)
        {
            return MatchesTerms(quest, Text);
        }

        // Space-separated terms, all of which must appear in the quest's flag or name — the two
        // columns actually on screen. Info and hint are deliberately not searched: they're long
        // prose, and matching them turns up rows whose visible text has nothing to do with the term.
        //
        // Splitting on spaces is what lets "kill task" find both spellings: the flag runs the words
        // together as killtaskgurog while the name spells them out as Gurog Kill Task, and each term
        // only has to appear somewhere in the pair.
        private static bool MatchesTerms(Quest quest, string query)
        {
            string trimmed = (query ?? "").Trim();
            if (trimmed.Length == 0) return true;

            string combined = $"{quest.Flag} {quest.DisplayName()}";

            foreach (string term in trimmed.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (combined.IndexOf(term, StringComparison.OrdinalIgnoreCase) < 0) return false;
            }

            return true;
        }
    }
}
