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
    public class MyQuests
    {
        public bool Process(string text)
        {
            string command = text.ToLower().Trim();

            CoreManager.Current.Actions.AddChatText("My Quests filter running", 1);

            if (command == "/myquests")
            {
                CoreManager.Current.Actions.AddChatText("My Quests", 1);
                return false;
            }
            return false;
        }
    }
}

