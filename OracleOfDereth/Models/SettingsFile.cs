using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Xml;

namespace OracleOfDereth
{
    public static class SettingsFile
    {
        private static XmlDocument _doc;
        private static string _filePath;

        public static void Init()
        {
            _filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), @"Decal Plugins\Oracle of Dereth\settings.xml");
            Load();
        }

        private static void Load()
        {
            _doc = NewDocument();
            try
            {
                Locked(() =>
                {
                    try { _doc = Read(); }
                    catch (XmlException ex)
                    {
                        Util.Log(ex);
                        File.Delete(_filePath);
                        Save();
                    }
                    if (!File.Exists(_filePath)) Save();
                });
            }
            catch (Exception ex) { Util.Log(ex); }
        }

        private static XmlDocument Read()
        {
            if (!File.Exists(_filePath)) return NewDocument();
            var loaded = new XmlDocument();
            loaded.Load(_filePath);
            if (loaded.DocumentElement == null || loaded.DocumentElement.Name != "Settings")
                throw new XmlException("Settings file has no Settings root element.");
            return loaded;
        }

        private static XmlDocument NewDocument()
        {
            var document = new XmlDocument();
            document.AppendChild(document.CreateXmlDeclaration("1.0", "utf-8", null));
            document.AppendChild(document.CreateElement("Settings"));
            return document;
        }

        public static string GetSetting(string key, string defaultValue)
        {
            try
            {
                XmlNode node = _doc.SelectSingleNode($"/Settings/{key}");
                if (node != null && node.InnerText.Length > 0)
                {
                    return node.InnerText;
                }
            }
            catch (Exception ex)
            {
                Util.Log(ex);
            }

            return defaultValue;
        }

        public static void PutSetting(string key, string value)
        {
            try
            {
                Locked(() =>
                {
                    // Merge this one change into the latest file, never a client's stale copy.
                    _doc = Read();
                    XmlNode root = _doc.DocumentElement;
                    XmlNode node = root.SelectSingleNode(key);
                    if (node == null)
                    {
                        node = _doc.CreateElement(key);
                        root.AppendChild(node);
                    }

                    node.InnerText = value;
                    Save();
                });
            }
            catch (Exception ex)
            {
                Util.Log(ex);
            }
        }

        private static void Locked(Action action)
        {
            string path = Path.GetFullPath(_filePath).ToUpperInvariant();
            using (var hash = SHA256.Create())
            using (var mutex = new Mutex(false, @"Local\OracleOfDereth.Settings." +
                BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(path))).Replace("-", "")))
            {
                bool acquired;
                try { acquired = mutex.WaitOne(1000); }
                catch (AbandonedMutexException) { acquired = true; }
                if (!acquired) throw new IOException("Settings are busy; try again in a moment.");
                try { action(); }
                finally { mutex.ReleaseMutex(); }
            }
        }

        private static void Save()
        {
            try
            {
                string dir = Path.GetDirectoryName(_filePath);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

                string tempPath = Path.Combine(dir, $"settings-{Guid.NewGuid():N}.tmp");
                try
                {
                    _doc.Save(tempPath);
                    if (File.Exists(_filePath)) File.Replace(tempPath, _filePath, null, true);
                    else File.Move(tempPath, _filePath);
                }
                finally
                {
                    if (File.Exists(tempPath)) File.Delete(tempPath);
                }
            }
            catch (Exception ex)
            {
                Util.Log(ex);
                Util.Chat("Oracle of Dereth could not save settings. See errors.txt.", Util.ColorPink);
            }
        }
    }
}
