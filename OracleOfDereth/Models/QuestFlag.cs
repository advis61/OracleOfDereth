using Decal.Adapter;
using Decal.Adapter.Wrappers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OracleOfDereth
{
    public class QuestFlag
    {
        // A "/myquests" line, e.g. 'blankquestflag - 3 solves (1719432000)"Some Quest" 5 82800'.
        // Anchored so the flag must lead the line (after an optional chat timestamp), which is only
        // ever true of our own "/myquests" output — the same hardening the Bank / Augs / Fship
        // parsers carry, so a pasted 'Someone says, "somequest - 1 solves (0)"' can't inject a
        // flag. It also matters for speed: unanchored, this was retried at every position of every
        // chat line in the game, and it runs third in PluginCore's chain.
        public static readonly Regex MyQuestRegex = new Regex(Util.ChatPrefixPattern + @"(?<key>\S+) \- (?<solves>\d+) solves \((?<completedOn>\d{0,11})\)\s*""?((?<description>.*)"" (?<maxSolves>.*) (?<repeatTime>\d{0,11}))?.*$", RegexOptions.IgnoreCase);
        public static readonly Regex KillTaskRegex = new Regex(@"(killtask|killcount|slayerquest|totalgolem.*dead|(kills$))");

        // The game's own confirmation that a flag was just set, e.g. "You've stamped moufreward!".
        // Lets the quest tabs mark a flag complete the moment it lands instead of waiting for the
        // next "/myquests", which is a whole-list round trip nobody wants to fire per stamp.
        //
        // Anchored the same way MyQuestRegex is, so 'Bob says, "You've stamped foo!"' can't inject
        // a flag. Both apostrophes are accepted because the client renders a typographic one.
        //
        // This is the server talking to the player, not a reply the plugin asked for, so it is
        // never suppressed — see PluginCore, which reads it and lets it print.
        public static readonly Regex StampedRegex = new Regex(Util.ChatPrefixPattern + @"You['’]ve stamped (?<key>[^\s!]+)(?: on first completion)?!", RegexOptions.IgnoreCase);

        // Collection of Quest Flags data objects — every flag /myquests reports, unfiltered.
        // There used to be a whitelist built from the quest CSVs, but the Flags tab wants the
        // whole picture, and a character's flag list is small enough to just keep in full.
        public static Dictionary<string, QuestFlag> QuestFlags =
            new Dictionary<string, QuestFlag>(StringComparer.OrdinalIgnoreCase);
        private static Dictionary<string, QuestFlag> pendingRefresh;
        private static readonly ChatRequest Request = new ChatRequest();
        public static bool SuppressChat => Request.Awaiting;

        // Properties
        public string Key = "";
        public string Description = "";
        public int Solves = 0;
        public int MaxSolves = 0;
        public DateTime CompletedOn = DateTime.MinValue;
        public TimeSpan RepeatTime = TimeSpan.FromSeconds(0);

        public static void Init()
        {
            QuestFlags.Clear();
            pendingRefresh = null;
            Request.Clear();
            QuestState.Init();
        }

        public static void Refresh() {
            pendingRefresh = new Dictionary<string, QuestFlag>(StringComparer.OrdinalIgnoreCase);
            QuestState.BeginRefresh();
            Request.Sent();
            Util.Command("/myquests");
        }

        public static void ManualRefresh() { Request.Clear(); }

        public static bool Add(string line)
        {
            QuestFlag questFlag = FromMyQuestsLine(line);
            if (questFlag == null) { return false; }

            if (pendingRefresh != null)
            {
                pendingRefresh[questFlag.Key] = questFlag;
                QuestState.RefreshFlagReceived();
            }
            else
            {
                QuestFlags[questFlag.Key] = questFlag;
                QuestState.FlagChanged(questFlag, false);
            }
            return true;
        }

        internal static void CompleteRefresh(bool replace)
        {
            if (pendingRefresh == null) return;

            if (replace)
            {
                QuestFlags = pendingRefresh;
                foreach (QuestFlag flag in QuestFlags.Values) QuestCatalog.Add(flag);
                QuestCatalog.RemoveUnobservedDiscoveries();
            }

            pendingRefresh = null;
        }

        // A stamp is enough evidence for both live collections. Record it immediately without
        // pretending either full-list refresh occurred.
        public static bool Stamped(string line)
        {
            Match match = StampedRegex.Match(line);
            if (!match.Success) { return false; }

            string key = match.Groups["key"].Value.ToLowerInvariant();
            if (!QuestFlags.TryGetValue(key, out QuestFlag questFlag))
            {
                questFlag = new QuestFlag { Key = key };
                QuestFlags[key] = questFlag;
            }

            questFlag.Solves += 1;
            questFlag.CompletedOn = DateTime.UtcNow;
            QuestState.FlagChanged(questFlag, false);
            QuestAccountFlag.Add(key);
            return true;
        }

        // From UtilityBelt QuestTracker.cs
        public static QuestFlag FromMyQuestsLine(string line)
        {
            try
            {
                var questFlag = new QuestFlag();
                Match match = MyQuestRegex.Match(line);

                if (match.Success)
                {
                    questFlag.Key = match.Groups["key"].Value.ToLower();
                    questFlag.Description = match.Groups["description"].Value.Trim().Trim('"').Trim();

                    int.TryParse(match.Groups["solves"].Value, out questFlag.Solves);
                    int.TryParse(match.Groups["maxSolves"].Value, out questFlag.MaxSolves);

                    double completedOn = 0;
                    if (double.TryParse(match.Groups["completedOn"].Value, out completedOn))
                    {
                        questFlag.CompletedOn = Util.UnixTimeStampToDateTime(completedOn);

                        double repeatTime = 0;
                        if (double.TryParse(match.Groups["repeatTime"].Value, out repeatTime))
                        {
                            questFlag.RepeatTime = TimeSpan.FromSeconds(repeatTime);
                        }
                    }

                    return questFlag;
                }
                else
                {
                    Util.Log("Unable to parse myquests line: " + line);
                    return null;
                }
            }
            catch (Exception ex) { Util.Log(ex); }

            return null;
        }

        // instance methods
        public override string ToString()
        {
            return $"{Key}: {Description} CompletedOn:{CompletedOn} Solves:{Solves} MaxSolves:{MaxSolves} RepeatTime:{Util.GetFriendlyTimeDifference(RepeatTime)}";
        }

        public TimeSpan NextAvailableTime()
        {
            return (CompletedOn + RepeatTime) - DateTime.UtcNow;
        }

        public bool Ready()
        {
            var difference = NextAvailableTime();
            return difference.TotalSeconds <= 0;
        }

        public string NextAvailable()
        {
            var difference = NextAvailableTime();

            if (difference.TotalSeconds > 0) {
                return Util.GetFriendlyTimeDifference(difference);
            } else {
                return "ready";
            }
        }
    }
}

