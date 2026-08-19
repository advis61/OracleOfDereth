using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using VirindiViewService.Controls;

namespace OracleOfDereth
{
    partial class MainView
    {
        // The shortlist the player built on the Flags tab. Same columns and the same click
        // behaviour as that tab, minus the filters — the searching happens over there, and what
        // lands here is already the short list.
        private static readonly List<int> FavoritesRowColumns = new List<int> { 1, 2, 3, 4 };

        private readonly List<int> favoritesRowTinted = new List<int>();

        // The picked row, by flag rather than index — same reason as the Flags tab.
        private string favoritesSelectedFlag = "";

        public HudStaticText FavoritesText { get; private set; }
        public HudButton FavoritesRemove { get; private set; }
        public HudButton FavoritesRefresh { get; private set; }

        // Reorder arrows. Picture boxes in a fixed layout rather than push buttons, same as the
        // sort icons on the other tabs.
        public HudFixedLayout FavoritesUp { get; private set; }
        public HudFixedLayout FavoritesDown { get; private set; }
        public HudPictureBox FavoritesUpIcon { get; private set; }
        public HudPictureBox FavoritesDownIcon { get; private set; }

        public HudStaticText FavoritesListFlag { get; private set; }
        public HudStaticText FavoritesListName { get; private set; }
        public HudStaticText FavoritesListReady { get; private set; }
        public HudStaticText FavoritesListSolves { get; private set; }

        public HudList FavoritesList { get; private set; }

        private void InitFavorites()
        {
            FavoritesText = (HudStaticText)view["FavoritesText"];
            FavoritesText.FontHeight = 10;

            FavoritesRemove = (HudButton)view["FavoritesRemove"];
            FavoritesRemove.Hit += FavoritesRemove_Hit;

            FavoritesRefresh = (HudButton)view["FavoritesRefresh"];
            FavoritesRefresh.Hit += QuestFlagsRefresh_Hit;

            FavoritesUpIcon = new HudPictureBox();
            FavoritesUpIcon.Image = IconArrowUp;
            FavoritesUp = (HudFixedLayout)view["FavoritesUp"];
            FavoritesUp.AddControl(FavoritesUpIcon, new Rectangle(0, 0, 16, 16));
            FavoritesUpIcon.Hit += FavoritesUp_Hit;

            FavoritesDownIcon = new HudPictureBox();
            FavoritesDownIcon.Image = IconArrowDown;
            FavoritesDown = (HudFixedLayout)view["FavoritesDown"];
            FavoritesDown.AddControl(FavoritesDownIcon, new Rectangle(0, 0, 16, 16));
            FavoritesDownIcon.Hit += FavoritesDown_Hit;

            FavoritesListFlag = (HudStaticText)view["FavoritesListFlag"];
            FavoritesListName = (HudStaticText)view["FavoritesListName"];
            FavoritesListReady = (HudStaticText)view["FavoritesListReady"];
            FavoritesListSolves = (HudStaticText)view["FavoritesListSolves"];

            FavoritesList = (HudList)view["FavoritesList"];
            FavoritesList.Click += FavoritesList_Click;
            FavoritesList.ClearRows();
        }

        private void DisposeFavorites()
        {
            FavoritesRemove.Hit -= FavoritesRemove_Hit;
            FavoritesRefresh.Hit -= QuestFlagsRefresh_Hit;
            FavoritesUpIcon.Hit -= FavoritesUp_Hit;
            FavoritesDownIcon.Hit -= FavoritesDown_Hit;
            FavoritesList.Click -= FavoritesList_Click;
        }

        public void UpdateFavorites()
        {
            if (!QuestState.HasRequestedRefresh) { QuestFlag.Refresh(); }

            // Repaint every tick so the Ready column counts down, but not into a closed window —
            // same deal as the Flags tab.
            if (!view.Visible) { return; }

            UpdateFavoritesList();
        }

        private void UpdateFavoritesList()
        {
            List<Quest> quests = QuestFavorite.Quests();

            // Reordering and removing both act on the picked row, so all three appear together.
            bool picked = favoritesSelectedFlag.Length > 0;
            FavoritesRemove.Visible = picked;
            FavoritesUp.Visible = picked;
            FavoritesDown.Visible = picked;

            int completed = 0;

            for (int x = 0; x < quests.Count; x++)
            {
                HudList.HudListRowAccessor row;

                if (x >= FavoritesList.RowCount) {
                    row = FavoritesList.AddRow();

                    ((HudStaticText)row[3]).TextAlignment = VirindiViewService.WriteTextFormats.Right;
                    ((HudStaticText)row[4]).TextAlignment = VirindiViewService.WriteTextFormats.Right;
                } else {
                    row = FavoritesList[x];
                }

                while (favoritesRowTinted.Count <= x) { favoritesRowTinted.Add(TintNone); }

                Quest quest = quests[x];

                bool done = FavoriteIsDone(quest);
                if (done) { completed += 1; }

                AssignImage((HudPictureBox)row[0], done);
                SetText(row, 1, quest.Flag);
                SetText(row, 2, quest.DisplayName());
                SetText(row, 3, quest.Status());
                SetText(row, 4, quest.SolvesText());

                int tint = quest.Flag == favoritesSelectedFlag ? TintRowSelected
                         : quest.IsNew ? TintNew
                         : TintNone;

                if (favoritesRowTinted[x] != tint)
                {
                    AssignTint(row, TintColor(tint), FavoritesRowColumns);
                    favoritesRowTinted[x] = tint;
                }
            }

            while (FavoritesList.RowCount > quests.Count)
            {
                FavoritesList.RemoveRow(FavoritesList.RowCount - 1);
            }

            if (favoritesRowTinted.Count > quests.Count)
            {
                favoritesRowTinted.RemoveRange(quests.Count, favoritesRowTinted.Count - quests.Count);
            }

            // Stored favourites can outnumber the rows: a flag favourited on another server stays
            // in the file but has no row in this server's list. Say so rather than looking like
            // the list lost something.
            int stored = QuestFavorite.Count;
            string missing = stored > quests.Count ? $" ({stored - quests.Count} not on this server)" : "";

            if (quests.Count == 0 && stored == 0) {
                SetText(FavoritesText, "Browse quests to add favorites");
            } else {
                // Counts what the icons show, so the tally can't contradict the list: outstanding
                // means a one-time quest not yet done, or a repeatable off cooldown.
                SetText(FavoritesText, $"Favorites: {quests.Count - completed} of {quests.Count} ready{missing}");
            }
        }

        // The completed icon means "nothing to do right now" here, which is not what it means on
        // the Flags tab. That tab is a record of what a character has earned, so its icon is just
        // IsComplete(). This tab is a worklist — the whole reason to favourite a quest is to run
        // it — so a repeatable sitting on cooldown reads as done, and the same repeatable once its
        // timer elapses reads as outstanding again even though the flag is still held. One-time
        // quests have no cooldown to distinguish, so for them this is still plain IsComplete().
        //
        // Same reading CustomQuest and SocietyQuest use; see the note on Quest.IsComplete().
        private static bool FavoriteIsDone(Quest quest)
        {
            return quest.IsOneTime() ? quest.IsComplete() : !quest.Ready();
        }

        // The picked row keeps its selection across a move, so a row can be walked several places
        // with repeated clicks instead of being re-picked each time.
        private void FavoritesUp_Hit(object sender, EventArgs e)
        {
            if (QuestFavorite.MoveUp(favoritesSelectedFlag)) { UpdateFavoritesList(); }
        }

        private void FavoritesDown_Hit(object sender, EventArgs e)
        {
            if (QuestFavorite.MoveDown(favoritesSelectedFlag)) { UpdateFavoritesList(); }
        }

        private void FavoritesRemove_Hit(object sender, EventArgs e)
        {
            string flag = favoritesSelectedFlag;
            if (flag.Length == 0) { return; }

            if (QuestFavorite.Remove(flag)) { Util.Chat($"Removed from Favorites: {flag}", Util.ColorPink); }

            favoritesSelectedFlag = "";
            UpdateFavoritesList();
        }

        // Same column actions as the Flags tab, so a favourite behaves like the row it came from.
        private void FavoritesList_Click(object sender, int row, int col)
        {
            string flag = ((HudStaticText)FavoritesList[row][1]).Text;

            Quest quest = QuestCatalog.Quests.FirstOrDefault(x => x.Flag == flag);
            if (quest == null) { return; }

            favoritesSelectedFlag = favoritesSelectedFlag == flag ? "" : flag;
            UpdateFavoritesList();

            QuestFlag.QuestFlags.TryGetValue(flag, out QuestFlag questFlag);

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

            if (col == 1 || col == 2)
            {
                string details = quest.Details();
                if (details.Length > 0)
                {
                    Util.ThinkQuestDirections($"{quest.Flag}: {details}", quest.Hint);
                }
            }

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
