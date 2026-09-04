using System;
using System.Collections.Generic;
using System.Reflection;
using VirindiViewService.Controls;

namespace OracleOfDereth
{
    partial class MainView
    {
        public HudStaticText HelpVersion { get; private set; }
        public HudList HelpList { get; private set; }
        public HudList HelpStatusList { get; private set; }

        private static readonly List<(string Command, string Description)> HelpCommands = new List<(string, string)>
        {
            ("/od",                       "Show plugin version"),
            ("/od fellow create",         "Create a new fellowship"),
            ("/od fellow open",           "Open the fellowship to recruiting"),
            ("/od fellow close",          "Close the fellowship to recruiting"),
            ("/od fellow quit",           "Leave the current fellowship"),
            ("/od fellow disband",        "Disband the fellowship (leader only)"),
            ("/od fellow recruit <name>", "Recruit a nearby player by name"),
            ("/od landblock",             "Print the current landblock ID"),
            ("/od logout",                "Log out of the game"),
            ("/od questflag",             "Look up quest info for the selected NPC"),
            ("/od quests update",         "Download and reload the latest quest list"),
            ("/od quests reset",          "Delete the downloaded list and reload the bundled list"),
            ("/od quests send",           "Send your new quest flags to Oracle of Dereth"),
            ("/od quests send clear",     "Forget which quest flags you've sent"),
            ("/od update",                "Checks for available updates"),
        };

        private void InitHelp()
        {
            HelpVersion = (HudStaticText)view["HelpVersion"];
            HelpList = (HudList)view["HelpList"];
            HelpList.ClearRows();
            HelpStatusList = (HudList)view["HelpStatusList"];
            HelpStatusList.ClearRows();
        }

        public void UpdateHelp()
        {
            HelpVersion.Text = $"Oracle of Dereth v{Assembly.GetExecutingAssembly().GetName().Version}";

            string source = QuestCatalog.UsingBundledList ? "Bundled quests.csv" : "Downloaded quests.csv";
            string installed = FormatTimestamp(QuestCatalogUpdater.InstalledAt());
            string[,] status =
            {
                { "Master quest list version", QuestCatalogUpdater.Version() },
                { "Installed", installed },
                { "Last checked", QuestCatalogUpdater.LastChecked().Length > 0 ? FormatTimestamp(QuestCatalogUpdater.LastChecked()) : "Never" },
                { "Source", source }
            };

            while (HelpStatusList.RowCount < status.GetLength(0)) HelpStatusList.AddRow();
            for (int i = 0; i < status.GetLength(0); i++)
            {
                ((HudStaticText)HelpStatusList[i][0]).Text = status[i, 0];
                ((HudStaticText)HelpStatusList[i][1]).Text = status[i, 1];
            }

            // Static content; only build the rows once.
            if (HelpList.RowCount == HelpCommands.Count) return;

            HelpList.ClearRows();
            foreach (var cmd in HelpCommands)
            {
                HudList.HudListRowAccessor row = HelpList.AddRow();
                ((HudStaticText)row[0]).Text = cmd.Command;
                ((HudStaticText)row[1]).Text = cmd.Description;
            }
        }

        private static string FormatTimestamp(string value)
        {
            return DateTime.TryParse(value, null, System.Globalization.DateTimeStyles.RoundtripKind, out DateTime timestamp)
                ? timestamp.ToLocalTime().ToString("yyyy-MM-dd HH:mm")
                : "Unknown";
        }
    }
}
