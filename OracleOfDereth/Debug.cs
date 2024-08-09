using Decal.Adapter;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OracleOfDereth
{
    public static class Debug
    {
        public static void Log(Exception ex)
        {
            Debug.Log(ex.ToString());
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
                CoreManager.Current.Actions.AddChatText(message, 1);
            }
            catch { }
        }
    }
}
