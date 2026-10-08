using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace OracleOfDereth
{
    public static class ArenaBoard
    {
        public class Player
        {
            public int Rank;
            public string Name;
            // Points, matches, wins, draws, losses, in display order.
            public readonly string[] Metrics = new string[5];
            public bool Complete => Metrics.All(value => value != null);
        }

        private static readonly Regex Header = new Regex(@"^\s*(?:\[[^\]]*\][\s:]*)*\*+\s*Top Ten 1v1 Players\s*\*+\s*$");
        private static readonly Regex Entry = new Regex(@"^\s*(?:\[[^\]]*\][\s:]*)*Rank #(\d+)\s*-\s*(.+?)\s*$");
        private static readonly Regex Metric = new Regex(@"^\s*(?:\[[^\]]*\][\s:]*)*(Rank Points|Total Matches|Total Wins|Total Draws|Total Losses):\s*([\d,]+)\s*$");
        private static readonly Regex Footer = new Regex(@"^\s*(?:\[[^\]]*\][\s:]*)*\*{3,}\s*$");
        private static readonly string[] MetricNames = { "Rank Points", "Total Matches", "Total Wins", "Total Draws", "Total Losses" };
        private static readonly List<Player> Players = new List<Player>();
        private static Player Current;
        private static DateTime LastRefresh = DateTime.MinValue;
        private static DateTime Opened = DateTime.MinValue;
        private static bool Requested;
        private static bool Cleared;
        private static bool Active => DateTime.UtcNow - Opened < TimeSpan.FromSeconds(15);

        public static List<Player> Sorted() => Players.Where(p => p.Complete).OrderBy(p => p.Rank).ToList();

        public static void Init()
        {
            Players.Clear();
            Current = null;
            LastRefresh = Opened = DateTime.MinValue;
            Requested = Cleared = false;
        }

        public static bool RefreshIfStale() => DateTime.UtcNow - LastRefresh >= TimeSpan.FromMinutes(5) && Refresh();

        public static bool Refresh()
        {
            if (!Server.IsConquest || Active) return false;
            LastRefresh = Opened = DateTime.UtcNow;
            Requested = true;
            Cleared = false;
            Current = null;
            Util.Command("/arena rank 1v1");
            return true;
        }

        public static bool Matches(string text)
        {
            if (text == null || !Server.IsConquest) return false;
            return text.Split('\n').Any(line => Header.IsMatch(line) ||
                (Active && (Entry.IsMatch(line) || Metric.IsMatch(line) || Footer.IsMatch(line))));
        }

        // Accept both a whole response and a response split into individual chat lines.
        // Only plugin-requested output is eligible for the existing chat suppression setting.
        public static bool NoteChat(string text)
        {
            if (text == null) return false;
            bool ours = true;
            bool matched = false;
            foreach (string line in text.Split('\n'))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (Header.IsMatch(line))
                {
                    if (!Active)
                    {
                        Opened = DateTime.UtcNow;
                        Requested = false;
                        Cleared = false;
                        Current = null;
                    }
                    ClearOnce();
                }
                else if (!Active) { ours = false; continue; }
                else if (Entry.Match(line) is Match entry && entry.Success)
                {
                    if (!int.TryParse(entry.Groups[1].Value, out int rank)) { ours = false; continue; }
                    ClearOnce();
                    Current = new Player { Rank = rank, Name = entry.Groups[2].Value };
                    Players.RemoveAll(p => p.Rank == rank);
                    Players.Add(Current);
                }
                else if (Metric.Match(line) is Match metric && metric.Success)
                {
                    if (Current != null)
                        Current.Metrics[Array.IndexOf(MetricNames, metric.Groups[1].Value)] = metric.Groups[2].Value;
                }
                else if (Footer.IsMatch(line))
                {
                    ours &= Requested;
                    matched = true;
                    Opened = DateTime.MinValue;
                    Current = null;
                    continue;
                }
                else { ours = false; continue; }
                matched = true;
                ours &= Requested;
            }
            return matched && ours;
        }

        private static void ClearOnce()
        {
            if (Cleared) return;
            Players.Clear();
            Cleared = true;
        }
    }
}
