using System;
using System.IO;
using System.Windows.Forms;
using System.Xml;

namespace Fb2GenreSelection
{
    /// <summary>
    /// Чтение и запись путей к внешним редакторам и папкам в DirList.xml.
    /// Файл лежит рядом с exe. Если файла нет — ReadPathXxx вернут null,
    /// WritePathXxx создаст файл с пустой структурой.
    /// </summary>
    public class GenreSelectionConfig
    {
        private const string FileName = "DirList.xml";

        private static readonly string[] AllNodeNames =
        {
            "pathFBE", "pathNP", "pathFSV", "pathBook", "pathCover"
        };

        // Папка, где лежит exe. НЕ Directory.GetCurrentDirectory():
        // рабочая папка может отличаться от папки приложения.
        private static string AppDir => Application.StartupPath;

        private static string ConfigPath => Path.Combine(AppDir, FileName);

        // ================== ЗАПИСЬ ==================

        public void WritePaths(string pathFBE, string pathNP, string pathFSV,
                               string pathBook, string pathCover)
        {
            try
            {
                XmlDocument doc = new();

                if (File.Exists(ConfigPath))
                {
                    doc.Load(ConfigPath);
                }
                else
                {
                    // Создаём скелет файла, чтобы можно было записывать
                    var root = doc.CreateElement("directories");
                    doc.AppendChild(root);

                    foreach (string nodeName in AllNodeNames)
                        root.AppendChild(doc.CreateElement(nodeName));
                }

                SetNodeValue(doc, "pathFBE", pathFBE);
                SetNodeValue(doc, "pathNP", pathNP);
                SetNodeValue(doc, "pathFSV", pathFSV);
                SetNodeValue(doc, "pathBook", pathBook);
                SetNodeValue(doc, "pathCover", pathCover);

                doc.Save(ConfigPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось сохранить настройки: {ex.Message}",
                    "Настройки", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ================== ЧТЕНИЕ ==================

        public string ReadPathFBE() => ReadPath("pathFBE");
        public string ReadPathNP() => ReadPath("pathNP");
        public string ReadPathFSV() => ReadPath("pathFSV");
        public string ReadPathBook() => ReadPath("pathBook");
        public string ReadPathCover() => ReadPath("pathCover");

        // ================== ВНУТРЕННЕЕ ==================

        private static string ReadPath(string nodeName)
        {
            if (!File.Exists(ConfigPath)) return null;

            try
            {
                XmlDocument doc = new();
                doc.Load(ConfigPath);

                XmlNode node = doc.SelectSingleNode($"/directories/{nodeName}");
                return node?.InnerText;
            }
            catch
            {
                return null;
            }
        }

        private static void SetNodeValue(XmlDocument doc, string nodeName, string value)
        {
            XmlNode node = doc.SelectSingleNode($"/directories/{nodeName}");
            if (node != null)
            {
                node.InnerText = value ?? string.Empty;
            }
            else
            {
                // Узел отсутствует — добавляем
                XmlNode root = doc.SelectSingleNode("/directories");
                if (root != null)
                {
                    var newNode = doc.CreateElement(nodeName);
                    newNode.InnerText = value ?? string.Empty;
                    root.AppendChild(newNode);
                }
            }
        }
    }
}