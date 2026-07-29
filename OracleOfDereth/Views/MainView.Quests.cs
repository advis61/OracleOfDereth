using System;
using System.Collections.Generic;
using System.Linq;
using VirindiViewService.Controls;

namespace OracleOfDereth
{
    partial class MainView
    {
        // Set while Reset clears the filter controls, so each control's Change event doesn't
        // trigger its own repaint — we do one at the end instead.
        private bool suppressQuestsFilter = false;

        // Painting every row of quests.csv is expensive, so the tick only repaints when
        // something that affects the list has actually changed: the filter, or the quest
        // flags behind the completed/ready/solves columns.
        private bool questsListStale = true;

        public HudStaticText QuestsText { get; private set; }
        public HudButton QuestsRefresh { get; private set; }
        public HudTextBox QuestsFilterText { get; private set; }
        public HudButton QuestsFilterReset { get; private set; }
        public HudCheckBox QuestsFilterCompleted { get; private set; }
        public HudCheckBox QuestsFilterIncomplete { get; private set; }
        public HudList QuestsList { get; private set; }

        private void InitQuests()
        {
            QuestsText = (HudStaticText)view["QuestsText"];
            QuestsText.FontHeight = 10;

            QuestsRefresh = (HudButton)view["QuestsRefresh"];
            QuestsRefresh.Hit += QuestFlagsRefresh_Hit;

            QuestsFilterText = (HudTextBox)view["QuestsFilterText"];
            QuestsFilterText.Change += QuestsFilter_Change;

            QuestsFilterReset = (HudButton)view["QuestsFilterReset"];
            QuestsFilterReset.Hit += QuestsFilterReset_Hit;

            QuestsFilterCompleted = (HudCheckBox)view["QuestsFilterCompleted"];
            QuestsFilterCompleted.Change += QuestsFilter_Change;

            QuestsFilterIncomplete = (HudCheckBox)view["QuestsFilterIncomplete"];
            QuestsFilterIncomplete.Change += QuestsFilter_Change;

            QuestsList = (HudList)view["QuestsList"];
            QuestsList.Click += QuestsList_Click;
            QuestsList.ClearRows();
        }

        private void DisposeQuests()
        {
            QuestsList.Click -= QuestsList_Click;
            QuestsFilterText.Change -= QuestsFilter_Change;
            QuestsFilterReset.Hit -= QuestsFilterReset_Hit;
            QuestsFilterCompleted.Change -= QuestsFilter_Change;
            QuestsFilterIncomplete.Change -= QuestsFilter_Change;
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
            };
        }

        private void UpdateQuestsList()
        {
            List<Quest> quests = Quest.Quests.Where(QuestsFilter().Matches).ToList();
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

                // Update
                Quest quest = quests[x];
                QuestFlag.QuestFlags.TryGetValue(quest.Flag, out QuestFlag questFlag);

                bool complete = quest.IsComplete();
                if (complete) { completed += 1; }

                AssignImage((HudPictureBox)row[0], complete);
                ((HudStaticText)row[1]).Text = quest.Flag;
                ((HudStaticText)row[2]).Text = quest.Name;

                // Same three-way split the Society tab uses: never earned, a permanent one-time
                // stamp (no cooldown to count down, solves always 1), or a repeatable on timer.
                if (questFlag == null) {
                    ((HudStaticText)row[3]).Text = "ready";
                    ((HudStaticText)row[4]).Text = "";
                } else if (quest.IsOneTime()) {
                    ((HudStaticText)row[3]).Text = "completed";
                    ((HudStaticText)row[4]).Text = "";
                } else {
                    ((HudStaticText)row[3]).Text = questFlag.NextAvailable();
                    ((HudStaticText)row[4]).Text = $"{questFlag.Solves}";
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

            // Update Text
            QuestsText.Text = $"Quests Flags: {completed} of {quests.Count} completed";

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
            suppressQuestsFilter = false;

            UpdateQuestsList();
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
