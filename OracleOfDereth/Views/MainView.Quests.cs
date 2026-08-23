using Decal.Adapter;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using VirindiViewService.Controls;

namespace OracleOfDereth
{
    partial class MainView
    {
        // Set while Reset clears the filter controls, so each control's Change event doesn't
        // trigger its own repaint — we do one at the end instead.
        private bool suppressQuestsFilter = false;

        // The text columns AssignSelected tints for a newly-discovered flag. Hoisted out of the
        // render loop so it isn't reallocated once per row across thousands of rows.
        private static readonly List<int> QuestsRowColumns = new List<int> { 1, 2, 3, 4 };

        // How each row is currently tinted, parallel to the list's rows. Tinting writes four colour
        // properties per row, so firing it on every row of every repaint is the most expensive
        // thing on this tab — this lets it fire only when a row's tint actually flips. A row can
        // qualify for both tints at once, hence a code rather than a bool.
        private const int TintNone = 0;
        private const int TintNew = 1;
        private const int TintRowSelected = 2;

        private readonly List<int> questsRowTinted = new List<int>();

        // The flag of the clicked row, or "" for none. Tracked by flag rather than row index
        // because sorting and filtering move rows out from under an index between repaints.
        private string questsSelectedFlag = "";

        public HudStaticText QuestsText { get; private set; }
        public HudButton QuestsRefresh { get; private set; }
        public HudButton QuestsRefreshAll { get; private set; }
        public HudButton QuestsSend { get; private set; }
        public HudButton QuestsClipboard { get; private set; }
        public HudButton QuestsHelp { get; private set; }
        public HudTextBox QuestsFilterText { get; private set; }
        public HudButton QuestsFilterReset { get; private set; }
        public HudButton QuestsFavorite { get; private set; }
        public HudCheckBox QuestsFilterCompleted { get; private set; }
        public HudCheckBox QuestsFilterIncomplete { get; private set; }
        public HudCheckBox QuestsFilterVerified { get; private set; }
        public HudCheckBox QuestsFilterUnverified { get; private set; }
        public HudCheckBox QuestsFilterOneTime { get; private set; }
        public HudCheckBox QuestsFilterRepeatable { get; private set; }
        public HudCheckBox QuestsFilterServer { get; private set; }
        public HudCheckBox QuestsFilterKillTask { get; private set; }
        public HudCheckBox QuestsFilterNew { get; private set; }

        public HudFixedLayout QuestsListSortComplete { get; private set; }
        public HudPictureBox QuestsListSortCompleteIcon { get; private set; }

        public HudStaticText QuestsListSortFlag { get; private set; }
        public HudStaticText QuestsListSortName { get; private set; }
        public HudStaticText QuestsListSortReady { get; private set; }
        public HudStaticText QuestsListSortSolves { get; private set; }

        public HudList QuestsList { get; private set; }

        private void InitQuests()
        {
            QuestsText = (HudStaticText)view["QuestsText"];
            QuestsText.FontHeight = 10;

            QuestsRefresh = (HudButton)view["QuestsRefresh"];
            QuestsRefresh.Hit += QuestFlagsRefresh_Hit;

            QuestsRefreshAll = (HudButton)view["QuestsRefreshAll"];
            QuestsRefreshAll.Hit += QuestsRefreshAll_Hit;

            QuestsSend = (HudButton)view["QuestsSend"];
            QuestsSend.Hit += QuestsSend_Hit;

            QuestsClipboard = (HudButton)view["QuestsClipboard"];
            QuestsClipboard.Hit += QuestsClipboard_Hit;

            QuestsHelp = (HudButton)view["QuestsHelp"];
            QuestsHelp.Hit += QuestsHelp_Hit;

            QuestsFilterText = (HudTextBox)view["QuestsFilterText"];
            QuestsFilterText.Change += QuestsFilter_Change;

            QuestsFilterReset = (HudButton)view["QuestsFilterReset"];
            QuestsFilterReset.Hit += QuestsFilterReset_Hit;

            QuestsFavorite = (HudButton)view["QuestsFavorite"];
            QuestsFavorite.Hit += QuestsFavorite_Hit;

            QuestsFilterCompleted = (HudCheckBox)view["QuestsFilterCompleted"];
            QuestsFilterCompleted.Change += QuestsFilter_Change;

            QuestsFilterIncomplete = (HudCheckBox)view["QuestsFilterIncomplete"];
            QuestsFilterIncomplete.Change += QuestsFilter_Change;

            QuestsFilterVerified = (HudCheckBox)view["QuestsFilterVerified"];
            QuestsFilterVerified.Change += QuestsFilter_Change;

            QuestsFilterUnverified = (HudCheckBox)view["QuestsFilterUnverified"];
            QuestsFilterUnverified.Change += QuestsFilter_Change;

            QuestsFilterOneTime = (HudCheckBox)view["QuestsFilterOneTime"];
            QuestsFilterOneTime.Change += QuestsFilter_Change;

            QuestsFilterRepeatable = (HudCheckBox)view["QuestsFilterRepeatable"];
            QuestsFilterRepeatable.Change += QuestsFilter_Change;

            QuestsFilterServer = (HudCheckBox)view["QuestsFilterServer"];
            QuestsFilterServer.Change += QuestsFilter_Change;

            QuestsFilterKillTask = (HudCheckBox)view["QuestsFilterKillTask"];
            QuestsFilterKillTask.Change += QuestsFilter_Change;

            QuestsFilterNew = (HudCheckBox)view["QuestsFilterNew"];
            QuestsFilterNew.Change += QuestsFilter_Change;

            QuestsList = (HudList)view["QuestsList"];
            QuestsList.Click += QuestsList_Click;
            QuestsList.ClearRows();

            QuestsListSortCompleteIcon = new HudPictureBox();
            QuestsListSortCompleteIcon.Image = IconSort;
            QuestsListSortComplete = (HudFixedLayout)view["QuestsListSortComplete"];
            QuestsListSortComplete.AddControl(QuestsListSortCompleteIcon, new Rectangle(0, 0, 16, 16));
            QuestsListSortCompleteIcon.Hit += QuestsListSortComplete_Click;

            QuestsListSortFlag = (HudStaticText)view["QuestsListSortFlag"];
            QuestsListSortFlag.Hit += QuestsListSortFlag_Click;

            QuestsListSortName = (HudStaticText)view["QuestsListSortName"];
            QuestsListSortName.Hit += QuestsListSortName_Click;

            QuestsListSortReady = (HudStaticText)view["QuestsListSortReady"];
            QuestsListSortReady.Hit += QuestsListSortReady_Click;

            QuestsListSortSolves = (HudStaticText)view["QuestsListSortSolves"];
            QuestsListSortSolves.Hit += QuestsListSortSolves_Click;
        }

        private void DisposeQuests()
        {
            QuestsList.Click -= QuestsList_Click;
            QuestsListSortCompleteIcon.Hit -= QuestsListSortComplete_Click;
            QuestsListSortFlag.Hit -= QuestsListSortFlag_Click;
            QuestsListSortName.Hit -= QuestsListSortName_Click;
            QuestsListSortReady.Hit -= QuestsListSortReady_Click;
            QuestsListSortSolves.Hit -= QuestsListSortSolves_Click;
            QuestsFilterText.Change -= QuestsFilter_Change;
            QuestsFilterReset.Hit -= QuestsFilterReset_Hit;
            QuestsFavorite.Hit -= QuestsFavorite_Hit;
            QuestsFilterCompleted.Change -= QuestsFilter_Change;
            QuestsFilterIncomplete.Change -= QuestsFilter_Change;
            QuestsFilterVerified.Change -= QuestsFilter_Change;
            QuestsFilterUnverified.Change -= QuestsFilter_Change;
            QuestsFilterOneTime.Change -= QuestsFilter_Change;
            QuestsFilterRepeatable.Change -= QuestsFilter_Change;
            QuestsFilterServer.Change -= QuestsFilter_Change;
            QuestsFilterKillTask.Change -= QuestsFilter_Change;
            QuestsFilterNew.Change -= QuestsFilter_Change;
            QuestsClipboard.Hit -= QuestsClipboard_Hit;
            QuestsHelp.Hit -= QuestsHelp_Hit;
            QuestsRefresh.Hit -= QuestFlagsRefresh_Hit;
            QuestsRefreshAll.Hit -= QuestsRefreshAll_Hit;
            QuestsSend.Hit -= QuestsSend_Hit;
        }

        public void UpdateQuests()
        {
            if (!QuestState.HasRequestedRefresh) { QuestFlag.Refresh(); }
            if (QuestState.RefreshStatus == QuestRefreshStatus.Loaded && QuestHistory.RefreshIfMissing())
            {
                FlashButton(QuestsRefreshAll);
            }

            // Repaint every tick like the other tabs, so the Ready column's countdowns actually
            // count down — SetText makes that cheap by writing only the cells that changed. The
            // one thing not worth doing is painting thousands of rows into a closed window,
            // which the other tabs are small enough not to bother guarding against.
            if (!view.Visible) { return; }

            UpdateQuestsList();
        }

        // Build the filter from the tab's search box + checkboxes.
        private QuestFilter QuestsFilter()
        {
            return new QuestFilter
            {
                Text = QuestsFilterText?.Text ?? "",
                Completed = QuestsFilterCompleted.Checked,
                Incomplete = QuestsFilterIncomplete.Checked,
                Verified = QuestsFilterVerified.Checked,
                Unverified = QuestsFilterUnverified.Checked,
                OneTime = QuestsFilterOneTime.Checked,
                Repeatable = QuestsFilterRepeatable.Checked,
                Server = QuestsFilterServer.Checked,
                KillTask = QuestsFilterKillTask.Checked,
                New = QuestsFilterNew.Checked,
            };
        }

        private void UpdateQuestsList()
        {
            UpdateQuestSendButton();

            QuestFilter filter = QuestsFilter();

            // With nothing filtering, use the collection as-is. The Where(...).ToList() otherwise
            // allocates a 4,000-entry list and runs a delegate per row on every single repaint,
            // only to reproduce the list we already have. Read-only either way.
            List<Quest> quests = filter.IsActive ? QuestCatalog.Quests.Where(filter.Matches).ToList() : QuestCatalog.Quests;
            QuestsFavorite.Visible = questsSelectedFlag.Length > 0 &&
                quests.Any(q => q.Flag == questsSelectedFlag);
            int completed = 0;

            for (int x = 0; x < quests.Count; x++)
            {
                HudList.HudListRowAccessor row;

                if (x >= QuestsList.RowCount) {
                    row = QuestsList.AddRow();

                    ((HudStaticText)row[3]).TextAlignment = VirindiViewService.WriteTextFormats.Right;
                    ((HudStaticText)row[4]).TextAlignment = VirindiViewService.WriteTextFormats.Right;
                } else {
                    row = QuestsList[x];
                }

                while (questsRowTinted.Count <= x) { questsRowTinted.Add(TintNone); }

                // Update
                Quest quest = quests[x];

                bool complete = quest.IsCompleteInQuestView();
                if (complete) { completed += 1; }

                AssignImage((HudPictureBox)row[0], complete);
                SetText(row, 1, quest.Flag);
                SetText(row, 2, quest.DisplayName());
                SetText(row, 3, quest.StatusInQuestView());
                SetText(row, 4, quest.SolvesText());

                // Flags the server reported that quests.csv doesn't list are tinted rather than
                // tagged in the Name column — their name is the game's own description, which
                // already fills the column, and a "(new)" prefix would just crowd it out. The
                // clicked row outranks that: you need to see what you're about to favourite, and
                // it's one row against however many are new.
                int tint = quest.Flag == questsSelectedFlag ? TintRowSelected
                         : quest.IsNew ? TintNew
                         : TintNone;

                if (questsRowTinted[x] != tint)
                {
                    AssignTint(row, TintColor(tint), QuestsRowColumns);
                    questsRowTinted[x] = tint;
                }
            }

            // Trim surplus rows the filter has hidden. Nothing to clean up alongside them:
            // AssignImage keeps its state on the box, so a destroyed row takes it with it.
            while (QuestsList.RowCount > quests.Count)
            {
                QuestsList.RemoveRow(QuestsList.RowCount - 1);
            }

            // Keep the tint tracking the same length as the rows it describes.
            if (questsRowTinted.Count > quests.Count)
            {
                questsRowTinted.RemoveRange(quests.Count, questsRowTinted.Count - quests.Count);
            }

            // Update Text. A completed-of-total tally only means something against the whole
            // list, so once a filter is narrowing things it gives way to a plain match count.
            if (filter.IsActive) {
                QuestsText.Text = $"Quest Flags: {quests.Count} flags";
            } else {
                QuestsText.Text = $"Quest Flags: {completed} of {quests.Count} completed";
            }
        }

        public int UpdateQuestSendButton()
        {
            int pending = QuestSubmit.PendingCount();
            QuestsSend.Visible = pending > 0;
            return pending;
        }

        public void ClearSentQuestFlags()
        {
            int cleared = QuestSubmit.SentCount;
            QuestSubmit.ClearSent();
            int pending = UpdateQuestSendButton();
            Util.Chat($"Cleared {cleared} sent quest flags. {pending} ready to send.");
        }

        private Color? TintColor(int tint)
        {
            if (tint == TintRowSelected) { return ColorRowSelected; }
            if (tint == TintNew) { return ColorSelected; }

            return null;
        }

        private void QuestsFilter_Change(object sender, EventArgs e)
        {
            if (suppressQuestsFilter) return;

            UpdateQuestsList();
        }

        // Clear the search box and both checkboxes, then repaint once.
        private void QuestsFilterReset_Hit(object sender, EventArgs e)
        {
            suppressQuestsFilter = true;
            QuestsFilterText.Text = "";
            QuestsFilterCompleted.Checked = false;
            QuestsFilterIncomplete.Checked = false;
            QuestsFilterVerified.Checked = false;
            QuestsFilterUnverified.Checked = false;
            QuestsFilterOneTime.Checked = false;
            QuestsFilterRepeatable.Checked = false;
            QuestsFilterServer.Checked = false;
            QuestsFilterKillTask.Checked = false;
            QuestsFilterNew.Checked = false;
            suppressQuestsFilter = false;

            // The picked row is part of what Reset is clearing — leaving it selected would strand
            // an Add button pointing at a row that's scrolled off somewhere in the full list.
            questsSelectedFlag = "";

            UpdateQuestsList();
        }

        // Adds the picked row to the Favorites tab and clears the selection, so the button goes
        // away rather than inviting the same click twice.
        private void QuestsFavorite_Hit(object sender, EventArgs e)
        {
            string flag = questsSelectedFlag;
            if (flag.Length == 0) { return; }

            Quest quest = QuestCatalog.Quests.FirstOrDefault(q => q.Flag == flag);
            string name = quest != null ? quest.DisplayName() : "";
            string label = name.Length > 0 ? $"{flag} - {name}" : flag;

            if (QuestFavorite.Add(flag))
            {
                Util.Chat($"Added to Favorites: {label}", Util.ColorPink);
            }
            else
            {
                Util.Chat($"Already in Favorites: {label}", Util.ColorPink);
            }

            questsSelectedFlag = "";
            UpdateQuestsList();
        }

        // Each header toggles its own column between ascending and descending, same as the John
        // and Titles tabs. Quest.Sort reorders the collection, so the repaint picks it up.
        void QuestsListSortComplete_Click(object sender, EventArgs e)
        {
            if (QuestCatalog.CurrentSortType == Quest.SortType.CompleteAscending) {
                QuestCatalog.Sort(Quest.SortType.CompleteDescending);
            } else {
                QuestCatalog.Sort(Quest.SortType.CompleteAscending);
            }

            UpdateQuestsList();
        }

        void QuestsListSortFlag_Click(object sender, EventArgs e)
        {
            if (QuestCatalog.CurrentSortType == Quest.SortType.FlagAscending) {
                QuestCatalog.Sort(Quest.SortType.FlagDescending);
            } else {
                QuestCatalog.Sort(Quest.SortType.FlagAscending);
            }

            UpdateQuestsList();
        }

        void QuestsListSortName_Click(object sender, EventArgs e)
        {
            if (QuestCatalog.CurrentSortType == Quest.SortType.NameAscending) {
                QuestCatalog.Sort(Quest.SortType.NameDescending);
            } else {
                QuestCatalog.Sort(Quest.SortType.NameAscending);
            }

            UpdateQuestsList();
        }

        void QuestsListSortReady_Click(object sender, EventArgs e)
        {
            if (QuestCatalog.CurrentSortType == Quest.SortType.ReadyAscending) {
                QuestCatalog.Sort(Quest.SortType.ReadyDescending);
            } else {
                QuestCatalog.Sort(Quest.SortType.ReadyAscending);
            }

            UpdateQuestsList();
        }

        // Solves opens descending, unlike the other columns: the vast majority of rows have no
        // solve count at all, so ascending would just show thousands of blanks.
        void QuestsListSortSolves_Click(object sender, EventArgs e)
        {
            if (QuestCatalog.CurrentSortType == Quest.SortType.SolvesDescending) {
                QuestCatalog.Sort(Quest.SortType.SolvesAscending);
            } else {
                QuestCatalog.Sort(Quest.SortType.SolvesDescending);
            }

            UpdateQuestsList();
        }

        // The rows currently on screen: Copy uses the filtered list, not the full catalog.
        private List<Quest> DisplayedQuests() => QuestCatalog.Quests.Where(QuestsFilter().Matches).ToList();

        private void QuestsRefreshAll_Hit(object sender, EventArgs e)
        {
            QuestHistory.Refresh();
            FlashButton(sender as HudButton);
        }

        // Chat rather than Think so the help reads in pink: a /tell takes whatever colour the
        // client gives the tell channel, while AddChatText lets us pick one. Still two lines,
        // to keep the two topics apart.
        private void QuestsHelp_Hit(object sender, EventArgs e)
        {
            Util.Chat("Quest flags are sourced from the ACE database and ILT Mega Book v2.0. Some may be unobtainable. Verified flags have been confirmed by players on this server.", Util.ColorPink);
            Util.Chat("New flags are ones you've discovered that aren't in the master list yet. Click Send to share them and contribute to this server.", Util.ColorPink);
            Util.Chat(QuestCatalog.SourceDescription(), Util.ColorPink);
        }

        // ---- Send ------------------------------------------------------------------------------
        // Exports the flags the master list doesn't cover yet, posts them to the submission
        // channel, and leaves the file behind either way so it can be passed along by hand.
        //
        // Issues no server command: every quest tab runs QuestFlag.Refresh() on first view, and
        // Refresh sits one button to the right for a deliberate re-pull.
        private void QuestsSend_Hit(object sender, EventArgs e) => SendQuestFlags();

        // Also "/od quests send", which stays reachable when the button is hidden — hence the
        // explicit nothing-to-send message rather than a silent no-op.
        public void SendQuestFlags()
        {
            int held = QuestHistory.Count;
            if (held == 0)
            {
                Util.Chat("Send: no quest flags have been observed yet.", Util.ColorPink);
                return;
            }

            QuestSubmit.Pending(out List<Quest> unknown, out List<Quest> unverified);
            List<Quest> send = unknown.Concat(unverified).OrderBy(q => q.Flag).ToList();

            Util.Chat($"Send: {held} flags tracked. {unknown.Count} not in the master list, {unverified.Count} unverified on {Server.Name}.", Util.ColorPink);

            if (send.Count == 0)
            {
                Util.Chat("Nothing new to send - every tracked flag is already covered.", Util.ColorPink);
                return;
            }

            string path;
            try
            {
                // One import-ready CSV for new and known-but-unverified evidence.
                path = QuestExport.ToSubmissionCsv(send);
            }
            catch (Exception ex)
            {
                Util.Log(ex);
                Util.Chat($"Send: could not write the export file - {ex.Message}", Util.ColorRed);
                return;
            }

            Util.ClipboardCopy(SendBreakdown(held, unknown, unverified));
            SendPost(path, send, unknown.Count, unverified.Count);
        }

        // Posts the file just written rather than re-deriving its text, so what arrives is exactly
        // what's on disk and the manual fallback can't disagree with it. Names what leaves the
        // machine — this ships to other players, and a button called Send shouldn't be vague.
        private static void SendPost(string path, List<Quest> send, int unknown, int unverified)
        {
            string character = CoreManager.Current.CharacterFilter.Name;
            string server = Server.Name;

            if (!QuestSubmit.CanSend)
            {
                Util.Chat($"Send: no submission address in this build - pass {path} along by hand.", Util.ColorPink);
                return;
            }

            string content;
            try
            {
                content = File.ReadAllText(path);
            }
            catch (Exception ex)
            {
                Util.Log(ex);
                Util.Chat($"Send: wrote {path} but couldn't read it back to post it.", Util.ColorRed);
                return;
            }

            string summary = $"**{character}** ({server}) - {send.Count} flags: {unknown} new, {unverified} unverified";
            string[] flags = send.Select(q => q.Flag).ToArray();

            bool started = QuestSubmit.SendAsync(Path.GetFileName(path), content, summary, server, flags, (success, reason) =>
            {
                if (success)
                {
                    Util.Chat($"Sent {send.Count} flags as {character} of {server}. Thanks! They won't be sent again.", Util.ColorPink);
                }
                else
                {
                    Util.Chat($"Send: not posted - {reason}. Saved to {path}, pass it along by hand.", Util.ColorPink);
                }
            }, out string startReason);

            if (!started)
            {
                Util.Chat($"Send: not posted - {startReason}. Saved to {path}, pass it along by hand.", Util.ColorPink);
            }
        }

        private static string SendBreakdown(int held, List<Quest> unknown, List<Quest> unverified)
        {
            var sb = new StringBuilder();

            sb.AppendLine($"# {CoreManager.Current.CharacterFilter.Name} ({Server.Name}) {DateTime.Now:yyyy-MM-dd HH:mm} - {held} flags tracked");
            sb.AppendLine();
            sb.AppendLine($"# Not in the master list ({unknown.Count}) - flag,name");
            foreach (Quest quest in unknown)
            {
                string name = quest.DisplayName();
                sb.AppendLine(name.Length > 0 ? $"{quest.Flag},{name}" : quest.Flag);
            }

            sb.AppendLine();
            sb.AppendLine($"# Not verified on {Server.Name} ({unverified.Count})");
            foreach (Quest quest in unverified) { sb.AppendLine(quest.Flag); }

            return sb.ToString();
        }

        private void QuestsClipboard_Hit(object sender, EventArgs e)
        {
            List<Quest> quests = DisplayedQuests();
            string text = string.Join("\n", quests.Select(QuestExport.Describe));
            Util.ClipboardCopy(text);
            Util.Chat($"Copied {quests.Count} quests to clipboard");
        }

        private void QuestsList_Click(object sender, int row, int col)
        {
            string flag = ((HudStaticText)QuestsList[row][1]).Text;

            Quest quest = QuestCatalog.Quests.FirstOrDefault(x => x.Flag == flag);
            if (quest == null) { return; }

            // Picking the row is on top of what the column click already does, not instead of it:
            // the URL / details / flag-history actions below are what most clicks are for, and
            // losing them to make room for selection would be a bad trade. Clicking the picked row
            // again lets it go, so there's a way out that isn't Reset.
            questsSelectedFlag = questsSelectedFlag == flag ? "" : flag;
            UpdateQuestsList();

            QuestFlag.QuestFlags.TryGetValue(flag, out QuestFlag questFlag);

            // Quest URL. Say so when there isn't one rather than swallowing the click — a few
            // hundred rows are internal counters and Conquest-only flags with no wiki page, and
            // silence there is indistinguishable from a misclick.
            if (col == 0)
            {
                if (quest.Url.Length > 0)
                {
                    Util.ThinkQuestUrl($"{quest.Flag}: {quest.Url}", quest.Url);
                }
                else
                {
                    Util.Think($"{quest.Flag}: No url available");
                }
            }

            // Quest Info + Hint
            if (col == 1 || col == 2)
            {
                string details = quest.Details();
                if (details.Length > 0)
                {
                    Util.ThinkQuestDirections($"{quest.Flag}: {details}", quest.Hint);
                }
                else
                {
                    Util.Think($"{quest.Flag}: No hint available");
                }
            }

            // Quest Flag
            if (col >= 3)
            {
                if (questFlag == null)
                {
                    Util.Chat($"{flag}: Never completed", Util.ColorPink);
                }
                else
                {
                    Util.Chat($"{questFlag.ToString()}", Util.ColorPink);
                }
            }
        }
    }
}
