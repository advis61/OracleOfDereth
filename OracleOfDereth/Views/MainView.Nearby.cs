using Decal.Adapter;
using System;
using System.Collections.Generic;
using System.Linq;
using VirindiViewService.Controls;

namespace OracleOfDereth
{
    partial class MainView
    {
        public HudList NearbyList { get; private set; }
        public HudCombo NearbySort { get; private set; }
        public HudCheckBox NearbySimpleList { get; private set; }
        public HudCheckBox NearbyFilterPlayers { get; private set; }
        public HudCheckBox NearbyFilterMonsters { get; private set; }
        public HudCheckBox NearbyFilterOther { get; private set; }

        private readonly List<int> NearbyListColumns = new List<int> { 1, 2, 3 };
        private const string NearbySortSetting = "NearbySort";
        private string nearbyClickGroup;
        private int nearbyClickRow;
        private int nearbyClickColumn;
        private long nearbyClickTime;
        private System.Drawing.Point nearbyClickPosition;
        public static Dictionary<string, bool> NearbyListExpanded = new Dictionary<string, bool>();

        private void InitNearby()
        {
            NearbyListExpanded.Clear();
            NearbySort = (HudCombo)view["NearbySort"];
            NearbySort.AddItem("Sort by Default", "Sort by Default"); // 0
            NearbySort.AddItem("Sort by Distance", "Sort by Distance"); // 1
            NearbySort.AddItem("Sort by Name", "Sort by Name"); // 2
            NearbySort.AddItem("Sort by Relevance", "Sort by Relevance"); // 3
            if (!Enum.TryParse(SettingsFile.GetSetting(NearbySortSetting, "Relevance"), true, out NearbyItem.SortType savedSort) ||
                !Enum.IsDefined(typeof(NearbyItem.SortType), savedSort))
            {
                savedSort = NearbyItem.SortType.Relevance;
            }
            NearbySort.Current = (int)savedSort;
            NearbyItem.Sort(savedSort);
            NearbySort.Change += NearbySort_Change;

            NearbySimpleList = (HudCheckBox)view["NearbySimpleList"];
            NearbySimpleList.Change += NearbySimpleList_Change;

            NearbyFilterPlayers = (HudCheckBox)view["NearbyFilterPlayers"];
            NearbyFilterMonsters = (HudCheckBox)view["NearbyFilterMonsters"];
            NearbyFilterOther = (HudCheckBox)view["NearbyFilterOther"];
            NearbyFilterPlayers.Change += NearbyFilter_Change;
            NearbyFilterMonsters.Change += NearbyFilter_Change;
            NearbyFilterOther.Change += NearbyFilter_Change;

            NearbyList = (HudList)view["NearbyList"];
            NearbyList.Click += NearbyList_Click;
            NearbyList.ClearRows();
        }

        private void DisposeNearby()
        {
            NearbyListExpanded.Clear();
            nearbyClickGroup = null;
            NearbySort.Change -= NearbySort_Change;
            NearbySimpleList.Change -= NearbySimpleList_Change;
            NearbyFilterPlayers.Change -= NearbyFilter_Change;
            NearbyFilterMonsters.Change -= NearbyFilter_Change;
            NearbyFilterOther.Change -= NearbyFilter_Change;
            NearbyList.Click -= NearbyList_Click;
        }

        public void UpdateNearby()
        {
            UpdateNearbyList();
        }

        private void NearbySort_Change(object sender, EventArgs e)
        {
            NearbyItem.SortType sort = (NearbyItem.SortType)NearbySort.Current;
            NearbyItem.Sort(sort);
            SettingsFile.PutSetting(NearbySortSetting, sort.ToString());
            UpdateNearbyList();
        }

        private void NearbyFilter_Change(object sender, EventArgs e)
        {
            UpdateNearbyList();
        }

        private void NearbySimpleList_Change(object sender, EventArgs e)
        {
            UpdateNearbyList();
        }

        private List<NearbyItem> FilteredNearbyItems() => NearbyItem.NearbyItems()
                .Where(item => NearbyItem.MatchesFilter(item.IsPlayer(), item.IsMonster(),
                    NearbyFilterPlayers.Checked, NearbyFilterMonsters.Checked, NearbyFilterOther.Checked))
                .ToList();

        private void UpdateNearbyList()
        {
            int index = NearbyListAdd(FilteredNearbyItems(), 0);

            while (NearbyList.RowCount > index) { NearbyList.RemoveRow(NearbyList.RowCount - 1); }
        }

        private int NearbyListAdd(List<NearbyItem> items, int index)
        {
            if (items.Count == 0) { NearbyListExpanded.Clear(); return index; }

            HudList.HudListRowAccessor row;
            int targetId = Target.GetCurrent().Id;
            bool showWcid = Setting.ShowNearbyWcid.IsYes;

            List<IGrouping<string, NearbyItem>> grouped = items
                .GroupBy(i => NearbySimpleList.Checked ? i.Item.Id.ToString() : i.GroupKey()).ToList();

            // Discard expansion state for groups no longer displayed instead of retaining
            // every creature/fellowship name encountered during a long session.
            var currentGroups = new HashSet<string>(grouped.Select(group => group.Key));
            foreach (string key in NearbyListExpanded.Keys.Where(key => !currentGroups.Contains(key)).ToArray())
                NearbyListExpanded.Remove(key);

            foreach (var group in grouped)
            {
                List<NearbyItem> groupItems = NearbyItem.CurrentSortType == NearbyItem.SortType.Distance
                    ? group.OrderBy(i => i.Distance()).ThenBy(i => i.Item.Name).ToList()
                    : group.OrderBy(i => i.Item.Name).ThenBy(i => i.Distance()).ToList();
                NearbyListExpanded.TryGetValue(group.Key, out bool expanded);
                bool isGrouped = !NearbySimpleList.Checked && (group.Count() > 1 || group.First().ForceGroup());

                if (isGrouped)
                {
                    if (index >= NearbyList.RowCount) { row = NearbyList.AddRow(); } else { row = NearbyList[index]; }
                    index++;

                    NearbyItem item = groupItems.First();

                    AssignImage((HudPictureBox)row[0], item.Item.Icon);
                    AssignSelected(row, (!expanded && groupItems.Any(i => i.Item.Id == targetId)), NearbyListColumns);

                    SetText(row, 1, showWcid
                        ? $"[{item.Item.Type}] {group.Key} ({group.Count()})"
                        : $"{group.Key} ({group.Count()})");
                    SetText(row, 2, (expanded ? "[-]" : "[+]"));
                    SetText(row, 3, item.Item.Id.ToString());
                    SetText(row, 4, group.Key);
                }

                // Maybe render items
                if (expanded || !isGrouped)
                {
                    foreach (NearbyItem item in groupItems)
                    {
                        if (index >= NearbyList.RowCount) { row = NearbyList.AddRow(); } else { row = NearbyList[index]; }
                        index++;

                        AssignImage((HudPictureBox)row[0], (isGrouped ? 0 : item.Item.Icon));
                        SetText(row, 1, showWcid
                            ? $"[{item.Item.Type}] {item.Item.Name}"
                            : item.Item.Name);

                        if (item.Item.Id == targetId)
                        {
                            AssignSelected(row, true, NearbyListColumns);
                            SetText(row, 2, ((int)item.Distance()).ToString());
                        }
                        else
                        {
                            AssignSelected(row, false, NearbyListColumns);
                            SetText(row, 2, "");
                        }

                        SetText(row, 3, item.Item.Id.ToString());
                        SetText(row, 4, "");
                    }
                }

                if (expanded && isGrouped) { index = NearbyListAddBlank(index); }
            }

            return index;
        }

        private int NearbyListAddBlank(int index)
        {
            HudList.HudListRowAccessor row;

            if (index >= NearbyList.RowCount) { row = NearbyList.AddRow(); } else { row = NearbyList[index]; }
            AssignImage((HudPictureBox)row[0], 0);
            SetText(row, 1, "");
            SetText(row, 2, "");
            SetText(row, 3, "");
            SetText(row, 4, "");

            return (index + 1);
        }

        private void NearbyList_Click(object sender, int row, int col)
        {
            if (row < 0 || row >= NearbyList.RowCount) return;
            string group = ((HudStaticText)NearbyList[row][4]).Text;

            if (!int.TryParse(((HudStaticText)NearbyList[row][3]).Text, out int id))
            {
                nearbyClickGroup = null;
                return;
            }

            long now = System.Diagnostics.Stopwatch.GetTimestamp();
            var position = System.Windows.Forms.Cursor.Position;
            var doubleClickSize = System.Windows.Forms.SystemInformation.DoubleClickSize;
            bool doubleClick = !string.IsNullOrEmpty(group) && group == nearbyClickGroup &&
                row == nearbyClickRow && col == nearbyClickColumn &&
                (now - nearbyClickTime) * 1000.0 / System.Diagnostics.Stopwatch.Frequency <=
                    System.Windows.Forms.SystemInformation.DoubleClickTime &&
                Math.Abs(position.X - nearbyClickPosition.X) <= doubleClickSize.Width / 2 &&
                Math.Abs(position.Y - nearbyClickPosition.Y) <= doubleClickSize.Height / 2;

            // Consume the pair so a third click starts a new gesture. The toggle column
            // already works with one click and must not prime a double-click on the name.
            nearbyClickGroup = doubleClick || col == 2 ? null : group;
            nearbyClickRow = row;
            nearbyClickColumn = col;
            nearbyClickTime = now;
            nearbyClickPosition = position;

            if (!string.IsNullOrEmpty(group) && (col == 2 || doubleClick))
            {
                NearbyListExpanded.TryGetValue(group, out bool expanded);
                NearbyListExpanded[group] = !expanded;
            } else {
                if (!string.IsNullOrEmpty(group))
                {
                    // Resolve at click time so movement and list sorting cannot pick a farther item.
                    NearbyItem closest = FilteredNearbyItems().Where(i => i.GroupKey() == group)
                        .OrderBy(i => i.Distance()).FirstOrDefault();
                    if (closest == null) return;
                    id = closest.Item.Id;
                }
                CoreManager.Current.Actions.SelectItem(id);
            }

            UpdateNearbyList();
        }
    }
}
