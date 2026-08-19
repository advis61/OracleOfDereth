using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace OracleOfDereth
{
    // Permanent per-server account quest history from /myqstlist, supplemented by stamp messages.
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
            @"^\s*(?:\[[^\]]*\][\s:]*)*(?<flag>[A-Za-z0-9_]+)\s*$");
        private static readonly Regex StoredFlagRegex = new Regex(@"^[A-Za-z0-9_]+$");

        private static readonly TimeSpan CollectWindow = TimeSpan.FromMinutes(2);

        private static string filePath;
        private static DateTime collectingAt;
        private static int expected;
        private static bool collecting;
        private static bool dirty;
        private static bool headerSeen;
        private static bool footerSeen;
        private static readonly HashSet<string> Collected =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public static int Count => Flags.Count;
        public static bool Contains(string flag) =>
            !string.IsNullOrEmpty(flag) && Flags.Contains(flag);
        public static int CompletedCount()
        {
            int count = Flags.Count;
            foreach (string flag in QuestFlag.QuestFlags.Keys)
            {
                if (!Flags.Contains(flag)) count++;
            }
            return count;
        }

        public static void Init()
        {
            Flags.Clear();
            EndBlock();

            string root = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Personal),
                @"Decal Plugins\Oracle of Dereth\quest-history");
            string server = SafeName(Server.Name).ToLowerInvariant();
            filePath = Path.Combine(root, server + ".csv");

            try
            {
                if (!File.Exists(filePath)) return;

                bool removedInvalid = false;
                foreach (string line in File.ReadAllLines(filePath).Skip(1))
                {
                    string flag = line.Trim();
                    if (!StoredFlagRegex.IsMatch(flag))
                    {
                        removedInvalid = true;
                        continue;
                    }

                    if (Flags.Add(flag)) QuestCatalog.AddHistorical(flag);
                }

                if (removedInvalid) Save();
            }
            catch (Exception ex) { Util.Log(ex); }
        }

        public static void Refresh()
        {
            Util.Command("/myqstlist");
        }

        // Called when the player types /myqstlist. Their output remains visible.
        public static void ManualRefresh()
        {
            OpenBlock();
        }

        public static bool Matches(string text)
        {
            if (text == null) return false;
            if (HeaderRegex.IsMatch(text) || FooterRegex.IsMatch(text)) return true;
            return Active() && FlagRegex.IsMatch(text);
        }

        // Returns whether this line belongs to a refresh issued by the plugin.
        public static bool NoteChat(string text)
        {
            Match header = HeaderRegex.Match(text);
            if (header.Success)
            {
                if (!Active()) OpenBlock();
                if (int.TryParse(header.Groups[1].Value, out int count)) expected = count;
                headerSeen = true;
                CloseIfComplete();
                return false;
            }

            if (FooterRegex.IsMatch(text))
            {
                footerSeen = true;
                CloseIfComplete();
                return false;
            }

            if (!Active()) return false;

            Match entry = FlagRegex.Match(text);
            if (!entry.Success) return false;

            string flag = entry.Groups["flag"].Value;
            if (Collected.Add(flag) && Flags.Add(flag))
            {
                dirty = true;
                QuestCatalog.AddHistorical(flag);
                QuestState.HistoryChanged();
            }

            CloseIfComplete();
            return false;
        }

        public static void AddStamp(string flag)
        {
            if (string.IsNullOrWhiteSpace(flag) || !Flags.Add(flag.Trim())) return;

            QuestCatalog.AddHistorical(flag.Trim());
            Save();
        }

        private static void OpenBlock()
        {
            collecting = true;
            collectingAt = DateTime.UtcNow;
            expected = int.MaxValue;
            dirty = false;
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
            dirty = false;
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
            try
            {
                string directory = Path.GetDirectoryName(filePath);
                Directory.CreateDirectory(directory);
                File.WriteAllLines(
                    filePath,
                    new[] { "Flag" }.Concat(Flags.OrderBy(f => f, StringComparer.OrdinalIgnoreCase)));
                dirty = false;
            }
            catch (Exception ex) { Util.Log(ex); }
        }

        private static string SafeName(string value)
        {
            string safe = value ?? "";
            foreach (char c in Path.GetInvalidFileNameChars()) safe = safe.Replace(c, '_');
            return safe.Length > 0 ? safe : "Unknown";
        }
    }
}
