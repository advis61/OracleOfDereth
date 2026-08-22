using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;

namespace OracleOfDereth
{
    public static class QuestCatalogUpdater
    {
        private const string Url = "https://raw.githubusercontent.com/advis61/OracleOfDereth/master/OracleOfDereth/Resources/quests.csv";
        private const string LastCheckedKey = "LastCheckedQuestList";
        private const string DateFormat = "yyyy-MM-dd";
        private const int TimeoutMs = 15000;
        private static readonly TimeSpan Delay = TimeSpan.FromSeconds(10);

        private static DateTime? armedAt;
        private static bool ran;
        private static volatile bool pendingChecked;
        private static volatile string pendingMessage;
        private static volatile Exception pendingException;

        public static void Init()
        {
            if (Setting.AutoUpdateQuestList.IsYes) Arm();
        }

        public static void Arm()
        {
            if (ran) return;
            if (SettingsFile.GetSetting(LastCheckedKey, "") == DateTime.Today.ToString(DateFormat))
            {
                ran = true;
                return;
            }
            armedAt = DateTime.UtcNow;
        }

        public static void Tick()
        {
            if (pendingException != null) { Util.Log(pendingException); pendingException = null; }
            if (pendingChecked)
            {
                SettingsFile.PutSetting(LastCheckedKey, DateTime.Today.ToString(DateFormat));
                pendingChecked = false;
            }
            if (pendingMessage != null) { Util.Chat(pendingMessage, Util.ColorPink, ""); pendingMessage = null; }

            if (ran || armedAt == null || DateTime.UtcNow - armedAt.Value < Delay) return;
            ran = true;
            new Thread(Run) { IsBackground = true }.Start();
        }

        private static void Run()
        {
            try
            {
                string[] downloaded = Fetch().Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
                if (!QuestCatalog.Validate(downloaded, out string error))
                    throw new InvalidDataException("Downloaded quests.csv is invalid: " + error);

                string[] current = File.Exists(QuestCatalog.FilePath)
                    ? File.ReadAllLines(QuestCatalog.FilePath)
                    : Array.Empty<string>();

                if (!downloaded.SequenceEqual(current, StringComparer.Ordinal))
                {
                    QuestHistory.WriteFile(QuestCatalog.FilePath, downloaded);
                    pendingMessage = "Oracle of Dereth quest list updated. It will be used the next time the plugin loads.";
                }
                pendingChecked = true;
            }
            catch (Exception ex) { pendingException = ex; }
        }

        private static string Fetch()
        {
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
            var request = (HttpWebRequest)WebRequest.Create(Url);
            request.UserAgent = "OracleOfDereth-Plugin";
            request.Timeout = TimeoutMs;
            request.ReadWriteTimeout = TimeoutMs;
            using (var response = request.GetResponse())
            using (var reader = new StreamReader(response.GetResponseStream()))
                return reader.ReadToEnd();
        }
    }
}
