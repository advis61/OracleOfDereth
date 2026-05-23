using System.Collections.Generic;
using System.Reflection;
using VirindiViewService.Controls;

namespace OracleOfDereth
{
    partial class MainView
    {
        public HudStaticText HelpVersion { get; private set; }
        public HudList HelpList { get; private set; }

        private static readonly List<(string Command, string Description)> HelpCommands = new List<(string, string)>
        {
            ("/od",                       "Show plugin version"),
            ("/od questflag",             "Look up quest info for the selected NPC"),
            ("/od landblock",             "Print the current landblock ID"),
            ("/od logout",                "Log out of the game"),
            ("/od fellow create",         "Create a new fellowship"),
            ("/od fellow open",           "Open the fellowship to recruiting"),
            ("/od fellow close",          "Close the fellowship to recruiting"),
            ("/od fellow quit",           "Leave the current fellowship"),
            ("/od fellow disband",        "Disband the fellowship (leader only)"),
            ("/od fellow recruit <name>", "Recruit a nearby player by name"),
        };

        private void InitHelp()
        {
            HelpVersion = (HudStaticText)view["HelpVersion"];
            HelpList = (HudList)view["HelpList"];
            HelpList.ClearRows();
        }

        public void UpdateHelp()
        {
            HelpVersion.Text = $"Oracle of Dereth v{Assembly.GetExecutingAssembly().GetName().Version}";

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
    }
}
