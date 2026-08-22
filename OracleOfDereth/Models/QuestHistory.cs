using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace OracleOfDereth
{
    // Permanent per-server union of flags observed through /myquests, /myqstlist, or stamps.
    public static class QuestHistory
    {
        private static readonly HashSet<string> Flags =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        private static readonly Regex HeaderRegex = new Regex(
            @"^\s*(?:\[[^\]]*\][\s:]*)*-{4}\s*Account Quests\s*\((\d+)\).*?-{4}\s*$",
            RegexOptions.IgnoreCase);
        private static readonly Regex FooterRegex = new Regex(
            @"^\s*(?:\[[^\]]*\][\s:]*)*-{4}\s*End of Account Quests\s*-{4}\s*$",
            RegexOptions.IgnoreCase);
        private static readonly Regex FlagRegex = new Regex(
            @"^\s*(?:\[[^\]]*\][\s:]*)*(?<flag>\S+)\s*$");
        private static readonly Regex NumberedFlagRegex = new Regex(
            @"^\s*(?:\[[^\]]*\][\s:]*)*\d+\.\s+(?<flag>\S+)(?:\s+\([^)]*\))?\s*$");

        private static readonly TimeSpan CollectWindow = TimeSpan.FromMinutes(2);

        private static string filePath;
        private static DateTime collectingAt;
        private static DateTime changedAt;
        private static int expected;
        private static bool collecting;
        private static bool dirty;
        private static bool headerSeen;
        private static bool footerSeen;
        private static bool accountRefreshRequested;
        private static readonly HashSet<string> Collected =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public static int Count => Flags.Count;
        public static bool Contains(string flag) =>
            !string.IsNullOrEmpty(flag) && Flags.Contains(flag);
        public static void Init()
        {
            Flags.Clear();
            EndBlock();
            dirty = false;
            changedAt = DateTime.MinValue;
            accountRefreshRequested = false;

            string root = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Personal),
                @"Decal Plugins\Oracle of Dereth\quest-history");
            string server = SafeName(Server.Name).ToLowerInvariant();
            filePath = Path.Combine(root, server + ".csv");

            try
            {
                string backupPath = filePath + ".bak";
                if (!File.Exists(filePath) && File.Exists(backupPath)) File.Move(backupPath, filePath);
                accountRefreshRequested = File.Exists(filePath);
                if (!File.Exists(filePath)) return;

                string[] lines = File.ReadAllLines(filePath);
                bool rewrite = lines.Length > 0 && Util.CsvParseLine(lines[0]).Length > 1;
                foreach (string line in lines.Skip(1))
                {
                    string flag = Util.CsvParseLine(line).FirstOrDefault()?.Trim() ?? "";
                    if (!IsValidFlag(flag))
                    {
                        rewrite = true;
                        continue;
                    }

                    if (Flags.Add(flag)) QuestCatalog.AddHistorical(flag);
                }

                if (rewrite) Save();
            }
            catch (Exception ex) { Util.Log(ex); }
        }

        public static void Refresh()
        {
            accountRefreshRequested = true;
            Util.Command("/myqstlist");
        }

        public static bool RefreshIfMissing()
        {
            if (accountRefreshRequested || File.Exists(filePath)) return false;

            Refresh();
            return true;
        }

        // Called when the player types /myqstlist. Their output remains visible.
        public static void ManualRefresh()
        {
            accountRefreshRequested = true;
            OpenBlock();
        }

        public static bool Capture(string text)
        {
            if (text == null) return false;

            Match header = HeaderRegex.Match(text);
            if (header.Success)
            {
                if (!Active()) OpenBlock();
                if (int.TryParse(header.Groups[1].Value, out int count)) expected = count;
                headerSeen = true;
                CloseIfComplete();
                return true;
            }

            if (FooterRegex.IsMatch(text))
            {
                footerSeen = true;
                CloseIfComplete();
                return true;
            }

            if (!Active()) return false;

            Match entry = NumberedFlagRegex.Match(text);
            if (!entry.Success) entry = FlagRegex.Match(text);
            if (!entry.Success) return false;

            string flag = entry.Groups["flag"].Value;
            if (!IsValidFlag(flag)) return false;
            if (Collected.Add(flag) && Add(flag))
            {
                QuestState.HistoryChanged();
            }

            CloseIfComplete();
            return true;
        }

        public static void AddStamp(string flag)
        {
            Add(flag);
            SaveIfDirty();
        }

        // /myquests can contain thousands of lines, so merge in memory and save once it settles.
        public static void AddSeen(string flag)
        {
            Add(flag);
        }

        public static void Tick()
        {
            if (dirty && DateTime.UtcNow - changedAt >= TimeSpan.FromSeconds(2)) Save();
        }

        private static void OpenBlock()
        {
            collecting = true;
            collectingAt = DateTime.UtcNow;
            expected = int.MaxValue;
            headerSeen = false;
            footerSeen = false;
            Collected.Clear();
        }

        private static bool Active()
        {
            if (!collecting) return false;
            if (DateTime.UtcNow - collectingAt < CollectWindow) return true;

            SaveIfDirty();
            EndBlock();
            return false;
        }

        private static void CloseIfComplete()
        {
            if (!headerSeen || !footerSeen || Collected.Count < expected) return;

            SaveIfDirty();
            EndBlock();
        }

        private static void EndBlock()
        {
            collecting = false;
            expected = int.MaxValue;
            headerSeen = false;
            footerSeen = false;
            Collected.Clear();
        }

        private static void SaveIfDirty()
        {
            if (dirty) Save();
        }

        private static void Save()
        {
            string tempPath = null;
            string backupPath = null;
            try
            {
                string directory = Path.GetDirectoryName(filePath);
                Directory.CreateDirectory(directory);
                tempPath = Path.Combine(directory, $"quest-history-{Guid.NewGuid():N}.tmp");
                File.WriteAllLines(
                    tempPath,
                    new[] { "Flag" }.Concat(
                        Flags.OrderBy(f => f, StringComparer.OrdinalIgnoreCase).Select(Util.CsvEscape)));
                if (File.Exists(filePath))
                {
                    backupPath = filePath + ".bak";
                    if (File.Exists(backupPath)) File.Delete(backupPath);
                    File.Move(filePath, backupPath);
                }
                File.Move(tempPath, filePath);
                dirty = false;
            }
            catch (Exception ex)
            {
                try
                {
                    if (!File.Exists(filePath) && backupPath != null && File.Exists(backupPath))
                        File.Move(backupPath, filePath);
                }
                catch (Exception restoreException) { Util.Log(restoreException); }
                Util.Log(ex);
            }
            finally
            {
                try
                {
                    if (tempPath != null && File.Exists(tempPath)) File.Delete(tempPath);
                    if (!dirty && backupPath != null && File.Exists(backupPath)) File.Delete(backupPath);
                }
                catch (Exception ex) { Util.Log(ex); }
            }
        }

        private static bool Add(string flag)
        {
            string value = (flag ?? "").Trim();
            if (!IsValidFlag(value) || !Flags.Add(value)) return false;

            QuestCatalog.AddHistorical(value);
            dirty = true;
            changedAt = DateTime.UtcNow;
            return true;
        }

        private static bool IsValidFlag(string flag)
        {
            return !string.IsNullOrWhiteSpace(flag) &&
                !flag.Any(char.IsWhiteSpace) &&
                !Regex.IsMatch(flag, @"^-+$");
        }

        private static string SafeName(string value)
        {
            string safe = value ?? "";
            foreach (char c in Path.GetInvalidFileNameChars()) safe = safe.Replace(c, '_');
            return safe.Length > 0 ? safe : "Unknown";
        }
    }
}
