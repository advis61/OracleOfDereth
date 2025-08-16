using Decal.Adapter;
using Decal.Adapter.Wrappers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OracleOfDereth
{
    public static class CommandLineText
    {
        public static bool Process(string text)
        {
            string command = text.ToLower().Trim();

            if (command == "/od" || command == "/ood")
            {
                Version version = Assembly.GetExecutingAssembly().GetName().Version;
                CoreManager.Current.Actions.AddChatText($"Oracle of Dereth v{version}", 1);
                return true;
            }

            if (command == "/od exception")
            {
                CoreManager.Current.Actions.AddChatText($"Oracle of Dereth EXCEPTION", 1);
                throw new InvalidOperationException("An error occurred.");
            }

            if (command == "/myquests")
            {
                CoreManager.Current.Actions.AddChatText("OOD myquests", 1);
                return false;
            }

            return false;
        }
    }
}
