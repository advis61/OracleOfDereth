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
        public static readonly Regex MyQuestRegex = new Regex(@"^\s*(?:\[[^\]]*\]\s*)?(?<key>\S+) \- (?<solves>\d+) solves \((?<completedOn>\d{0,11})\)""?((?<description>.*)"" (?<maxSolves>.*) (?<repeatTime>\d{0,11}))?.*$");
        public static readonly Regex KillTaskRegex = new Regex(@"(killtask|killcount|slayerquest|totalgolem.*dead|(kills$))");

        // Collection of Quest Flags data objects — every flag /myquests reports, unfiltered.
        // There used to be a whitelist built from the quest CSVs, but the Flags tab wants the
        // whole picture, and a character's flag list is small enough to just keep in full.
        public static Dictionary<string, QuestFlag> QuestFlags = new Dictionary<string, QuestFlag>();

        // MyQuests tracking
        public static bool QuestsChanged = false;
        public static bool MyQuestsRan = false;

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

            QuestsChanged = false;
            MyQuestsRan = false;
        }

        public static void Refresh() {
            Init();

            MyQuestsRan = true;
            Util.Command("/myquests");
        }

        public static bool Add(string line)
        {
            MyQuestsRan = true;
            QuestsChanged = true;

            QuestFlag questFlag = FromMyQuestsLine(line);
            if (questFlag == null) { return false; }

            // Store this quest flag in the QuestFlags dictionary
            QuestFlags[questFlag.Key] = questFlag;

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
                    questFlag.Description = match.Groups["description"].Value;

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

