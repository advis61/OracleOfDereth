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
    public static class ChatBoxMessage
    {
        public static bool Process(string text)
        {
            if (QuestFlag.MyQuestRegex.IsMatch(text))
            {
                return QuestFlag.Add(text);
            }
            return false;
        }
    }
}

