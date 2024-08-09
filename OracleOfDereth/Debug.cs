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
            try
            {
                CoreManager.Current.Actions.AddChatText(ex.ToString(), 1);

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
                CoreManager.Current.Actions.AddChatText(message, 1);
            }
            catch { }
        }
    }
}
