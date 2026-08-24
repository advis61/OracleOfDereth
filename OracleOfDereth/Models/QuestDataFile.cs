using System;
using System.Collections.Generic;
using System.IO;

namespace OracleOfDereth
{
    // Shared safe-write helpers for the catalog and favorites CSVs. Quest flag captures are
    // session-only and never come through this class.
    internal static class QuestDataFile
    {
        public static string ServerPath(string suffix)
        {
            string root = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Personal),
                @"Decal Plugins\Oracle of Dereth\quest-history");
            string server = SafeName(Server.Name).ToLowerInvariant();
            return Path.Combine(root, server + suffix + ".csv");
        }

        public static void Recover(string path)
        {
            string backup = path + ".bak";
            if (!File.Exists(path) && File.Exists(backup)) File.Move(backup, path);
        }

        public static void Write(string path, IEnumerable<string> lines)
        {
            string directory = Path.GetDirectoryName(path);
            string temp = Path.Combine(directory, $"quest-data-{Guid.NewGuid():N}.tmp");
            string backup = path + ".bak";

            Directory.CreateDirectory(directory);
            try
            {
                File.WriteAllLines(temp, lines);
                if (File.Exists(path))
                {
                    if (File.Exists(backup)) File.Delete(backup);
                    File.Move(path, backup);
                }
                File.Move(temp, path);
                if (File.Exists(backup)) File.Delete(backup);
            }
            catch
            {
                if (!File.Exists(path) && File.Exists(backup)) File.Move(backup, path);
                throw;
            }
            finally
            {
                if (File.Exists(temp)) File.Delete(temp);
            }
        }

        private static string SafeName(string value)
        {
            string safe = value ?? "";
            foreach (char c in Path.GetInvalidFileNameChars()) safe = safe.Replace(c, '_');
            return safe.Length > 0 ? safe : "Unknown";
        }
    }
}
