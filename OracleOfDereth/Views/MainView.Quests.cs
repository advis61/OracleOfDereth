using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
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

        // VVS re-renders a cell on every Text assignment, so skip the ones that would write the
        // same string back. Most repaints here change a handful of cells out of ~17,000.
        private static void SetText(HudList.HudListRowAccessor row, int column, string value)
        {
            HudStaticText cell = (HudStaticText)row[column];
            if (cell.Text != value) { cell.Text = value; }
        }

        // Painting every row of quests.csv is expensive, so the tick only repaints when
        // something that affects the list has actually changed: the filter, or the quest
        // flags behind the completed/ready/solves columns.
        private bool questsListStale = true;

        public HudStaticText QuestsText { get; private set; }
        public HudButton QuestsRefresh { get; private set; }
        public HudButton QuestsClipboard { get; private set; }
        public HudButton QuestsExportText { get; private set; }
        public HudButton QuestsExportCsv { get; private set; }
        public HudButton QuestsExportJson { get; private set; }
        public HudTextBox QuestsFilterText { get; private set; }
        public HudButton QuestsFilterReset { get; private set; }
        public HudCheckBox QuestsFilterCompleted { get; private set; }
        public HudCheckBox QuestsFilterIncomplete { get; private set; }
        public HudCheckBox QuestsFilterOneTime { get; private set; }
        public HudCheckBox QuestsFilterRepeatable { get; private set; }
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

            QuestsClipboard = (HudButton)view["QuestsClipboard"];
            QuestsClipboard.Hit += QuestsClipboard_Hit;

            QuestsExportText = (HudButton)view["QuestsExportText"];
            QuestsExportText.Hit += QuestsExportText_Hit;

            QuestsExportCsv = (HudButton)view["QuestsExportCsv"];
            QuestsExportCsv.Hit += QuestsExportCsv_Hit;

            QuestsExportJson = (HudButton)view["QuestsExportJson"];
            QuestsExportJson.Hit += QuestsExportJson_Hit;

            QuestsFilterText = (HudTextBox)view["QuestsFilterText"];
            QuestsFilterText.Change += QuestsFilter_Change;

            QuestsFilterReset = (HudButton)view["QuestsFilterReset"];
            QuestsFilterReset.Hit += QuestsFilterReset_Hit;

            QuestsFilterCompleted = (HudCheckBox)view["QuestsFilterCompleted"];
            QuestsFilterCompleted.Change += QuestsFilter_Change;

            QuestsFilterIncomplete = (HudCheckBox)view["QuestsFilterIncomplete"];
            QuestsFilterIncomplete.Change += QuestsFilter_Change;

            QuestsFilterOneTime = (HudCheckBox)view["QuestsFilterOneTime"];
            QuestsFilterOneTime.Change += QuestsFilter_Change;

            QuestsFilterRepeatable = (HudCheckBox)view["QuestsFilterRepeatable"];
            QuestsFilterRepeatable.Change += QuestsFilter_Change;

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
            QuestsFilterOneTime.Change -= QuestsFilter_Change;
            QuestsFilterRepeatable.Change -= QuestsFilter_Change;
            QuestsFilterNew.Change -= QuestsFilter_Change;
            QuestsClipboard.Hit -= QuestsClipboard_Hit;
            QuestsExportText.Hit -= QuestsExportText_Hit;
            QuestsExportCsv.Hit -= QuestsExportCsv_Hit;
            QuestsExportJson.Hit -= QuestsExportJson_Hit;
            QuestsRefresh.Hit -= QuestFlagsRefresh_Hit;
        }

        public void UpdateQuests()
        {
            if (QuestFlag.MyQuestsRan == false) { QuestFlag.Refresh(); }
            if (questsListStale) { UpdateQuestsList(); }
        }

        // Build the filter from the tab's search box + checkboxes.
        private QuestFilter QuestsFilter()
        {
            return new QuestFilter
            {
                Text = QuestsFilterText?.Text ?? "",
                Completed = QuestsFilterCompleted.Checked,
                Incomplete = QuestsFilterIncomplete.Checked,
                OneTime = QuestsFilterOneTime.Checked,
                Repeatable = QuestsFilterRepeatable.Checked,
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
                SetText(row, 2, quest.Name);
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

            // Trim surplus rows the filter has hidden, dropping their image boxes from the
            // tracking dict — otherwise those (now destroyed) boxes leak as dead keys.
            while (QuestsList.RowCount > quests.Count)
            {
                HudList.HudListRowAccessor row = QuestsList[QuestsList.RowCount - 1];
                AssignedImages.Remove((HudPictureBox)row[0]);
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
                QuestsText.Text = $"Quest Flags: {quests.Count} quests";
            } else {
                QuestsText.Text = $"Quest Flags: {completed} of {quests.Count} completed";
            }

            questsListStale = false;
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
            QuestsFilterOneTime.Checked = false;
            QuestsFilterRepeatable.Checked = false;
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

            // Quest URL
            if (col == 0 && quest.Url.Length > 0)
            {
                Util.ThinkQuestUrl($"{quest.Flag}: {quest.Url}", quest.Url);
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
