using Decal.Adapter;
using Decal.Adapter.Wrappers;
using System;
using System.Collections.Generic;
using System.Linq;
using VirindiViewService.Controls;

namespace OracleOfDereth
{
    partial class MainView
    {
        public HudList FellowshipList { get; private set; }
        public HudCheckBox FellowshipAutoRecruit { get; private set; }

        public HudButton FellowshipCreate { get; private set; }
        public HudButton FellowshipLeader { get; private set; }
        public HudButton FellowshipQuit { get; private set; }
        public HudButton FellowshipOpen { get; private set; }
        public HudButton FellowshipClose { get; private set; }
        public HudButton FellowshipRecruit { get; private set; }
        public HudButton FellowshipDismiss { get; private set; }
        public HudButton FellowshipDisband { get; private set; }

        public HudStaticText FellowshipName { get; private set; }
        public HudStaticText FellowsName { get; private set; }

        public HudList FellowsList { get; private set; }

        private void InitFellowship()
        {
            FellowshipList = (HudList)view["FellowshipList"];
            FellowshipList.ClearRows();

            FellowshipName = (HudStaticText)view["FellowshipName"];
            FellowsName = (HudStaticText)view["FellowsName"];

            FellowshipAutoRecruit = (HudCheckBox)view["FellowshipAutoRecruit"];
            FellowshipAutoRecruit.Change += FellowshipAutoRecruit_Change;

            FellowshipCreate = (HudButton)view["FellowshipCreate"];
            FellowshipCreate.Hit += FellowshipCreate_Hit;
            FellowshipCreate.Visible = true;

            FellowshipLeader = (HudButton)view["FellowshipLeader"];
            FellowshipLeader.Hit += FellowshipLeader_Hit;
            FellowshipLeader.Visible = false;

            FellowshipQuit = (HudButton)view["FellowshipQuit"];
            FellowshipQuit.Hit += FellowshipQuit_Hit;
            FellowshipQuit.Visible = false;

            FellowshipOpen = (HudButton)view["FellowshipOpen"];
            FellowshipOpen.Hit += FellowshipOpen_Hit;
            FellowshipOpen.Visible = false;

            FellowshipClose = (HudButton)view["FellowshipClose"];
            FellowshipClose.Hit += FellowshipClose_Hit;
            FellowshipClose.Visible = false;

            FellowshipRecruit = (HudButton)view["FellowshipRecruit"];
            FellowshipRecruit.Hit += FellowshipRecruit_Hit;
            FellowshipRecruit.Visible = false;

            FellowshipDismiss = (HudButton)view["FellowshipDismiss"];
            FellowshipDismiss.Hit += FellowshipDismiss_Hit;
            FellowshipDismiss.Visible = false;

            FellowshipDisband = (HudButton)view["FellowshipDisband"];
            FellowshipDisband.Hit += FellowshipDisband_Hit;
            FellowshipDisband.Visible = false;

            FellowsList = (HudList)view["FellowsList"];
            FellowsList.Click += FellowsList_Click;
            FellowsList.ClearRows();
        }

        private void DisposeFellowship()
        {
            FellowsList.Click -= FellowsList_Click;
            FellowshipAutoRecruit.Change -= FellowshipAutoRecruit_Change;

            FellowshipCreate.Hit -= FellowshipCreate_Hit;
            FellowshipDisband.Hit -= FellowshipDisband_Hit;
            FellowshipDismiss.Hit -= FellowshipDismiss_Hit;
            FellowshipRecruit.Hit -= FellowshipRecruit_Hit;
            FellowshipOpen.Hit -= FellowshipOpen_Hit;
            FellowshipClose.Hit -= FellowshipClose_Hit;
            FellowshipQuit.Hit -= FellowshipQuit_Hit;
            FellowshipLeader.Hit -= FellowshipLeader_Hit;
        }

        public void UpdateFellowship()
        {
            UpdateFellowshipNames();
            UpdateFellowshipList();
            UpdateFellowshipButtons();
            UpdateFellowsList();
        }

        private void UpdateFellowshipList()
        {
            List<KeyValuePair<string, string>> items = Fellowship.Status();

            for (int x = 0; x < items.Count(); x++)
            {
                HudList.HudListRowAccessor row;
                if (x >= FellowshipList.RowCount) { row = FellowshipList.AddRow(); } else { row = FellowshipList[x]; }

                // Update
                ((HudStaticText)row[0]).Text = items[x].Key;
                ((HudStaticText)row[1]).Text = items[x].Value;
            }

            while (FellowshipList.RowCount > items.Count()) { FellowshipList.RemoveRow(FellowshipList.RowCount - 1); }
        }
        private void FellowshipAutoRecruit_Change(object sender, EventArgs e)
        {
            Fellowship.AutoRecruitEnabled = FellowshipAutoRecruit.Checked;
            UpdateFellowship();

            if (Fellowship.AutoRecruitEnabled) Fellowship.RecruitAll();
        }

        private void UpdateFellowshipNames()
        {
            bool isInFellowship = Fellowship.IsInFellowship();

            FellowshipAutoRecruit.Visible = isInFellowship;
            FellowsName.Visible = isInFellowship;

            FellowshipName.Text = (isInFellowship ? Fellowship.Name() : "No Fellowship");
            FellowsName.Text = (isInFellowship ? $"Fellows ({Fellowship.FellowCount()})" : "");
        }

        private void UpdateFellowshipButtons()
        {
            bool isInFellowship = Fellowship.IsInFellowship();

            FellowshipCreate.Visible = !isInFellowship;
            FellowshipLeader.Visible = isInFellowship;
            FellowshipQuit.Visible = isInFellowship;
            FellowshipOpen.Visible = isInFellowship && !Fellowship.IsOpen();
            FellowshipClose.Visible = isInFellowship && Fellowship.IsOpen();
            FellowshipRecruit.Visible = isInFellowship;
            FellowshipDismiss.Visible = isInFellowship;
            FellowshipDisband.Visible = isInFellowship;

            FellowshipLeader.Image = (FellowshipLeaderEnabled()) ? null : ImageDisabled;
            FellowshipOpen.Image = (FellowshipOpenEnabled()) ? null : ImageDisabled;
            FellowshipClose.Image = (FellowshipCloseEnabled()) ? null : ImageDisabled;
            FellowshipRecruit.Image = (FellowshipRecruitEnabled()) ? null : ImageDisabled;
            FellowshipDismiss.Image = (FellowshipDismissEnabled()) ? null : ImageDisabled;
            FellowshipDisband.Image = (FellowshipDisbandEnabled()) ? null : ImageDisabled;
        }


        private bool FellowshipOpenEnabled() { return Fellowship.IsLeader() && !Fellowship.IsOpen(); }
        private bool FellowshipCloseEnabled() { return Fellowship.IsLeader() && Fellowship.IsOpen() && !Fellowship.AutoRecruitEnabled; }
        private bool FellowshipDisbandEnabled() { return Fellowship.IsLeader(); }

        private bool FellowshipRecruitEnabled()
        {
            WorldObject target = Target.GetCurrent().Item();
            if (target == null || target.ObjectClass != ObjectClass.Player) { return false; }

            if(Fellowship.IsInFellowship(target.Id)) { return false; }

            return Fellowship.CanRecruit();
        }

        private bool FellowshipLeaderEnabled()
        {
            if (Fellowship.IsLeader() == false) { return false; }

            int id = Fellowship.SelectedFellowId();
            if(id == 0) { return false; }

            return !Fellowship.IsLeader(id);
        }

        private bool FellowshipDismissEnabled()
        {
            if(Fellowship.IsLeader() == false) { return false; }

            int id = Fellowship.SelectedFellowId();
            if(id == 0) { return false; }

            return !Fellowship.IsLeader(id);
        }
        private void UpdateFellowsList()
        {
            List<KeyValuePair<int, string>> items = Fellowship.Fellows().OrderBy(f => f.Value).ToList();
            int selectedFellowId = Fellowship.SelectedFellowId();
            List<int> columns = new List<int> { 0, 1 };

            for (int x = 0; x < items.Count(); x++)
            {
                HudList.HudListRowAccessor row;
                if (x >= FellowsList.RowCount) { row = FellowsList.AddRow(); } else { row = FellowsList[x]; }

                int fellowId = items[x].Key;
                bool selected = (fellowId == selectedFellowId && fellowId != 0);
                AssignSelected(row, selected, columns);

                ((HudStaticText)row[0]).Text = items[x].Value;

                if (items[x].Key == 0) {
                    ((HudStaticText)row[1]).Text = "";
                } else {
                    ((HudStaticText)row[1]).Text = CoreManager.Current.WorldFilter[fellowId] == null ? "Out of Range" : "";
                }

                ((HudStaticText)row[2]).Text = fellowId.ToString();
            }

            while (FellowsList.RowCount > items.Count()) { FellowsList.RemoveRow(FellowsList.RowCount - 1); }
        }
        private void FellowsList_Click(object sender, int row, int col)
        {
            string text = ((HudStaticText)FellowsList[row][2]).Text;

            if(text == null || text == "") {
                Fellowship.SelectFellow(0);
                UpdateFellowsList();
                return;
            }

            int id = int.Parse(text);

            Fellowship.SelectFellow(id);
            CoreManager.Current.Actions.SelectItem(id);

            UpdateFellowsList();
        }

        private void FellowshipCreate_Hit(object sender, EventArgs e)
        {
            Fellowship.Create();
            UpdateFellowship();
        }

        private void FellowshipDisband_Hit(object sender, EventArgs e)
        {
            if(FellowshipDisbandEnabled() == false) { return; }
            Fellowship.Disband();
            UpdateFellowship();
        }

        private void FellowshipLeader_Hit(object sender, EventArgs e)
        {
            if (FellowshipLeaderEnabled() == false) { return; }

            int id = Fellowship.SelectedFellowId();
            if(id != 0) { Fellowship.Leader(id); }

            UpdateFellowship();
        }

        private void FellowshipDismiss_Hit(object sender, EventArgs e)
        {
            if(FellowshipDismissEnabled() == false) { return; }

            int id = Fellowship.SelectedFellowId();
            if(id != 0) { Fellowship.Dismiss(id); }

            UpdateFellowship();
        }

        private void FellowshipRecruit_Hit(object sender, EventArgs e)
        {
            if(FellowshipRecruitEnabled() == false) { return; }

            Fellowship.Recruit(Target.GetCurrent().Item().Id);
            UpdateFellowship();
        }

        private void FellowshipOpen_Hit(object sender, EventArgs e)
        {
            if(FellowshipOpenEnabled() == false) { return; }
            Fellowship.Open();
            UpdateFellowship();
        }

        private void FellowshipClose_Hit(object sender, EventArgs e)
        {
            if(FellowshipCloseEnabled() == false) { return; }
            Fellowship.Close();
            UpdateFellowship();
        }

        private void FellowshipQuit_Hit(object sender, EventArgs e)
        {
            Fellowship.Quit();
            UpdateFellowship();
        }
    }
}
