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

        // --- Static registry of all settings ---

        public static List<Setting> All = new List<Setting>();

        public static Setting AutoOpenFellowship;
        public static Setting ShowTradeNotifications;
        public static Setting ShowNearbyMonsters;
        public static Setting ShowNearbyPlayers;
        public static Setting AutoRefreshQuests;

        public static void Init()
        {
            All.Clear();

            AutoOpenFellowship = Register("Auto Open Fellowship", "AutoOpenFellowship", new List<string> { "Yes", "No" }, "No");
            ShowTradeNotifications = Register("Show Trade Notifications", "ShowTradeNotifications", new List<string> { "Yes", "No" }, "Yes");
            ShowNearbyMonsters = Register("Show Nearby Monsters", "ShowNearbyMonsters", new List<string> { "Yes", "No" }, "Yes");
            ShowNearbyPlayers = Register("Show Nearby Players", "ShowNearbyPlayers", new List<string> { "Yes", "No" }, "Yes");
            AutoRefreshQuests = Register("Auto Refresh Quests", "AutoRefreshQuests", new List<string> { "Yes", "No" }, "No");
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
