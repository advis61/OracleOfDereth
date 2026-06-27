using System.Collections.Generic;

namespace OracleOfDereth
{
    public class Setting
    {
        public string Name { get; set; }
        public string Key { get; set; }
        public List<string> AllowedValues { get; set; }
        public string DefaultValue { get; set; }

        public string Value
        {
            get { return SettingsFile.GetSetting(Key, DefaultValue); }
            set { SettingsFile.PutSetting(Key, value); }
        }

        public void CycleValue()
        {
            int currentIndex = AllowedValues.IndexOf(Value);
            int nextIndex = (currentIndex + 1) % AllowedValues.Count;
            Value = AllowedValues[nextIndex];
        }

        public bool IsYes => Value == "Yes";
        public bool IsNo => Value == "No";

        // --- Static registry of all settings ---

        public static List<Setting> All = new List<Setting>();

        public static Setting AnnouncePlayers;
        public static Setting BuffsRemaining;
        public static Setting CheckForUpdates;
        public static Setting CopyQuestDirections;
        public static Setting CopyQuestUrl;
        public static Setting ShowNearbyWcid;
        public static Setting ShowTradeWindow;
        public static Setting SummonScore;
        public static Setting WeaponScore;
        public static Setting WikiSource;

        public static void Init()
        {
            All.Clear();

            List<string> YesNo = new List<string> { "Yes", "No" };

            AnnouncePlayers = Register("Announce Nearby Players", "AnnouncePlayers", YesNo, "Yes");
            CheckForUpdates = Register("Check For Updates On Login", "CheckForUpdates", YesNo, "Yes");
            BuffsRemaining = Register("Show Remaining Buff Time", "BuffsRemaining", YesNo, "Yes");
            CopyQuestUrl = Register("Copy Quest URL to Clipboard", "CopyQuestUrl", YesNo, "Yes");
            CopyQuestDirections = Register("Copy Quest Directions to Clipboard", "CopyQuestDirections", YesNo, "Yes");
            SummonScore = Register("Show Summons Score", "SummonScore", YesNo, "Yes");
            ShowNearbyWcid = Register("Show WCID on Nearby Tab", "NearbyWcid", YesNo, "No");
            WeaponScore = Register("Show Weapons Score", "WeaponScore", YesNo, "Yes");
            ShowTradeWindow = Register("Use Trade Window", "TradeWindow", YesNo, "Yes");
            WikiSource = Register("Wiki Source", "WikiSource", new List<string> { "Levistras", "ACPedia", "Fandom" }, "Levistras");
        }

        private static Setting Register(string name, string key, List<string> allowedValues, string defaultValue)
        {
            var setting = new Setting
            {
                Name = name,
                Key = key,
                AllowedValues = allowedValues,
                DefaultValue = defaultValue
            };
            All.Add(setting);
            return setting;
        }
    }
}
