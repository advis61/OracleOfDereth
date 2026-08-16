using System;
using System.Collections.Generic;
using System.Drawing;
using VirindiViewService.Controls;

namespace OracleOfDereth
{
    partial class MainView
    {
        // The plugin icon bar, in the order the icons actually appear. One row per plugin rather
        // than one per icon: a plugin's several views share a group and always move together, so a
        // row per icon would offer moves that can't happen.
        private static readonly List<int> DecalRowColumns = new List<int> { 0 };

        private readonly List<int> decalRowTinted = new List<int>();

        // The picked row, by plugin name rather than index — the list is rebuilt on every move, and
        // the name is what survives that. Same reason the Favorites tab tracks a flag.
        private string decalSelectedPlugin = "";

        public HudStaticText DecalText { get; private set; }

        // Reorder arrows, matching the Favorites tab: picture boxes in a fixed layout.
        public HudFixedLayout DecalUp { get; private set; }
        public HudFixedLayout DecalDown { get; private set; }
        public HudPictureBox DecalUpIcon { get; private set; }
        public HudPictureBox DecalDownIcon { get; private set; }

        public HudList DecalList { get; private set; }

        private void InitDecal()
        {
            DecalText = (HudStaticText)view["DecalText"];
            DecalText.FontHeight = 10;

            DecalUpIcon = new HudPictureBox();
            DecalUpIcon.Image = IconArrowUp;
            DecalUp = (HudFixedLayout)view["DecalUp"];
            DecalUp.AddControl(DecalUpIcon, new Rectangle(0, 0, 16, 16));
            DecalUpIcon.Hit += DecalUp_Hit;

            DecalDownIcon = new HudPictureBox();
            DecalDownIcon.Image = IconArrowDown;
            DecalDown = (HudFixedLayout)view["DecalDown"];
            DecalDown.AddControl(DecalDownIcon, new Rectangle(0, 0, 16, 16));
            DecalDownIcon.Hit += DecalDown_Hit;

            DecalList = (HudList)view["DecalList"];
            DecalList.Click += DecalList_Click;
            DecalList.ClearRows();
        }

        private void DisposeDecal()
        {
            DecalUpIcon.Hit -= DecalUp_Hit;
            DecalDownIcon.Hit -= DecalDown_Hit;
            DecalList.Click -= DecalList_Click;
        }

        public void UpdateDecal()
        {
            // The bar only changes when a plugin registers or drops a view, which is rare — but the
            // tab is cheap to repaint (a couple of dozen rows) and this keeps it honest if another
            // plugin starts up while it's open.
            if (!view.Visible) { return; }

            // A plugin that recreates its view comes back with the group value VVS generates for
            // it, losing the place we gave it — Virindi Window Tool does this. Re-apply when the
            // bar's make-up changes. Scoped to this tab rather than the global tick: it is the only
            // place the order is being looked at, and a hash of the bar's keys every second for the
            // rest of the session buys nothing when nobody is watching.
            VVSBar.ReapplyIfChanged();

            UpdateDecalList();
        }

        private void UpdateDecalList()
        {
            List<string> plugins = VVSBar.PluginNames();

            // Only offer the arrows that can do something. Without the range check the top row
            // still showed an up arrow, which before the fix in VVSBar swapped the plugin with
            // VVS's own theme button.
            int selected = plugins.IndexOf(decalSelectedPlugin);

            DecalUp.Visible = selected > 0;
            DecalDown.Visible = selected >= 0 && selected < plugins.Count - 1;

            for (int x = 0; x < plugins.Count; x++)
            {
                HudList.HudListRowAccessor row;

                if (x >= DecalList.RowCount) {
                    row = DecalList.AddRow();
                } else {
                    row = DecalList[x];
                }

                while (decalRowTinted.Count <= x) { decalRowTinted.Add(TintNone); }

                SetText(row, 0, plugins[x]);

                int tint = x == selected ? TintRowSelected : TintNone;

                if (decalRowTinted[x] != tint)
                {
                    AssignTint(row, TintColor(tint), DecalRowColumns);
                    decalRowTinted[x] = tint;
                }
            }

            while (DecalList.RowCount > plugins.Count)
            {
                DecalList.RemoveRow(DecalList.RowCount - 1);
            }

            if (decalRowTinted.Count > plugins.Count)
            {
                decalRowTinted.RemoveRange(plugins.Count, decalRowTinted.Count - plugins.Count);
            }

            if (plugins.Count == 0) {
                SetText(DecalText, "Plugin bar not available");
            } else {
                SetText(DecalText, $"Plugin bar order: {plugins.Count} plugins");
            }
        }

        // The picked row keeps its selection across a move, so a plugin can be walked several places
        // with repeated clicks. Each move is saved immediately, so the arrangement on screen is
        // always the one that will be restored next login — there is nothing to press to commit.
        private void DecalUp_Hit(object sender, EventArgs e)
        {
            if (VVSBar.MovePlugin(decalSelectedPlugin, -1))
            {
                VVSBar.Save();
                UpdateDecalList();
            }
        }

        private void DecalDown_Hit(object sender, EventArgs e)
        {
            if (VVSBar.MovePlugin(decalSelectedPlugin, 1))
            {
                VVSBar.Save();
                UpdateDecalList();
            }
        }

        private void DecalList_Click(object sender, int row, int col)
        {
            string plugin = ((HudStaticText)DecalList[row][0]).Text;

            decalSelectedPlugin = decalSelectedPlugin == plugin ? "" : plugin;
            UpdateDecalList();
        }
    }
}
