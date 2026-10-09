using System;
using System.Globalization;
using System.Collections.Generic;
using VirindiViewService;
using VirindiViewService.Controls;

namespace OracleOfDereth
{
    partial class MainView
    {
        public HudStaticText IncomeSessionText { get; private set; }
        public HudButton IncomeReset { get; private set; }
        public HudButton IncomeRefresh { get; private set; }
        public HudList IncomeList { get; private set; }
        private string selectedIncomeMetric;
        private static readonly List<int> IncomeColumns = new List<int> { 0, 1, 2, 3 };
        private void InitIncome()
        {
            IncomeSessionText = (HudStaticText)view["IncomeSessionText"];
            IncomeSessionText.FontHeight = 10;
            IncomeReset = (HudButton)view["IncomeReset"];
            IncomeReset.Hit += IncomeReset_Hit;
            IncomeRefresh = (HudButton)view["IncomeRefresh"];
            IncomeRefresh.Hit += IncomeRefresh_Hit;
            IncomeList = (HudList)view["IncomeList"];
            IncomeList.Click += IncomeList_Click;
            // Match the XML header bounds exactly instead of adding VVS's default
            // horizontal cell and outer padding to each column.
            IncomeList.WPadding = 0;
            IncomeList.WPaddingOuter = 0;
            foreach (string name in new[] { "IncomeTotalHeader", "IncomeHourHeader", "IncomeFiveMinuteHeader" })
                ((HudStaticText)view[name]).TextAlignment = WriteTextFormats.Right;
        }
        private void DisposeIncome()
        {
            if (IncomeReset != null) IncomeReset.Hit -= IncomeReset_Hit;
            if (IncomeRefresh != null) IncomeRefresh.Hit -= IncomeRefresh_Hit;
            if (IncomeList != null) IncomeList.Click -= IncomeList_Click;
            selectedIncomeMetric = null;
        }
        private void IncomeList_Click(object sender, int row, int col)
        {
            var session = Income.Session;
            if (session == null || !session.Started || row < 0 || row >= IncomeList.RowCount || row >= session.Metrics.Count) return;
            var metric = session.Metrics[row];
            selectedIncomeMetric = metric.Name;
            Income.Tick();
            UpdateIncome();
            Util.Think(metric.Report(session.Elapsed(DateTime.Now)));
        }
        private void IncomeRefresh_Hit(object sender, EventArgs e)
        {
            if (Income.Refresh()) FlashButton(IncomeRefresh);
            UpdateIncome();
        }
        private void IncomeReset_Hit(object sender, EventArgs e)
        {
            Income.Reset();
            UpdateIncome();
        }
        private static string FormatIncome(double? value) => value.HasValue ? value.Value.ToString("N0", CultureInfo.InvariantCulture) : "—";
        private void UpdateIncome()
        {
            var session = Income.Session;
            var elapsed = session.Elapsed(DateTime.Now);
            IncomeSessionText.Text = session.Started
                ? string.Format("Session: {0:00}:{1:00}:{2:00}", (long)elapsed.TotalHours, elapsed.Minutes, elapsed.Seconds)
                : "Session: —";
            for (int i = 0; i < session.Metrics.Count; i++)
            {
                var metric = session.Metrics[i];
                var row = i < IncomeList.RowCount ? IncomeList[i] : IncomeList.AddRow();
                SetText(row, 0, metric.Name);
                for (int column = 1; column < 4; column++)
                    ((HudStaticText)row[column]).TextAlignment = WriteTextFormats.Right;
                SetText(row, 1, session.Started ? metric.Total.ToString("N0", CultureInfo.InvariantCulture) : "—");
                SetText(row, 2, FormatIncome(session.Started ? metric.SessionPerHour(elapsed) : (double?)null));
                SetText(row, 3, FormatIncome(metric.FiveMinutePerHour));
                AssignSelected(row, metric.Name == selectedIncomeMetric, IncomeColumns);
            }
        }
    }
}
