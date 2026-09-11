using System.Collections.Generic;
using VirindiViewService.Controls;

namespace OracleOfDereth
{
    partial class MainView
    {
        public HudList SettingsList { get; private set; }

        private void InitSettings()
        {
            SettingsList = (HudList)view["SettingsList"];
            SettingsList.Click += SettingsList_Click;
            SettingsList.ClearRows();
        }

        private void DisposeSettings()
        {
            SettingsList.Click -= SettingsList_Click;
        }

        public void UpdateSettings()
        {
            UpdateSettingsList();
        }

        private void UpdateSettingsList()
        {
            List<Setting> settings = Setting.All;

            for (int x = 0; x < settings.Count; x++)
            {
                HudList.HudListRowAccessor row;

                if (x >= SettingsList.RowCount)
                {
                    row = SettingsList.AddRow();
                }
                else
                {
                    row = SettingsList[x];
                }

                Setting setting = settings[x];
                SetText(row, 0, setting.Name);
                SetText(row, 1, setting.Value);
            }

            while (SettingsList.RowCount > settings.Count)
            {
                SettingsList.RemoveRow(SettingsList.RowCount - 1);
            }
        }

        private void SettingsList_Click(object sender, int row, int col)
        {
            if (row < 0 || row >= Setting.All.Count) return;

            Setting setting = Setting.All[row];
            setting.CycleValue();
            if (setting == Setting.SetVGITrackAllItems) VGInventoryTracking.Init();
            if (setting == Setting.ShowWeaponWorkmanship || setting == Setting.ShowArmorWorkmanship)
            {
                ItemList.RefreshAll();
                vgInventorySearchDue = System.DateTime.UtcNow;
            }
            UpdateSettingsList();

            Util.Chat($"{setting.Name}: {setting.Value}", Util.ColorPink);
        }
    }
}
