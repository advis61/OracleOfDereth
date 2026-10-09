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
        private static IncomeKills kills;
        private static IncomePetKills petKills;
        private static IncomeConquestCoins conquestCoins;
        private static IncomeLuminance luminance;
        private static IncomeMetric luminanceMetric;
        private static ConquestIncomeLuminance conquestLuminance;
        private static readonly ChatRequest request = new ChatRequest();
        private static bool incomeVisible;
        private static DateTime nextPoll;
        private static WindowsTimer timer;
        private static CoreManager subscribedCore;

        public static void Init()
        {
            Dispose();
            Session = new IncomeSession();
            xp = Session.Register("Experience");
            luminanceMetric = Session.Register("Luminance");
            kills = new IncomeKills(Session.Register("Kills"));
            petKills = new IncomePetKills(Session.Register("Kills (pet)"));
            luminance = new IncomeLuminance(luminanceMetric);
            if (Server.IsConquest)
            {
                conquestLuminance = new ConquestIncomeLuminance(luminanceMetric);
                conquestCoins = new IncomeConquestCoins(Session.Register("Conquest Coins"));
            }
            timer = new WindowsTimer { Interval = 60000 };
            timer.Tick += SampleMinute;
            Reset();
            subscribedCore = CoreManager.Current;
            subscribedCore.MessageProcessed += MessageProcessed;
        }
        public static void Reset()
        {
            timer.Stop();
            Session.Reset(DateTime.Now);
            luminance.ResetBaseline();
            request.Clear();
            nextPoll = DateTime.UtcNow.AddSeconds(10);
            if (conquestLuminance != null)
            {
                conquestLuminance.Reset();
                StartLph();
            }
            Tick();
            timer.Start();
        }
        public static void Tick()
        {
            if (Session != null && Session.Started && CoreManager.Current.CharacterFilter.LoginStatus >= 1)
                xp.ObserveCounter(CoreManager.Current.CharacterFilter.TotalXP);
        }
        public static void UpdateVisibility(bool visible)
        {
            if (conquestLuminance == null) return;
            if (incomeVisible == visible)
            {
                if (visible && DateTime.UtcNow >= nextPoll)
                    PollLph();
                return;
            }
            incomeVisible = visible;
            if (visible) PollLph();
        }
        private static void StartLph()
        {
            if (CoreManager.Current.CharacterFilter.LoginStatus < 1) return;
            request.Sent();
            Util.Command("/lph start");
        }
        public static bool Refresh()
        {
            if (timer == null || CoreManager.Current.CharacterFilter.LoginStatus < 1) return false;
            Tick();
            if (conquestLuminance != null)
            {
                if (!incomeVisible) UpdateVisibility(true);
                else PollLph();
            }
            return true;
        }
        private static void PollLph()
        {
            var now = DateTime.UtcNow;
            nextPoll = now.AddSeconds(10);
            if (!conquestLuminance.Ready) { StartLph(); return; }
            request.Sent();
            Util.Command("/lph");
        }
        private static void SampleMinute(object sender, EventArgs e)
        {
            if (timer == null || !ReferenceEquals(sender, timer)) return;
            try
            {
                if (CoreManager.Current.CharacterFilter.LoginStatus < 1) return;
                Tick();
                if (conquestLuminance == null) Session.SampleMinute();
                else
                {
                    foreach (var metric in Session.Metrics)
                        if (!ReferenceEquals(metric, luminanceMetric)) metric.SampleMinute();
                }
            }
            catch (Exception ex) { Util.Log(ex); }
        }
        public static bool NoteChat(string text)
        {
            if (Session == null || !Session.Started) return false;
            petKills?.NoteChat(text);
            conquestCoins?.NoteChat(text);
            if (conquestLuminance == null) { luminance?.NoteChat(text); return false; }
            bool ours = request.Awaiting;
            bool wasReady = conquestLuminance.Ready;
            if (!conquestLuminance.NoteChat(text, out _)) return false;
            request.Clear();
            if (!wasReady && conquestLuminance.Ready && incomeVisible) PollLph();
            return ours;
        }

        private static void MessageProcessed(object sender, MessageProcessedEventArgs e)
        {
            if (subscribedCore == null || Session == null || !Session.Started || e == null) return;
            try
            {
                var message = e.Message;
                int eventType = message.Type == 0xF7B0 ? message.Value<int>("event") : 0;
                kills.ObserveEvent(message.Type, eventType);
                if (conquestLuminance != null) return;
                // Same Int64 property and initial-character packet used by
                // Virindi Reporter's cXPCounting.Core_MessageProcessed.
                if (message.Type == 0x02CF && message.Value<int>("key") == 6)
                    luminance.ObserveBalance(message.Value<long>("value"));
                else if (message.Type == 0xF7B0 && eventType == 0x13)
                {
                    var properties = message.Struct("properties");
                    if ((properties.Value<int>("flags") & 0x80) == 0) return;
                    int count = properties.Value<short>("qwordCount");
                    var qwords = properties.Struct("qwords");
                    for (int i = 0; i < count; i++)
                    {
                        var property = qwords.Struct(i);
                        if (property.Value<int>("key") != 6) continue;
                        luminance.ObserveBalance(property.Value<long>("value"));
                        break;
                    }
                }
            }
            catch (Exception ex) { Util.Log(ex); }
        }
        public static void Dispose()
        {
            var core = subscribedCore;
            subscribedCore = null;
            if (core != null) core.MessageProcessed -= MessageProcessed;
            luminance = null;
            kills = null;
            petKills = null;
            conquestCoins = null;
            conquestLuminance = null;
            incomeVisible = false;
            request.Clear();
            if (timer == null) return;
            timer.Stop();
            timer.Tick -= SampleMinute;
            timer.Dispose();
            timer = null;
        }
    }
}
