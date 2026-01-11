using AcClient;
using Decal.Adapter;
using Decal.Adapter.Wrappers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
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
            catch { }
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
        public static readonly int ColorYellow = 4; // 3 as well
        public static readonly int ColorPink = 5;
        public static readonly int ColorRed = 6;
        public static readonly int ColorBlue = 7;
        public static readonly int ColorPeach = 8;
        public static readonly int ColorCyan = 13;
        public static readonly int ColorLoot = 14; // Same as Magtools
        public static readonly int ColorOrange = 18;

        public static void Chat(string message, int color = 1, string prefix = "[Oracle of Dereth] ")
        {
            CoreManager.Current.Actions.AddChatText(prefix + message, color);
        }

        public static void Command(string message)
        {
            CoreManager.Current.Actions.InvokeChatParser(message);
        }

        public static void Think(string message)
        {
            CoreManager.Current.Actions.InvokeChatParser(string.Format("/tell {0}, {1}", CoreManager.Current.CharacterFilter.Name, message));
        }

        public static void ClipboardCopy(string message)
        {
            try
            {
                System.Windows.Forms.Clipboard.SetText(message);
                Chat("Copied URL to clipboard.", Util.ColorPink);
            }
            catch (Exception ex)
            {
                Chat("Failed to copy URL to clipboard: " + ex.Message, Util.ColorPink);
            }
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

            if (output.Length == 0) return "0s";

            return output.Trim();
        }
        public static double GetDistance(WorldObject obj1, WorldObject obj2)
        {
            if (obj1.Id == 0) throw new ArgumentOutOfRangeException("obj1", "Object passed with an Id of 0");
            if (obj2.Id == 0) throw new ArgumentOutOfRangeException("obj2", "Object passed with an Id of 0");

            return CoreManager.Current.WorldFilter.Distance(obj1.Id, obj2.Id) * 240;
        }

        public static double GetDistanceFromPlayer(WorldObject destObj)
        {
            if (CoreManager.Current.CharacterFilter.Id == 0) throw new ArgumentOutOfRangeException("destObj", "CharacterFilter.Id of 0");
            if (destObj.Id == 0) throw new ArgumentOutOfRangeException("destObj", "Object passed with an Id of 0");

            return CoreManager.Current.WorldFilter.Distance(CoreManager.Current.CharacterFilter.Id, destObj.Id) * 240;
        }

        public static WorldObject GetClosestObject(ObjectClass objectClass)
        {
            WorldObject closest = null;

            foreach (WorldObject obj in CoreManager.Current.WorldFilter.GetLandscape())
            {
                if (obj.ObjectClass != objectClass) continue;
                if (closest == null || GetDistanceFromPlayer(obj) < GetDistanceFromPlayer(closest)) closest = obj;
            }

            return closest;
        }

        public static WorldObject GetClosestObject(string objectName, bool partialMatch = false)
        {
            WorldObject closest = null;

            foreach (WorldObject obj in CoreManager.Current.WorldFilter.GetLandscape())
            {
                if (!partialMatch && String.Compare(obj.Name, objectName, StringComparison.OrdinalIgnoreCase) != 0) continue;
                if (partialMatch && !obj.Name.ToLower().Contains(objectName.ToLower())) continue;

                if (closest == null || GetDistanceFromPlayer(obj) < GetDistanceFromPlayer(closest)) closest = obj;
            }

            return closest;
        }

        public unsafe static string CurrentLandblock()
        {
            var p = CoreManager.Current.Actions.Underlying.GetPhysicsObjectPtr(CoreManager.Current.CharacterFilter.Id);
            int landblock = *(int*)(p + 0x4C);

            return $"0x{landblock:X8}";
        }

        public unsafe static string ReadPStringFromBuffer(PStringBase<char> pstr)
        {
            if (pstr.m_buffer == null) return null;

            byte* raw = (byte*)pstr.m_buffer;

            // Based on your dump, real string starts at offset 16 (plus 4 fixes this)
            byte* strPtr = raw + 16 + 4;

            // Compute length until null terminator
            int len = 0;
            while (strPtr[len] != 0) len++;

            return new string((sbyte*)strPtr, 0, len);
        }
    }
}
