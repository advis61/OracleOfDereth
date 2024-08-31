using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using VirindiViewService.Controls;

namespace OracleOfDereth
{
    class MainView : IDisposable
    {
        readonly VirindiViewService.ViewProperties properties;
        readonly VirindiViewService.ControlGroup controls;
        readonly VirindiViewService.HudView view;

        public HudStaticText SummoningLabel { get; private set; }
        public HudStaticText VersionLabel { get; private set; }

        public MainView()
        {
            try
            {
                // Create the view
                VirindiViewService.XMLParsers.Decal3XMLParser parser = new VirindiViewService.XMLParsers.Decal3XMLParser();
                parser.ParseFromResource("OracleOfDereth.mainView.xml", out properties, out controls);

                // Display the view
                view = new VirindiViewService.HudView(properties, controls);
                if (view == null) { return; }

                // Assign the views objects to our local variables
                SummoningLabel = (HudStaticText)view["SummoningLabel"];
                VersionLabel = (HudStaticText)view["VersionLabel"];

                Init();
            }
            catch (Exception ex) { Debug.Log(ex); }
        }

        private void Init()
        {
            VersionLabel.Text = $"{DateTime.Now:HH:mm:ss}";
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing) view?.Dispose();
        }
    }
}
