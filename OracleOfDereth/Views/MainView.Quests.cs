using Decal.Adapter;
using System;
using System.Collections.Generic;
using System.Drawing;
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

        // Which rows are currently tinted as new, parallel to the list's rows. AssignSelected
        // writes four colour properties per row, so firing it on every row of every repaint is
        // the most expensive thing on this tab — this lets it fire only when a row's tint
        // actually flips.
        private readonly List<bool> questsRowTinted = new List<bool>();

        public HudStaticText QuestsText { get; private set; }
        public HudButton QuestsRefresh { get; private set; }
        public HudButton QuestsSend { get; private set; }
        public HudButton QuestsClipboard { get; private set; }
        public HudButton QuestsExportText { get; private set; }
        public HudButton QuestsExportCsv { get; private set; }
        public HudButton QuestsExportJson { get; private set; }
        public HudButton QuestsHelp { get; private set; }
        public HudTextBox QuestsFilterText { get; private set; }
        public HudButton QuestsFilterReset { get; private set; }
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

            QuestsSend = (HudButton)view["QuestsSend"];
            QuestsSend.Hit += QuestsSend_Hit;

            QuestsClipboard = (HudButton)view["QuestsClipboard"];
            QuestsClipboard.Hit += QuestsClipboard_Hit;

            QuestsExportText = (HudButton)view["QuestsExportText"];
            QuestsExportText.Hit += QuestsExportText_Hit;

            QuestsExportCsv = (HudButton)view["QuestsExportCsv"];
            QuestsExportCsv.Hit += QuestsExportCsv_Hit;

            QuestsExportJson = (HudButton)view["QuestsExportJson"];
            QuestsExportJson.Hit += QuestsExportJson_Hit;

            QuestsHelp = (HudButton)view["QuestsHelp"];
            QuestsHelp.Hit += QuestsHelp_Hit;

            QuestsFilterText = (HudTextBox)view["QuestsFilterText"];
            QuestsFilterText.Change += QuestsFilter_Change;

            QuestsFilterReset = (HudButton)view["QuestsFilterReset"];
            QuestsFilterReset.Hit += QuestsFilterReset_Hit;

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
            QuestsExportText.Hit -= QuestsExportText_Hit;
            QuestsExportCsv.Hit -= QuestsExportCsv_Hit;
            QuestsExportJson.Hit -= QuestsExportJson_Hit;
            QuestsHelp.Hit -= QuestsHelp_Hit;
            QuestsRefresh.Hit -= QuestFlagsRefresh_Hit;
            QuestsSend.Hit -= QuestsSend_Hit;
        }

        public void UpdateQuests()
        {
            if (QuestFlag.MyQuestsRan == false) { QuestFlag.Refresh(); }

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
            QuestFilter filter = QuestsFilter();

            // With nothing filtering, use the collection as-is. The Where(...).ToList() otherwise
            // allocates a 4,000-entry list and runs a delegate per row on every single repaint,
            // only to reproduce the list we already have. Read-only either way.
            List<Quest> quests = filter.IsActive ? Quest.Quests.Where(filter.Matches).ToList() : Quest.Quests;
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

                while (questsRowTinted.Count <= x) { questsRowTinted.Add(false); }

                // Update
                Quest quest = quests[x];

                bool complete = quest.IsComplete();
                if (complete) { completed += 1; }

                AssignImage((HudPictureBox)row[0], complete);
                SetText(row, 1, quest.Flag);
                SetText(row, 2, quest.DisplayName());
                SetText(row, 3, quest.Status());
                SetText(row, 4, quest.SolvesText());

                // Flags the server reported that quests.csv doesn't list are tinted rather than
                // tagged in the Name column — their name is the game's own description, which
                // already fills the column, and a "(new)" prefix would just crowd it out.
                if (questsRowTinted[x] != quest.IsNew)
                {
                    AssignSelected(row, quest.IsNew, QuestsRowColumns);
                    questsRowTinted[x] = quest.IsNew;
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

            UpdateQuestsList();
        }

        // Each header toggles its own column between ascending and descending, same as the John
        // and Titles tabs. Quest.Sort reorders the collection, so the repaint picks it up.
        void QuestsListSortComplete_Click(object sender, EventArgs e)
        {
            if (Quest.CurrentSortType == Quest.SortType.CompleteAscending) {
                Quest.Sort(Quest.SortType.CompleteDescending);
            } else {
                Quest.Sort(Quest.SortType.CompleteAscending);
            }

            UpdateQuestsList();
        }

        void QuestsListSortFlag_Click(object sender, EventArgs e)
        {
            if (Quest.CurrentSortType == Quest.SortType.FlagAscending) {
                Quest.Sort(Quest.SortType.FlagDescending);
            } else {
                Quest.Sort(Quest.SortType.FlagAscending);
            }

            UpdateQuestsList();
        }

        void QuestsListSortName_Click(object sender, EventArgs e)
        {
            if (Quest.CurrentSortType == Quest.SortType.NameAscending) {
                Quest.Sort(Quest.SortType.NameDescending);
            } else {
                Quest.Sort(Quest.SortType.NameAscending);
            }

            UpdateQuestsList();
        }

        void QuestsListSortReady_Click(object sender, EventArgs e)
        {
            if (Quest.CurrentSortType == Quest.SortType.ReadyAscending) {
                Quest.Sort(Quest.SortType.ReadyDescending);
            } else {
                Quest.Sort(Quest.SortType.ReadyAscending);
            }

            UpdateQuestsList();
        }

        // Solves opens descending, unlike the other columns: the vast majority of rows have no
        // solve count at all, so ascending would just show thousands of blanks.
        void QuestsListSortSolves_Click(object sender, EventArgs e)
        {
            if (Quest.CurrentSortType == Quest.SortType.SolvesDescending) {
                Quest.Sort(Quest.SortType.SolvesAscending);
            } else {
                Quest.Sort(Quest.SortType.SolvesDescending);
            }

            UpdateQuestsList();
        }

        // The rows currently on screen: the collection narrowed by the search box and the
        // status/new checkboxes. Export and Copy act on this, not the full list, so what you
        // save matches what you see.
        private List<Quest> DisplayedQuests() => Quest.Quests.Where(QuestsFilter().Matches).ToList();

        private void QuestsExportText_Hit(object sender, EventArgs e)
        {
            List<Quest> quests = DisplayedQuests();
            string path = QuestExport.ToText(quests);
            Util.ClipboardCopy(path);
            Util.Chat($"Exported {quests.Count} quests to {path}");
        }

        private void QuestsExportCsv_Hit(object sender, EventArgs e)
        {
            List<Quest> quests = DisplayedQuests();
            string path = QuestExport.ToCsv(quests);
            Util.ClipboardCopy(path);
            Util.Chat($"Exported {quests.Count} quests to {path}");
        }

        private void QuestsExportJson_Hit(object sender, EventArgs e)
        {
            List<Quest> quests = DisplayedQuests();
            string path = QuestExport.ToJson(quests);
            Util.ClipboardCopy(path);
            Util.Chat($"Exported {quests.Count} quests to {path}");
        }

        // Chat rather than Think so the help reads in pink: a /tell takes whatever colour the
        // client gives the tell channel, while AddChatText lets us pick one. Still two lines,
        // to keep the two topics apart.
        private void QuestsHelp_Hit(object sender, EventArgs e)
        {
            Util.Chat("This list of quest flags was built from the ACE database and the ILT Mega Book v2.0 with AI assistance, so expect the odd mistake - the plugin's other lists are all hand-curated.", Util.ColorPink);
            Util.Chat("AC quest flags are kind of a cluster, so some of these are likely unobtainable. Looking for help curating this list.", Util.ColorPink);
            Util.Chat("The New filter shows flags this server reports that aren't in the Oracle of Dereth master list yet. Send them along to Advis Eveldan if you'd like them added.", Util.ColorPink);
        }

        // ---- Send button --------------------------------------------------------------------
        // Writes the two things worth curating — flags this character holds that quests.csv has
        // never heard of, and flags it holds that the CSV lists but hasn't marked Verified — to a
        // file alongside the other exports, in /myquests format. Attach that file on Discord;
        // nothing is uploaded from here.
        //
        // Reads the tracked flags as they stand and issues no server command of its own. The
        // collection is already current: every tab that shows quest data runs QuestFlag.Refresh()
        // on first view, and Refresh is one button to the right for a deliberate re-pull. That
        // makes this synchronous — no /log to toggle, no waiting on chat, nothing left half-done
        // if the character logs out.
        private void QuestsSend_Hit(object sender, EventArgs e)
        {
            int flagCount = QuestFlag.QuestFlags.Count;

            if (flagCount == 0)
            {
                Util.Chat("Send: no quest flags tracked yet - hit Refresh first.", Util.ColorPink);
                return;
            }

            QuestsSendReport(flagCount);
        }

        private void QuestsSendReport(int flagCount)
        {
            // The merge normally happens on MainView's own tick; call it here so the diff can't
            // run against a collection that's a tick behind. It's idempotent.
            Quest.MergeQuestFlags();

            // Two separate curation jobs, so they're reported separately: unknown flags need a new
            // CSV row written, known-but-unverified ones only need the column filled in.
            List<Quest> unknown = Quest.Quests
                .Where(q => q.IsNew && q.IsComplete())
                .OrderBy(q => q.Flag).ToList();

            List<Quest> unverified = Quest.Quests
                .Where(q => !q.IsNew && q.IsComplete() && !q.VerifiedConquest)
                .OrderBy(q => q.Flag).ToList();

            // Both groups go in the one file: each needs sending, and the recipient can tell them
            // apart by checking against the master list. Kept as pure /myquests lines with no
            // headers or blank separators, so it stays parseable by anything that reads a real
            // chat log — the breakdown lives in chat and on the clipboard instead.
            List<Quest> send = unknown.Concat(unverified).OrderBy(q => q.Flag).ToList();

            Util.Chat($"Send: {flagCount} flags held. {unknown.Count} not in the master list, {unverified.Count} held but unverified.", Util.ColorPink);

            if (send.Count == 0)
            {
                Util.Chat("Nothing new to send - the master list already covers every flag you hold.", Util.ColorPink);
                return;
            }

            string path;
            try
            {
                path = QuestExport.ToMyQuests(send);
            }
            catch (Exception ex)
            {
                Util.Log(ex);
                Util.Chat($"Send: could not write the export file - {ex.Message}", Util.ColorRed);
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine($"# {CoreManager.Current.CharacterFilter.Name} ({Server.Name}) {DateTime.Now:yyyy-MM-dd HH:mm} - {flagCount} flags held");
            sb.AppendLine();
            sb.AppendLine($"# Not in the master list ({unknown.Count}) - flag,name");
            foreach (Quest quest in unknown)
            {
                string name = quest.DisplayName();
                sb.AppendLine(name.Length > 0 ? $"{quest.Flag},{name}" : quest.Flag);
            }
            sb.AppendLine();
            sb.AppendLine($"# Held but not marked Verified ({unverified.Count})");
            foreach (Quest quest in unverified) { sb.AppendLine(quest.Flag); }

            Util.ClipboardCopy(sb.ToString());

            Util.Chat($"Wrote {send.Count} flags to {path} - attach that file on Discord. Breakdown copied to clipboard.", Util.ColorPink);

            // Chat gets a preview only. The interesting list runs to hundreds of rows on a mature
            // character, and the clipboard already has all of it.
            QuestsSendPreview("Not in the master list", unknown);
            QuestsSendPreview("Held but not marked Verified", unverified);
        }

        private const int QuestsSendPreviewRows = 20;

        private static void QuestsSendPreview(string heading, List<Quest> quests)
        {
            if (quests.Count == 0) { return; }

            Util.Chat($"{heading}:", Util.ColorPink);

            foreach (Quest quest in quests.Take(QuestsSendPreviewRows))
            {
                string name = quest.DisplayName();
                Util.Chat(name.Length > 0 ? $"  {quest.Flag} - {name}" : $"  {quest.Flag}", Util.ColorPink);
            }

            if (quests.Count > QuestsSendPreviewRows)
            {
                Util.Chat($"  ...and {quests.Count - QuestsSendPreviewRows} more (clipboard has them all).", Util.ColorPink);
            }
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

            Quest quest = Quest.Quests.FirstOrDefault(x => x.Flag == flag);
            if (quest == null) { return; }

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
                    Util.Think($"{quest.Flag}: No Url");
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
