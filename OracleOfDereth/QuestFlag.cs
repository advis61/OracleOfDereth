using Decal.Adapter;
using Decal.Adapter.Wrappers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OracleOfDereth
{
    public class QuestFlag
    {
        public static readonly Regex MyQuestRegex = new Regex(@"(?<key>\S+) \- (?<solves>\d+) solves \((?<completedOn>\d{0,11})\)""?((?<description>.*)"" (?<maxSolves>.*) (?<repeatTime>\d{0,11}))?.*$");
        public static readonly Regex MyQuestsCooldownRegex = new Regex(@"^\[MyQuests\] This command may only be run once every");
        public static readonly Regex KillTaskRegex = new Regex(@"(killtask|killcount|slayerquest|totalgolem.*dead|(kills$))");

        // Quest Flags I care to track
        private static readonly List<string> QuestFlagsToTrack = new List<string> 
            { "legendaryquestsa", "legendaryquestsb", "legendaryquestsc", "explorationmarkersfound" }
            .Concat(Augmentation.Augmentations.Select(q => q.Flag))
            .Concat(AugQuest.AugQuests.Select(q => q.Flag))
            .Concat(CreditQuest.CreditQuests.Select(q => q.Flag))
            .Concat(FacilityQuest.FacilityQuests.Select(q => q.Flag))
            .Concat(FlagQuest.FlagQuests.Select(q => q.Flag))
            .Concat(FlagQuest.FlagQuests.Select(q => q.Flag2))
            .Concat(JohnQuest.JohnQuests.Select(q => q.Flag))
            .Concat(Marker.Markers.Select(q => q.Flag))
            .ToList();

        // Collection of Quest Flags data objects
        public static Dictionary<string, QuestFlag> QuestFlags = new Dictionary<string, QuestFlag>();

        // MyQuests tracking
        public static bool QuestsChanged = false;
        public static bool MyQuestsRan = false;

        // For servers with a 60 second timeout
        private static bool HasCooldown = false;
        private static DateTime LastRefresh = DateTime.MinValue;
        private static readonly TimeSpan RefreshCooldown = TimeSpan.FromSeconds(61);

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


        public static void SetCooldown()
        {
            HasCooldown = true;
            LastRefresh = DateTime.UtcNow;
            Util.Log("MyQuests is now on cooldown YO");
        }

        public static void Refresh(bool force = false)
        {
            if (HasCooldown && !force && DateTime.UtcNow - LastRefresh < RefreshCooldown)
            {
                return;
            }

            Init();
            LastRefresh = DateTime.UtcNow;
            Util.Command("/myquests");
        }

        public static bool Add(string line)
        {
            MyQuestsRan = true;
            QuestsChanged = true;

            QuestFlag questFlag = FromMyQuestsLine(line);
            if (questFlag == null) { return false; }

            // Store this quest flag in the QuestFlags dictionary
            if (QuestFlagsToTrack.Contains(questFlag.Key))
            {
                QuestFlags[questFlag.Key] = questFlag;
                //Util.Chat($"Now tracking #{questFlag.ToString()}.#{QuestFlags.Count()} quests tracked total", 1);
            }

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

