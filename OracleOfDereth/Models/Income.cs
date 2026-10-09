using System;
using Decal.Adapter;
using WindowsTimer = System.Windows.Forms.Timer;

namespace OracleOfDereth
{
    // Owns data collection independently of whether the Income tab is visible.
    public static class Income
    {
        public static IncomeSession Session { get; private set; }
        private static IncomeMetric xp;
        private static WindowsTimer timer;

        public static void Init()
        {
            Dispose();
            Session = new IncomeSession();
            xp = Session.Register("Experience");
            timer = new WindowsTimer { Interval = 60000 };
            timer.Tick += SampleMinute;
            Reset();
        }
        public static void Reset()
        {
            timer.Stop();
            Session.Reset(DateTime.Now);
            Tick();
            timer.Start();
        }
        public static void Tick()
        {
            if (Session != null && Session.Started && CoreManager.Current.CharacterFilter.LoginStatus >= 1)
                xp.ObserveCounter(CoreManager.Current.CharacterFilter.TotalXP);
        }
        private static void SampleMinute(object sender, EventArgs e)
        {
            if (timer == null || !ReferenceEquals(sender, timer)) return;
            try
            {
                if (CoreManager.Current.CharacterFilter.LoginStatus < 1) return;
                Tick();
                Session.SampleMinute();
            }
            catch (Exception ex) { Util.Log(ex); }
        }
        public static void Dispose()
        {
            if (timer == null) return;
            timer.Stop();
            timer.Tick -= SampleMinute;
            timer.Dispose();
            timer = null;
        }
    }
}
