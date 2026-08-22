using System;

namespace OracleOfDereth
{
    public enum QuestRefreshStatus
    {
        NotRequested,
        Loading,
        Loaded
    }

    // Coordinates live /myquests state with the quest catalog. Views observe Revision; they do
    // not own merging or clear a shared dirty flag.
    public static class QuestState
    {
        private static readonly TimeSpan QuietPeriod = TimeSpan.FromSeconds(2);
        private static DateTime requestedAt;
        private static DateTime lastFlagAt;
        private static bool receivedFlag;
        private static bool refreshCompleted;

        public static int Revision { get; private set; }
        public static bool HasRequestedRefresh { get; private set; }
        public static bool LastChangeWasFlag { get; private set; }

        public static QuestRefreshStatus RefreshStatus
        {
            get
            {
                if (!HasRequestedRefresh) return QuestRefreshStatus.NotRequested;

                DateTime activity = receivedFlag ? lastFlagAt : requestedAt;
                return DateTime.UtcNow - activity < QuietPeriod
                    ? QuestRefreshStatus.Loading
                    : QuestRefreshStatus.Loaded;
            }
        }

        public static void Init()
        {
            Revision = 0;
            HasRequestedRefresh = false;
            requestedAt = DateTime.MinValue;
            lastFlagAt = DateTime.MinValue;
            receivedFlag = false;
            refreshCompleted = false;
            LastChangeWasFlag = false;
        }

        public static void BeginRefresh()
        {
            HasRequestedRefresh = true;
            requestedAt = DateTime.UtcNow;
            lastFlagAt = DateTime.MinValue;
            receivedFlag = false;
            refreshCompleted = false;
            LastChangeWasFlag = false;
            Revision++;
        }

        public static void RefreshFlagReceived()
        {
            HasRequestedRefresh = true;
            lastFlagAt = DateTime.UtcNow;
            receivedFlag = true;
        }

        public static void Tick()
        {
            if (!HasRequestedRefresh || refreshCompleted || RefreshStatus == QuestRefreshStatus.Loading) return;

            QuestFlag.CompleteRefresh(receivedFlag);
            refreshCompleted = true;
            if (receivedFlag) LastChangeWasFlag = true;
            Revision++;
        }

        public static void FlagChanged(QuestFlag flag, bool fromRefresh)
        {
            if (fromRefresh)
            {
                HasRequestedRefresh = true;
                lastFlagAt = DateTime.UtcNow;
                receivedFlag = true;
            }

            QuestCatalog.Add(flag);
            LastChangeWasFlag = true;
            Revision++;
        }

        public static void HistoryChanged()
        {
            LastChangeWasFlag = true;
            Revision++;
        }
    }
}
