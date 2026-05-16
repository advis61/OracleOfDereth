using System;
using System.IO;
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
            _doc = new XmlDocument();

            try
            {
                if (File.Exists(_filePath))
                {
                    _doc.Load(_filePath);
                }
                else
                {
                    _doc.AppendChild(_doc.CreateXmlDeclaration("1.0", "utf-8", null));
                    _doc.AppendChild(_doc.CreateElement("Settings"));
                    Save();
                }
            }
            catch (Exception ex)
            {
                Util.Log(ex);
                _doc = new XmlDocument();
                _doc.AppendChild(_doc.CreateXmlDeclaration("1.0", "utf-8", null));
                _doc.AppendChild(_doc.CreateElement("Settings"));
            }
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
                XmlNode root = _doc.SelectSingleNode("/Settings");
                if (root == null)
                {
                    root = _doc.CreateElement("Settings");
                    _doc.AppendChild(root);
                }

                XmlNode node = root.SelectSingleNode(key);
                if (node == null)
                {
                    node = _doc.CreateElement(key);
                    root.AppendChild(node);
                }

                node.InnerText = value;
                Save();
            }
            catch (Exception ex)
            {
                Util.Log(ex);
            }
        }

        private static void Save()
        {
            try
            {
                string dir = Path.GetDirectoryName(_filePath);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

                _doc.Save(_filePath);
            }
            catch (Exception ex)
            {
                Util.Log(ex);
            }
        }
    }
}
