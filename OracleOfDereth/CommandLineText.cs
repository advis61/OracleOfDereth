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
                Util.Chat($"Oracle of Dereth v{version}", 1);
                return true;
            }

            if (command == "/od exception")
            {
                Util.Chat($"Oracle of Dereth EXCEPTION", 1);
                throw new InvalidOperationException("An error occurred.");
            }

            //if (command == "/myquests")
            //{
            //    Util.Chat($"Oracle of Dereth Parsing MyQuests", 1);
            //}

            return false;
        }
    }
}
