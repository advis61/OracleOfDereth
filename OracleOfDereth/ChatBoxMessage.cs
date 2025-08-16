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
                return QuestFlag.Process(text);
            }
            return false;
        }

        //private static bool ProcessQuestFlag(string text)
        //{
        //    CoreManager.Current.Actions.AddChatText("found it", 1);

        //    QuestFlag questFlag = QuestFlag.Process(text);

        //    // Add questFlag Key and Solves to chat
        //    CoreManager.Current.Actions.AddChatText($"found it #{questFlag}", 1);
        //    CoreManager.Current.Actions.AddChatText($"Key: {questFlag.Key}", 1);
        //    CoreManager.Current.Actions.AddChatText($"Solves: {questFlag.Solves}", 1);
        //    return true;
        //}
    }
}

