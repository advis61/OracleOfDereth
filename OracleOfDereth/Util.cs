using Decal.Adapter;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OracleOfDereth
{
    public static class Util
    {
        public static void Log(Exception ex)
        {
            try
            {
               Util.Chat(ex.ToString(), 1);

                using (StreamWriter writer = new StreamWriter(Environment.GetFolderPath(Environment.SpecialFolder.Personal) + @"\Decal Plugins\Oracle of Dereth" + "\\errors.txt", true))
                {
                    writer.WriteLine("============================================================================");
                    writer.WriteLine(DateTime.Now.ToString());
                    writer.WriteLine("Error: " + ex.Message);
                    writer.WriteLine("Source: " + ex.Source);
                    writer.WriteLine("Stack: " + ex.StackTrace);
                    if (ex.InnerException != null)
                    {
                        writer.WriteLine("Inner: " + ex.InnerException.Message);
                        writer.WriteLine("Inner Stack: " + ex.InnerException.StackTrace);
                    }
                    writer.WriteLine("============================================================================");
                    writer.WriteLine("");
                    writer.Close();
                }
            }
            catch
            {
            }
        }

        /// <summary>
        /// Log a string to log.txt in the same directory as the plugin.
        /// </summary>
        /// <param name="message"></param>
        public static void Log(string message)
        {
            try
            {
                File.AppendAllText(System.IO.Path.Combine(PluginCore.AssemblyDirectory, "log.txt"), $"{message}\n");
               Util.Chat(message, 1);
            }
            catch { }
        }

        public static readonly int ColorGreen = 1;
        public static readonly int ColorWhite = 2;
        public static readonly int ColorYellow = 4;
        public static readonly int ColorPink = 5;
        public static readonly int ColorRed = 6;
        public static readonly int ColorBlue = 7;
        public static readonly int ColorOrange = 18;

        public static void Chat(string message, int color = 1)
        {
            CoreManager.Current.Actions.AddChatText("[Oracle of Dereth] " + message, color);
        }

        public static void Command(string message)
        {
            CoreManager.Current.Actions.InvokeChatParser(message);
        }

        public static void Think(string message)
        {
            CoreManager.Current.Actions.InvokeChatParser(string.Format("/tell {0}, {1}", CoreManager.Current.CharacterFilter.Name, message));
        }

        public static DateTime UnixTimeStampToDateTime(double unixTimeStamp)
        {
            DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddSeconds(unixTimeStamp);
            return dtDateTime;
        }
        public static string GetFriendlyTimeDifference(TimeSpan difference)
        {
            string output = "";

            if (difference.Days > 0) output += difference.Days.ToString() + "d ";
            if (difference.Hours > 0) output += difference.Hours.ToString() + "h ";
            if (difference.Minutes > 0) output += difference.Minutes.ToString() + "m ";
            if (difference.Seconds > 0) output += difference.Seconds.ToString() + "s ";

            if (output.Length == 0)
                return "0s";
            return output.Trim();
        }

    }
}
