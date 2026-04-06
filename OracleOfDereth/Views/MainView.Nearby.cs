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

        private readonly List<int> NearbyListColumns = new List<int> { 1, 2, 3 };
        public static Dictionary<string, bool> NearbyListExpanded = new Dictionary<string, bool>();
        private static string LastClickGroup = "";
        private static DateTime LastClickAt = DateTime.MinValue;

        private void InitNearby()
        {
            NearbySort = (HudCombo)view["NearbySort"];
            NearbySort.AddItem("Sort by Default", "Sort by Default"); // 0
            NearbySort.AddItem("Sort by Distance", "Sort by Name"); // 1
            NearbySort.AddItem("Sort by Name", "Sort by Distance"); // 2
            NearbySort.Change += NearbySort_Change;

            NearbyList = (HudList)view["NearbyList"];
            NearbyList.Click += NearbyList_Click;
            NearbyList.ClearRows();
        }

        private void DisposeNearby()
        {
            NearbySort.Change -= NearbySort_Change;
            NearbyList.Click -= NearbyList_Click;
        }

        public void UpdateNearby()
        {
            UpdateNearbyList();
        }

        private void NearbySort_Change(object sender, EventArgs e)
        {
            NearbyItem.Sort((NearbyItem.SortType)NearbySort.Current);
            UpdateNearbyList();
        }

        private void UpdateNearbyList()
        {
            int index = 0;
            List<NearbyItem> items = NearbyItem.NearbyItems();

            index = NearbyListAdd(items, index);

            while (NearbyList.RowCount > index) { NearbyList.RemoveRow(NearbyList.RowCount - 1); }
        }

        private int NearbyListAdd(List<NearbyItem> items, int index)
        {
            if (items.Count() == 0) { return index; }

            HudList.HudListRowAccessor row;
            int targetId = Target.GetCurrent().Id;

            List<IGrouping<string, NearbyItem>> grouped = items.GroupBy(i => i.GroupKey()).ToList();

            foreach (var group in grouped)
            {
                NearbyListExpanded.TryGetValue(group.Key, out bool expanded);
                bool isGrouped = (group.Count() > 1 || group.First().ForceGroup());

                if (isGrouped)
                {
                    if (index >= NearbyList.RowCount) { row = NearbyList.AddRow(); } else { row = NearbyList[index]; }
                    index++;

                    NearbyItem item = group.First();

                    AssignImage((HudPictureBox)row[0], item.Item.Icon);
                    AssignSelected(row, (item.Item.Id == targetId && !expanded), NearbyListColumns);

                    //((HudStaticText)row[1]).Text = $"{group.Key} ({group.Count()})";
                    ((HudStaticText)row[1]).Text = $"{group.Key} ({group.Count()})";
                    ((HudStaticText)row[2]).Text = (expanded ? "[-]" : "[+]");
                    ((HudStaticText)row[3]).Text = item.Item.Id.ToString();
                    ((HudStaticText)row[4]).Text = group.Key;
                }

                // Maybe render items
                if (expanded || !isGrouped)
                {
                    foreach (NearbyItem item in group)
                    {
                        if (index >= NearbyList.RowCount) { row = NearbyList.AddRow(); } else { row = NearbyList[index]; }
                        index++;

                        AssignImage((HudPictureBox)row[0], (isGrouped ? 0 : item.Item.Icon));
                        ((HudStaticText)row[1]).Text = item.Item.Name;

                        if (item.Item.Id == targetId)
                        {
                            AssignSelected(row, true, NearbyListColumns);
                            ((HudStaticText)row[2]).Text = ((int)item.Distance()).ToString();
                        }
                        else
                        {
                            AssignSelected(row, false, NearbyListColumns);
                            ((HudStaticText)row[2]).Text = "";
                        }

                        ((HudStaticText)row[3]).Text = item.Item.Id.ToString();
                        ((HudStaticText)row[4]).Text = "";
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
            ((HudStaticText)row[1]).Text = "";
            ((HudStaticText)row[2]).Text = "";
            ((HudStaticText)row[3]).Text = "";

            return (index + 1);
        }

        private void NearbyList_Click(object sender, int row, int col)
        {
            string group = ((HudStaticText)NearbyList[row][4]).Text;

            string id = ((HudStaticText)NearbyList[row][3]).Text;
            if (id == null || id.Length < 1) { return; }

            DateTime now = DateTime.Now;
            bool doubleClick = (group == LastClickGroup) && ((int)(now - LastClickAt).TotalMilliseconds < 500);

            // Toggle expand / collapse
            if (group.Length > 0 && (col == 2 || doubleClick))
            {
                NearbyListExpanded.TryGetValue(group, out bool expanded);
                NearbyListExpanded[group] = !expanded;
            } else {
                // Otherwise select item
                CoreManager.Current.Actions.SelectItem(int.Parse(id));
            }

            LastClickGroup = group;
            LastClickAt = now;

            UpdateNearbyList();
        }
    }
}
