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
        private static int running;
        private static volatile bool pendingSuccess;
        private static volatile string pendingMessage;
        private static volatile Exception pendingException;

        public static void Init()
        {
            if (Setting.AutoUpdateQuestList.IsYes &&
                SettingsFile.GetSetting(LastCheckedKey, "") != DateTime.Today.ToString(DateFormat))
                armedAt = DateTime.UtcNow;
        }

        public static void Tick()
        {
            if (pendingException != null) { Util.Log(pendingException); pendingException = null; }
            if (pendingSuccess)
            {
                pendingSuccess = false;
                SettingsFile.PutSetting(LastCheckedKey, DateTime.Today.ToString(DateFormat));
                QuestCatalog.Reload();
            }
            if (pendingMessage != null) { Util.Chat(pendingMessage, Util.ColorPink, ""); pendingMessage = null; }

            if (armedAt == null || DateTime.UtcNow - armedAt.Value < Delay) return;
            armedAt = null;
            Start(false);
        }

        public static void UpdateNow()
        {
            if (!Start(true))
                Util.Chat("Oracle of Dereth is already checking the quest list.", Util.ColorPink, "");
        }

        public static string LastChecked() => SettingsFile.GetSetting(LastCheckedKey, "");

        private static bool Start(bool verbose)
        {
            if (Interlocked.CompareExchange(ref running, 1, 0) != 0) return false;
            new Thread(() => Run(verbose)) { IsBackground = true }.Start();
            return true;
        }

        private static void Run(bool verbose)
        {
            try
            {
                string[] downloaded = Fetch().Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
                if (!QuestCatalog.Validate(downloaded, out string error))
                    throw new InvalidDataException("Downloaded quests.csv is invalid: " + error);

                string[] current = File.Exists(QuestCatalog.FilePath)
                    ? File.ReadAllLines(QuestCatalog.FilePath)
                    : Array.Empty<string>();

                bool changed = !downloaded.SequenceEqual(current, StringComparer.Ordinal);
                if (changed)
                    QuestHistory.WriteFile(QuestCatalog.FilePath, downloaded);

                pendingMessage = changed
                    ? "Oracle of Dereth quest list updated and reloaded."
                    : verbose ? "Oracle of Dereth quest list reloaded; it was already up to date." : null;
                pendingSuccess = true;
            }
            catch (Exception ex)
            {
                pendingException = ex;
                if (verbose) pendingMessage = "Oracle of Dereth quest list update failed. See errors.txt for details.";
            }
            finally { Interlocked.Exchange(ref running, 0); }
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
