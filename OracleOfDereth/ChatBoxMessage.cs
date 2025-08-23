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
    public static class ChatBoxMessage
    {
        //public static readonly Regex YouCast = new Regex(@"^You cast (.+?) on (.+?)$");
        public static readonly Regex YouCast = new Regex(@"^You cast (.+?) on (.+?)(?:,.*)?$");
        public static readonly Regex YouCast2 = new Regex(@"You cast Incantation of Corruption.*$");

        public static bool Process(string text)
        {
            //Util.Chat($"The text is {text}");

            if (QuestFlag.MyQuestRegex.IsMatch(text))
            {
                return QuestFlag.Add(text);
            }

            if (YouCast.IsMatch(text))
            {
                Match match = YouCast.Match(text);
                string spell = match.Groups[1].Value;
                string target = match.Groups[2].Value;

                Util.Chat($"You cast '{spell}' on '{target}' bro.", 1);
            }

            if (YouCast2.IsMatch(text))
            {
                Util.Chat("YOU CAST 2");
            }


            return false;
        }
    }
}

