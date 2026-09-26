using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Linq;

namespace Fb2GenreSelection
{
    public class GenreButtonConfig
    {
        public string GenreCode { get; set; } = string.Empty;
        public string GenreName { get; set; } = string.Empty;
        public string ButtonText { get; set; } = string.Empty;
    }

    public class QuickButtonsManager
    {
        private readonly List<Button> _quickButtons;
        private readonly ContextMenuStrip _contextMenu;
        private readonly Action<string, string> _onGenreSelected;
        private readonly string _xmlConfigPath = Path.Combine(Application.UserAppDataPath, "user_buttons.xml");

        // Единый экземпляр ToolTip против наслоения подсказок
        private readonly ToolTip _buttonToolTip = new ToolTip();

        private Button _activeCustomButton;

        public QuickButtonsManager(
            IEnumerable<Button> quickButtons,
            ContextMenuStrip contextMenu,
            Action<string, string> onGenreSelected)
        {
            _quickButtons = quickButtons.ToList();
            _contextMenu = contextMenu;
            _onGenreSelected = onGenreSelected;

            _contextMenu.Opening += ContextMenu_Opening;
        }

        public void Initialize()
        {
            foreach (var btn in _quickButtons)
            {
                btn.Click += QuickGenreButton_Click;
                btn.ContextMenuStrip = _contextMenu;
            }

            LoadUserButtonPreferences();
        }

        private void QuickGenreButton_Click(object sender, EventArgs e)
        {
            if (sender is Button btn && btn.Tag is GenreButtonConfig config)
            {
                _onGenreSelected?.Invoke(config.GenreCode, config.GenreName);
            }
        }

        private void ContextMenu_Opening(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _activeCustomButton = _contextMenu.SourceControl as Button;

            if (_activeCustomButton == null || !_quickButtons.Contains(_activeCustomButton))
            {
                e.Cancel = true;
                return;
            }

            BuildGenreContextMenu();
        }

        private void BuildGenreContextMenu()
        {
            _contextMenu.Items.Clear();

            var mainMenuItem = new ToolStripMenuItem("Назначить жанр...");

            foreach (var groupKvp in GenreData.genreMap)
            {
                var groupSubMenu = new ToolStripMenuItem(groupKvp.Key);

                foreach (var genreTuple in groupKvp.Value)
                {
                    var item = new ToolStripMenuItem(genreTuple.Name)
                    {
                        Tag = genreTuple // Передает (Name, Code, Abrev)
                    };
                    item.Click += ContextGenreItem_Click;

                    groupSubMenu.DropDownItems.Add(item);
                }

                mainMenuItem.DropDownItems.Add(groupSubMenu);
            }

            _contextMenu.Items.Add(mainMenuItem);
        }

        private void ContextGenreItem_Click(object sender, EventArgs e)
        {
            if (_activeCustomButton == null) return;

            var menuItem = sender as ToolStripMenuItem;
            if (menuItem == null) return;

            // Извлекаем кортеж из 3 элементов (Name, Code, Abrev)
            if (menuItem.Tag is ValueTuple<string, string, string> tuple)
            {
                string genreName = tuple.Item1;
                string genreCode = tuple.Item2;
                string buttonText = tuple.Item3; // Берем готовую аббревиатуру из XML!

                var config = new GenreButtonConfig
                {
                    GenreCode = genreCode,
                    GenreName = genreName,
                    ButtonText = buttonText
                };

                _activeCustomButton.Tag = config;
                _activeCustomButton.Text = buttonText;

                // Обновляем подсказку через единый экземпляр ToolTip
                _buttonToolTip.SetToolTip(_activeCustomButton, genreName);

                SaveUserButtonPreferences();
            }
        }

        private void SaveUserButtonPreferences()
        {
            try
            {
                var root = new XElement("Buttons");

                foreach (var btn in _quickButtons)
                {
                    if (btn.Tag is GenreButtonConfig cfg)
                    {
                        var buttonElement = new XElement("Button",
                            new XAttribute("Name", btn.Name),
                            new XElement("GenreCode", cfg.GenreCode),
                            new XElement("GenreName", cfg.GenreName),
                            new XElement("ButtonText", cfg.ButtonText)
                        );

                        root.Add(buttonElement);
                    }
                }

                var doc = new XDocument(new XDeclaration("1.0", "utf-8", "yes"), root);
                doc.Save(_xmlConfigPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения кнопок: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadUserButtonPreferences()
        {
            try
            {
                if (!File.Exists(_xmlConfigPath)) return;

                var doc = XDocument.Load(_xmlConfigPath);
                var root = doc.Root;
                if (root == null) return;

                foreach (var btnElement in root.Elements("Button"))
                {
                    string btnName = btnElement.Attribute("Name")?.Value;
                    if (string.IsNullOrEmpty(btnName)) continue;

                    var btn = _quickButtons.FirstOrDefault(b => b.Name == btnName);
                    if (btn != null)
                    {
                        var cfg = new GenreButtonConfig
                        {
                            GenreCode = btnElement.Element("GenreCode")?.Value ?? string.Empty,
                            GenreName = btnElement.Element("GenreName")?.Value ?? string.Empty,
                            ButtonText = btnElement.Element("ButtonText")?.Value ?? string.Empty
                        };

                        btn.Tag = cfg;
                        btn.Text = cfg.ButtonText;

                        // Перезаписываем подсказку при загрузке сохраненных настроек
                        _buttonToolTip.SetToolTip(btn, cfg.GenreName);
                    }
                }
            }
            catch
            {
                // Игнорируем ошибки при отсутствии файла
            }
        }
    }
}