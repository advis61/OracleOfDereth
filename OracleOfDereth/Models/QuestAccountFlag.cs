using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace OracleOfDereth
{
    // The account flags returned by /myqstlist for this session. Kept separate from the
    // character-specific /myquests collection in QuestFlag; neither collection is persisted.
    public static class QuestAccountFlag
    {
        private static readonly HashSet<string> Flags =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        private static readonly Regex HeaderRegex = new Regex(
            Util.ChatPrefixPattern + @"-{4}\s*Account Quests\s*\((\d+)[^)]*\).*?-{4}\s*$",
            RegexOptions.IgnoreCase);
        private static readonly Regex FooterRegex = new Regex(
            Util.ChatPrefixPattern + @"-{4}\s*End of Account Quests\s*-{4}\s*$",
            RegexOptions.IgnoreCase);
        private static readonly Regex FlagRegex = new Regex(
            Util.ChatPrefixPattern + @"(?<flag>\S+)\s*$",
            RegexOptions.IgnoreCase);
        private static readonly Regex NumberedFlagRegex = new Regex(
            Util.ChatPrefixPattern + @"\d+\.\s+(?<flag>\S+)(?:\s+\([^)]*\))?\s*$",
            RegexOptions.IgnoreCase);

        private static readonly TimeSpan CollectWindow = TimeSpan.FromMinutes(2);

        private static DateTime collectingAt;
        private static int expected;
        private static bool collecting;
        private static bool headerSeen;
        private static bool footerSeen;
        private static bool accountRefreshQueued;
        private static readonly ChatRequest Request = new ChatRequest();
        private static readonly HashSet<string> BeforeHeader =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        public static bool HasRequestedRefresh { get; private set; }
        public static int Count => Flags.Count;
        public static bool SuppressChat => Request.Awaiting;
        public static bool Contains(string flag) =>
            !string.IsNullOrEmpty(flag) && Flags.Contains(flag);

        public static bool Add(string flag)
        {
            if (!IsValidFlag(flag)) return false;

            flag = flag.ToLowerInvariant();
            if (!Flags.Add(flag)) return false;

            QuestCatalog.AddHistorical(flag);
            QuestState.HistoryChanged();
            return true;
        }

        public static void Init()
        {
            Flags.Clear();
            EndBlock();
            accountRefreshQueued = false;
            Request.Clear();
            HasRequestedRefresh = false;
        }

        public static void Refresh()
        {
            HasRequestedRefresh = true;
            if (QuestState.RefreshStatus == QuestRefreshStatus.Loading)
            {
                accountRefreshQueued = true;
                return;
            }

            StartAccountRefresh();
        }

        // Called when the player types /myqstlist. Their output remains visible.
        public static void ManualRefresh()
        {
            HasRequestedRefresh = true;
            accountRefreshQueued = false;
            Request.Clear();
            OpenBlock();
        }

        public static bool Capture(string text)
        {
            if (text == null) return false;

            Match header = HeaderRegex.Match(text);
            if (header.Success)
            {
                if (!Active()) OpenBlock();
                if (!headerSeen)
                {
                    Flags.Clear();
                    foreach (string bufferedFlag in BeforeHeader)
                    {
                        Flags.Add(bufferedFlag);
                        QuestCatalog.AddHistorical(bufferedFlag);
                    }
                    QuestCatalog.RemoveUnobservedDiscoveries();
                    QuestState.HistoryChanged();
                }
                if (int.TryParse(header.Groups[1].Value, out int count)) expected = count;
                headerSeen = true;
                Report($"Account quest refresh started: expecting {expected} flags.", Util.ColorPink);
                CloseIfComplete();
                return true;
            }

            if (FooterRegex.IsMatch(text))
            {
                footerSeen = true;
                if (headerSeen && Flags.Count < expected)
                    Report($"Account quest refresh incomplete: captured {Flags.Count} of {expected} flags. Please retry.", Util.ColorRed);
                CloseIfComplete();
                return true;
            }

            if (!Active()) return false;

            Match entry = NumberedFlagRegex.Match(text);
            if (!entry.Success) entry = FlagRegex.Match(text);
            if (!entry.Success) return false;

            string flag = entry.Groups["flag"].Value;
            if (!IsValidFlag(flag)) return false;
            flag = flag.ToLowerInvariant();
            if (headerSeen)
            {
                if (Flags.Add(flag)) QuestCatalog.AddHistorical(flag);
            }
            else BeforeHeader.Add(flag);

            CloseIfComplete();
            return true;
        }

        public static void Tick()
        {
            if (accountRefreshQueued && QuestState.RefreshStatus != QuestRefreshStatus.Loading)
            {
                accountRefreshQueued = false;
                StartAccountRefresh();
            }

        }

        private static void OpenBlock()
        {
            collecting = true;
            collectingAt = DateTime.UtcNow;
            expected = int.MaxValue;
            headerSeen = false;
            footerSeen = false;
            BeforeHeader.Clear();
        }

        private static void StartAccountRefresh()
        {
            OpenBlock();
            Request.Sent();
            Util.Command("/myqstlist");
        }

        private static bool Active()
        {
            if (!collecting) return false;
            if (DateTime.UtcNow - collectingAt < CollectWindow) return true;

            EndBlock();
            return false;
        }

        private static void CloseIfComplete()
        {
            if (!headerSeen || !footerSeen || Flags.Count < expected) return;

            QuestCatalog.RemoveUnobservedDiscoveries();
            QuestState.HistoryChanged();
            Report($"Account quests refreshed: captured {Flags.Count} of {expected} flags.", Util.ColorPink);
            EndBlock();
        }

        private static void EndBlock()
        {
            collecting = false;
            expected = int.MaxValue;
            headerSeen = false;
            footerSeen = false;
            BeforeHeader.Clear();
        }

        private static bool IsValidFlag(string flag)
        {
            return !string.IsNullOrWhiteSpace(flag) &&
                !flag.Any(char.IsWhiteSpace) &&
                !Regex.IsMatch(flag, @"^-+$");
        }

        private static void Report(string message, int color)
        {
            if (Decal.Adapter.CoreManager.Current?.Actions != null) Util.Chat(message, color);
        }
    }
}
