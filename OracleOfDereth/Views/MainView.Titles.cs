using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using VirindiViewService.Controls;

namespace OracleOfDereth
{
    partial class MainView
    {
        public HudList TitlesList { get; private set; }
        public HudStaticText TitlesText { get; private set; }
        public HudCheckBox TitlesDisplayUnavailable { get; private set; }

        public HudFixedLayout TitlesListSortComplete { get; private set; }
        public HudPictureBox TitlesListSortCompleteIcon { get; private set; }

        public HudStaticText TitlesListSortName { get; private set; }
        public HudStaticText TitlesListSortLevel { get; private set; }
        public HudStaticText TitlesListSortCategory { get; private set; }

        private void InitTitles()
        {
            TitlesText = (HudStaticText)view["TitlesText"];
            TitlesText.FontHeight = 10;

            TitlesDisplayUnavailable = (HudCheckBox)view["TitlesDisplayUnavailable"];
            TitlesDisplayUnavailable.Change += TitlesDisplayUnavailable_Change;

            TitlesList = (HudList)view["TitlesList"];
            TitlesList.Click += TitlesList_Click;
            TitlesList.ClearRows();

            TitlesListSortCompleteIcon = new HudPictureBox();
            TitlesListSortCompleteIcon.Image = IconSort;
            TitlesListSortComplete = (HudFixedLayout)view["TitlesListSortComplete"];
            TitlesListSortComplete.AddControl(TitlesListSortCompleteIcon, new Rectangle(0, 0, 16, 16));
            TitlesListSortCompleteIcon.Hit += TitlesListSortComplete_Click;

            TitlesListSortName = (HudStaticText)view["TitlesListSortName"];
            TitlesListSortName.Hit += TitlesListSortName_Click;

            TitlesListSortLevel = (HudStaticText)view["TitlesListSortLevel"];
            TitlesListSortLevel.Hit += TitlesListSortLevel_Click;

            TitlesListSortCategory = (HudStaticText)view["TitlesListSortCategory"];
            TitlesListSortCategory.Hit += TitlesListSortCategory_Click;
        }

        private void DisposeTitles()
        {
            TitlesList.Click -= TitlesList_Click;
            TitlesDisplayUnavailable.Change -= TitlesDisplayUnavailable_Change;
            TitlesListSortCompleteIcon.Hit -= TitlesListSortComplete_Click;
            TitlesListSortName.Hit -= TitlesListSortName_Click;
            TitlesListSortLevel.Hit -= TitlesListSortLevel_Click;
            TitlesListSortCategory.Hit -= TitlesListSortCategory_Click;
        }

        public void UpdateTitles()
        {
            UpdateTitlesList();
            UpdateTitlesTexts();
        }

        // Titles to show: always the available ones, plus the unavailable ones when the
        // "Display Unavailable" checkbox is ticked. Drawn from the sorted Title.Titles list
        // so the current sort order is preserved across the merge.
        private List<Title> DisplayedTitles()
        {
            bool showUnavailable = TitlesDisplayUnavailable.Checked;
            return Title.Titles.Where(t => showUnavailable || t.Category != "Unavailable").ToList();
        }

        private void UpdateTitlesList()
        {
            List<Title> titles = DisplayedTitles();

            for (int x = 0; x < titles.Count; x++) {
                HudList.HudListRowAccessor row;

                if (x >= TitlesList.RowCount) {
                    row = TitlesList.AddRow();

                    ((HudStaticText)row[2]).TextAlignment = VirindiViewService.WriteTextFormats.Center;
                } else {
                    row = TitlesList[x];
                }

                // Update
                Title title = titles[x];
                if (title.Name == "Blank") { continue; }

                ((HudStaticText)row[1]).Text = title.Name;
                if (title.TitleId == 0) { continue; }

                AssignImage((HudPictureBox)row[0], title.IsComplete());
                ((HudStaticText)row[2]).Text = title.Level.ToString();
                ((HudStaticText)row[3]).Text = title.Category;
                ((HudStaticText)row[4]).Text = title.TitleId.ToString();
            }

            // Trim stale rows left behind after unchecking "Display Unavailable".
            while (TitlesList.RowCount > titles.Count) { TitlesList.RemoveRow(TitlesList.RowCount - 1); }
        }

        private void TitlesList_Click(object sender, int row, int col)
        {
            string text = ((HudStaticText)TitlesList[row][4]).Text;
            if (text == null || text == "" || text.IndexOf('-') > 0) { return; }

            int titleId = int.Parse(text);

            Title title = Title.Titles.FirstOrDefault(x => x.TitleId == titleId);
            if (title == null) { return; }

            // Quest URL
            if (col == 0 && title.Url.Length > 0) {
                Util.ThinkQuestUrl($"{title.Name}: {title.Url}", title.Url);
            }

            if((col == 1 || col == 2 || col == 3) && title.Hint.Length > 0) {
                Util.ThinkQuestDirections($"{title.Name}: {title.Hint}", title.Hint);
            }

            // Debug
            if (col > 3) {
                Util.Chat($"Name:{title.Name} TitleId:{title.TitleId}", Util.ColorPink);
            }
        }

        void TitlesDisplayUnavailable_Change(object sender, EventArgs e)
        {
            UpdateTitlesList();
        }

        void TitlesListSortName_Click(object sender, EventArgs e)
        {
            if (Title.CurrentSortType == Title.SortType.NameAscending) {
                Title.Sort(Title.SortType.NameDescending);
            } else {
                Title.Sort(Title.SortType.NameAscending);
            }

            UpdateTitlesList();
        }


        void TitlesListSortLevel_Click(object sender, EventArgs e)
        {
            if (Title.CurrentSortType == Title.SortType.LevelAscending)
            {
                Title.Sort(Title.SortType.LevelDescending);
            }
            else
            {
                Title.Sort(Title.SortType.LevelAscending);
            }

            UpdateTitlesList();
        }

        void TitlesListSortCategory_Click(object sender, EventArgs e)
        {
            if (Title.CurrentSortType == Title.SortType.CategoryAscending)
            {
                Title.Sort(Title.SortType.CategoryDescending);
            }
            else
            {
                Title.Sort(Title.SortType.CategoryAscending);
            }

            UpdateTitlesList();
        }

        void TitlesListSortComplete_Click(object sender, EventArgs e)
        {
            if (Title.CurrentSortType == Title.SortType.CompleteAscending)
            {
                Title.Sort(Title.SortType.CompleteDescending);
            }
            else
            {
                Title.Sort(Title.SortType.CompleteAscending);
            }

            UpdateTitlesList();
        }

        private void UpdateTitlesTexts()
        {
            TitlesText.Text = $"Titles: {Title.KnownTitleIds.Count} completed";
        }
    }
}
