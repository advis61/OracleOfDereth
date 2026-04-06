using System;
using System.Collections.Generic;
using System.Linq;
using VirindiViewService.Controls;

namespace OracleOfDereth
{
    partial class MainView
    {
        public HudStaticText MarkersText { get; private set; }
        public HudButton MarkersRefresh { get; private set; }
        public HudList MarkersList { get; private set; }

        private void InitMarkers()
        {
            MarkersText = (HudStaticText)view["MarkersText"];
            MarkersText.FontHeight = 10;

            MarkersRefresh = (HudButton)view["MarkersRefresh"];
            MarkersRefresh.Hit += QuestFlagsRefresh_Hit;

            MarkersList = (HudList)view["MarkersList"];
            MarkersList.Click += MarkersList_Click;
            MarkersList.ClearRows();
        }

        private void DisposeMarkers()
        {
            MarkersList.Click -= MarkersList_Click;
            MarkersRefresh.Hit -= QuestFlagsRefresh_Hit;
        }

        public void UpdateMarkers()
        {
            if (QuestFlag.MyQuestsRan == false) { QuestFlag.Refresh(); }
            UpdateMarkersList();
        }

        private void UpdateMarkersList()
        {
            List<Marker> markers = Marker.Markers.ToList();
            int completed = 0;

            for (int x = 0; x < markers.Count; x++)
            {
                HudList.HudListRowAccessor row;

                if (x >= MarkersList.RowCount) {
                    row = MarkersList.AddRow();
                    ((HudStaticText)row[1]).TextAlignment = VirindiViewService.WriteTextFormats.Center;
                    ((HudStaticText)row[3]).TextAlignment = VirindiViewService.WriteTextFormats.Right;
                } else {
                    row = MarkersList[x];
                }

                // Update
                Marker marker = markers[x];

                bool complete = marker.IsComplete();
                if (complete) { completed += 1; }

                AssignImage((HudPictureBox)row[0], complete);
                ((HudStaticText)row[1]).Text = marker.Number.ToString();
                ((HudStaticText)row[2]).Text = marker.Name;
                ((HudStaticText)row[3]).Text = marker.Location;
            }

            // Update Text
            MarkersText.Text = $"Exploration Markers: {completed} completed";
        }

        private void MarkersList_Click(object sender, int row, int col)
        {
            int number = int.Parse(((HudStaticText)MarkersList[row][1]).Text.Replace("#", ""));

            Marker marker = Marker.Markers.FirstOrDefault(x => x.Number == number);
            if (marker == null) { return; }

            if(col == 0)
            {
                Util.Think($"#{marker.Number} {marker.Name}: {marker.Url()}");
                Util.ClipboardCopy(marker.Url());
            }

            // Quest Hint
            if (col > 0 && col < 3 && marker.Hint.Length > 0)
            {
                Util.Think($"#{marker.Number} {marker.Name}: {marker.Hint}");
            }

            if(col == 3)
            {
                Util.Chat($"{marker.Flag}: BitMask:{marker.BitMask} Number:{marker.Number} Name:{marker.Name}", Util.ColorPink);
            }
        }
    }
}
