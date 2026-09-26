using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using TitleTools;
using SearchOption = System.IO.SearchOption;

namespace Fb2GenreSelection
{
    public partial class Form1 : Form
    {
        public ListViewItem lvItem;
        public ListViewItem lvItemTitles;
        public string dirFBE;
        public string dirNP;
        public string dirBook;
        public string dirFSV;
        public string dirCover;
        public string nameFile, author, bookName, razmerFile, kodePage, fn;
        public FolderBrowserDialog FBD = new();
        public Dictionary<string, string> GlavaDict = new();
        public Dictionary<string, string> PartsDict = new();
        public Dictionary<string, string> BooksDict = new();
        public Encoding Encod;
        public string strokaTitle = string.Empty;
        public ListView.SelectedListViewItemCollection TitleSelected;
        int Cnt = 0;
        string appDir = Directory.GetCurrentDirectory();
        string strSelectedText;
        bool fileSelected = false;
        private TextBox lastFocusedTextBox;
        GenreSelectionConfig GSC = new();
        private List<BookInfo> _allLoadedBooks = [];
        private readonly Fb2DocumentCache _docCache = new();
        private QuickButtonsManager _quickButtonsManager;

        //Title ttl = new();
        //Fb2ErrorProcessor ErrorProc = new();
        //private readonly Fb2SectionProcessor processor = new();


        public Dictionary<string, List<(string Name, string Code)>> genreMap = [];
        Button buttonGenre = new() { Text = " " };

        public Form1()
        {
            InitializeComponent();
            panel3.Controls.Add(buttonGenre);
            buttonGenre.Location = new Point(651, 8);
            buttonGenre.Width = 25;
            buttonGenre.Height = 25;
            buttonGenre.Image = Properties.Resources.btnMal3;

        }

        private void Form1_Load(object sender, EventArgs e)
        {
            XmlDocument doc = new();
            if (!File.Exists("genres_ru.xml"))
            {
                MessageBox.Show("Файл genres_ru.xml не найден!");
                return;
            }
            doc.Load("genres_ru.xml");
            GenreData.LoadGenres("genres_ru.xml");

            string menuGlavn = "   ";
            ToolStripMenuItem MnuStripItem = new(menuGlavn);
            MnuStrip.Items.Add(MnuStripItem);

            SubMenu(MnuStripItem, " ");
            buttonGenre.Click += (s, e) =>
            {
                // Открываем выпадающее меню программно
                MnuStripItem.ShowDropDown();
            };

            dirBook = GSC.ReadPathBook();
            dirNP = GSC.ReadPathNP();
            dirFBE = GSC.ReadPathFBE();
            dirCover = GSC.ReadPathCover();

            FBD.SelectedPath = Directory.GetCurrentDirectory();
            if (string.IsNullOrEmpty(dirBook))
            {
                TxtBoxPath.Text = FBD.SelectedPath;
            }
            else
            {
                TxtBoxPath.Text = dirBook;
            }

            // ПЕРЕНЕСЕНО: сопоставление колонок DataGridView с полями модели — это разовая
            // настройка UI, а не то, что должно пересчитываться на каждый клик по книге.
            // Раньше вызывалось внутри BindBookToControls (на каждый listView1_SelectedIndexChanged),
            // теперь — один раз здесь, сразу после загрузки формы. Имена колонок в дизайнере
            // (GenreName/GenreCode, FirstName/MiddleName/LastName) точно совпадают с именами
            // свойств GenreItem/AuthorItem, так что сопоставление по имени отработает корректно.
            DGVAuthors.AutoGenerateColumns = false;
            DGVAuthors.ReadOnly = true;
            MapColumnsToPropertiesIfEmpty(DGVAuthors, typeof(AuthorItem));

            DGVGenres.AutoGenerateColumns = false;
            DGVGenres.ReadOnly = true;
            MapColumnsToPropertiesIfEmpty(DGVGenres, typeof(GenreItem));

            NavigateAndLoadDirectory(TxtBoxPath.Text);

            // Перечисляем имена 18 широких кнопок
            var genreButtons = new Button[]
            {
            BtnQGenre1, BtnQGenre2, BtnQGenre3, BtnQGenre4, BtnQGenre5, BtnQGenre6,
            BtnQGenre7, BtnQGenre8, BtnQGenre9, BtnQGenre10, BtnQGenre11, BtnQGenre12,
            BtnQGenre13, BtnQGenre14, BtnQGenre15, BtnQGenre16, BtnQGenre17, BtnQGenre18
            };

            _quickButtonsManager = new QuickButtonsManager(
                genreButtons,
                menuItemSelectGenre,
                (code, name) =>
                {
                    textBoxKodGenre.Text = code;
                    textBoxNameGenre.Text = name;
                }
            );

            _quickButtonsManager.Initialize();

        }

        private void SubMenu(ToolStripMenuItem MnuItems, string var)
        {
            foreach (var group in GenreData.genreMap)
            {
                // создаём пункт меню для группы
                ToolStripMenuItem groupItem = new(group.Key);

                foreach (var genre in group.Value)
                {
                    ToolStripMenuItem genreItem = new(genre.Name);
                    genreItem.Tag = genre.Code; // сохраняем код жанра в Tag

                    // обработчик клика
                    genreItem.Click += (s, e) =>
                    {
                        string kodGenre = (string)((ToolStripMenuItem)s).Tag;
                        string nameGenre = GenreData.GetGenreName(kodGenre);
                        textBoxNameGenre.Text = nameGenre;
                        textBoxKodGenre.Text = kodGenre;
                    };

                    groupItem.DropDownItems.Add(genreItem);
                }

                MnuItems.DropDownItems.Add(groupItem);
            }
        }

        /*Чтобы назначить обработчик событий для кода я включил событие ChildClick в ToolStripMenuItem.
        Код события ChildClick EventHandler:*/

        public void ChildClick(object sender, EventArgs e)
        {
            if (sender is not ToolStripMenuItem clickedItem) return;

            string selectedGenreName = clickedItem.Text;

            var foundGenre = genreMap
                .SelectMany(group => group.Value)
                .FirstOrDefault(g => g.Name == selectedGenreName);

            if (foundGenre != default)
            {
                textBoxNameGenre.Text = foundGenre.Name;
                textBoxKodGenre.Text = foundGenre.Code;
            }
            else
            {
                textBoxNameGenre.Text = selectedGenreName;
                textBoxKodGenre.Text = "(код не найден)";
            }

        }

        private void BtnExit_Click(object sender, EventArgs e)
        {
            Close();
        }

        // Программа закрывается
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            _docCache?.SaveIfDirty();

            int totalBooks = _allLoadedBooks?.Count ?? 0;

            if (totalBooks > 50)
            {
                string message = $"В списке {totalBooks} книг. Сохранить список книг?";
                string caption = "Сохранение списка книг";
                MessageBoxButtons buttons = MessageBoxButtons.YesNo;
                MessageBoxIcon msgBoxIcon = MessageBoxIcon.Question;

                DialogResult result = MessageBox.Show(message, caption, buttons, msgBoxIcon);
                if (result == DialogResult.Yes)
                {
                    BtnSaveFileXML.PerformClick();
                }
            }

            // Удаляем резервные копии
            try
            {
                if (!string.IsNullOrEmpty(TxtBoxPath.Text) && Directory.Exists(TxtBoxPath.Text))
                {
                    string[] fileList = Directory.GetFiles(TxtBoxPath.Text, "*.bak", SearchOption.TopDirectoryOnly);
                    foreach (string f in fileList)
                        File.Delete(f);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка удаления .bak: {ex.Message}");
            }

            // Удаляем картинки обложек
            try
            {
                if (Directory.Exists(dirCover))
                {
                    var picList = Directory.GetFiles(dirCover, "*.*", SearchOption.TopDirectoryOnly);
                    foreach (string f in picList)
                        File.Delete(f);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка удаления картинок: {ex.Message}");
            }
        }

        private void btnOpenDirBig_Click(object sender, EventArgs e)
        {
            if (FBD.ShowDialog() == DialogResult.OK)
            {
                NavigateAndLoadDirectory(FBD.SelectedPath);
            }
        }

        // Асинхронная загрузка FB2-файлов каталога с отображением прогресса.
        // <param name="targetPath">Целевой путь каталога.</param>
        // <param name="selectFolderName">Имя папки, которую нужно выделить после загрузки (при переходе вверх).</param>
        private async void NavigateAndLoadDirectory(string targetPath, string selectFolderName = null)
        {
            if (string.IsNullOrEmpty(targetPath) || !Directory.Exists(targetPath)) return;

            _docCache.SaveIfDirty();
            TxtBoxPath.Text = targetPath;
            _allLoadedBooks.Clear();
            ClearBookControls();

            string[] files;
            try
            {
                files = Directory.GetFiles(targetPath, "*.fb2", SearchOption.TopDirectoryOnly);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка доступа к каталогу: {ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            int totalFiles = files.Length;

            if (totalFiles > 0)
            {
                toolSSLabelFiles.Text = $"Всего файлов: {totalFiles}";
                TextBoxObrabotano.Text = totalFiles.ToString();
                await RunWithProgressAsync(files, f => _allLoadedBooks.Add(BookInfo.ParseFb2(f)));
            }
            else
            {
                toolSSLabelFiles.Text = "Всего файлов: 0";
                TextBoxFinded.Text = "";
                TextBoxObrabotano.Text = "";
            }

            // Перерисовываем ListView
            ApplyComboBoxFilter();

            // Если задано имя папки для выделения — находим её в ListView
            if (!string.IsNullOrEmpty(selectFolderName))
            {
                foreach (ListViewItem item in ListV.Items)
                {
                    if (item.Tag is not BookInfo && item.Text.Equals(selectFolderName, StringComparison.OrdinalIgnoreCase))
                    {
                        item.Selected = true;
                        item.Focused = true;
                        item.EnsureVisible();
                        break;
                    }
                }
            }
        }
        // ================== ПРОГРЕСС ==================

        /// <summary>
        /// Показать прогресс-бар и настроить его на totalFiles шагов.
        /// </summary>
        private void ShowProgress(int totalFiles)
        {
            progressBar1.Maximum = totalFiles;
            progressBar1.Value = 0;
            progressBar1.Step = 1;
            progressBar1.Visible = true;

            labelProcessed.Visible = true;
            labelProcessed.Text = $"Обработано: 0/{totalFiles} 0%";
        }

        /// <summary>
        /// Обновить прогресс-бар на шаге current из total.
        /// </summary>
        private void UpdateProgress(int current, int total)
        {
            progressBar1.Value = current;
            int percent = total > 0 ? (current * 100) / total : 0;
            labelProcessed.Text = $"Обработано: {current}/{total} {percent}%";
            TextBoxFinded.Text = current.ToString();
        }

        /// <summary>
        /// Скрыть прогресс-бар и метку.
        /// </summary>
        private void HideProgress()
        {
            progressBar1.Value = 0;
            progressBar1.Visible = false;
            labelProcessed.Visible = false;
        }

        /// <summary>
        /// Прогоняет список items, выполняя action для каждого элемента,
        /// с обновлением прогресс-бара и возможностью UI перерисоваться.
        /// </summary>
        private async Task RunWithProgressAsync<T>(IList<T> items, Action<T> action)
        {
            if (items == null || items.Count == 0) return;

            int total = items.Count;
            ShowProgress(total);

            for (int i = 0; i < total; i++)
            {
                action(items[i]);

                UpdateProgress(i + 1, total);
                await Task.Delay(1); // даём UI перерисоваться
            }

            HideProgress();
        }

        // Обработчик события смены группы в ComboBox
        private void comboBoxGenres_SelectedIndexChanged(object sender, EventArgs e)
        {
            _docCache.SaveIfDirty();
            ApplyComboBoxFilter();
        }

        // Метод фильтрации текущего массива в памяти
        // Построение элементов ListView (Папки + Фильтрованные книги).
        private void ApplyComboBoxFilter()
        {
            ClearBookControls();

            string currentPath = TxtBoxPath.Text;
            if (string.IsNullOrEmpty(currentPath) || !Directory.Exists(currentPath))
            {
                ListV.Items.Clear();
                return;
            }

            ListV.BeginUpdate();
            try
            {
                ListV.Items.Clear();

                // 1. Формируем список директорий
                List<ListViewItem> folderItems = GetFolderItems(currentPath);
                ListV.Items.AddRange(folderItems.ToArray());

                // 2. Выбираем книги по фильтру
                List<BookInfo> filteredBooks = FilterBooks();

                // 3. Формируем список файлов книг
                List<ListViewItem> bookItems = new List<ListViewItem>(filteredBooks.Count);
                foreach (var book in filteredBooks)
                {
                    ListViewItem item = new ListViewItem(book.FileName)
                    {
                        ImageKey = "fb2",
                        Tag = book
                    };

                    // Добавляем ровно 7 SubItems для 8 колонок таблицы (0: FileName ... 7: Language)
                    item.SubItems.AddRange(
                    [
                        book.Author ?? "",
                        book.Seria ?? "",
                        book.Title ?? "",
                        book.GenreName ?? "",
                        book.Razmer ?? "",
                        book.KodePage ?? "",
                        book.Language ?? ""
                    ]);

                    bookItems.Add(item);
                }

                ListV.Items.AddRange(bookItems.ToArray());
            }
            finally
            {
                ListV.EndUpdate();

                // Считаем только книги (Tag = BookInfo), исключая ".." и папки
                if (TextBoxFinded != null)
                {
                    int bookCount = 0;
                    foreach (ListViewItem it in ListV.Items)
                    {
                        if (it.Tag is BookInfo) bookCount++;
                    }
                    TextBoxFinded.Text = bookCount.ToString();
                }
            }
        }

        // Генерация папок и пункта ".." с привязкой реального пути в Tag.
        private static List<ListViewItem> GetFolderItems(string currentPath)
        {
            List<ListViewItem> items = new();

            // Родительский каталог ".."
            DirectoryInfo parentDir = Directory.GetParent(currentPath);
            if (parentDir != null)
            {
                items.Add(new ListViewItem("..")
                {
                    ImageKey = "Up",
                    Tag = parentDir.FullName
                });
            }

            // Вложенные директории
            try
            {
                var directories = Directory.GetDirectories(currentPath)
                    .Select(d => new DirectoryInfo(d))
                    .Where(di => !di.Attributes.HasFlag(FileAttributes.System) &&
                                 !di.Attributes.HasFlag(FileAttributes.Hidden));

                foreach (DirectoryInfo dirInfo in directories)
                {
                    items.Add(new ListViewItem(dirInfo.Name)
                    {
                        ImageKey = "Folder",
                        Tag = dirInfo.FullName
                    });
                }
            }
            catch (UnauthorizedAccessException) { /* Пропуск защищенных папок */ }

            return items;
        }

        // Навигация кликом мыши по элементам папок.
        private void listView1_ItemActivate(object sender, EventArgs e)
        {
            if (ListV.SelectedItems.Count == 0) return;

            ListViewItem selectedItem = ListV.SelectedItems[0];

            // Клик по файлу книги игнорируем
            if (selectedItem.Tag is BookInfo) return;

            if (selectedItem.Tag is string targetPath && Directory.Exists(targetPath))
            {
                string currentFolderToSelect = null;

                // Если переходим НАВЕРХ по ".." — запоминаем имя текущей папки
                if (selectedItem.Text == "..")
                {
                    DirectoryInfo currentDir = new(TxtBoxPath.Text);
                    currentFolderToSelect = currentDir.Name;
                }

                NavigateAndLoadDirectory(targetPath, currentFolderToSelect);
            }
        }

        // Сопоставление категорий (Pattern Matching C# 8/9).
        private List<BookInfo> FilterBooks()
        {
            if (_allLoadedBooks == null || _allLoadedBooks.Count == 0)
                return new List<BookInfo>();

            // 1. Фильтр по языку (Отбираем ВСЕ, КРОМЕ "ru")
            if (chkFilterByLanguage.Checked || comboBoxGenres.Text == "Отбор по языкам")
            {
                return [.. _allLoadedBooks.Where(b =>
                    string.IsNullOrWhiteSpace(b.Language) ||
                    !b.Language.Trim().Equals("ru", StringComparison.OrdinalIgnoreCase)
                )];
            }

            // 2. Если чекбокс не взведен — работаем по категориям жанров
            string selectedCategory = comboBoxGenres.SelectedItem?.ToString()?.Trim() ?? string.Empty;

            return selectedCategory switch
            {
                "Ошибки" => _allLoadedBooks.Where(b => b.HasError).ToList(),

                _ when GenreData.genreMap.ContainsKey(selectedCategory) =>
                    FilterByGenreGroup(selectedCategory),

                _ => _allLoadedBooks.ToList()
            };
        }

        private List<BookInfo> FilterByGenreGroup(string groupName)
        {
            HashSet<string> groupCodes = GenreData.GetGroupCodes(groupName);
            return [.. _allLoadedBooks.Where(b =>
                !b.HasError &&
                b.AllGenreCodes.Any(code => groupCodes.Contains(code))
            )];
        }

        private async void BtnRefreshList_Click(object sender, EventArgs e)
        {
            _docCache.SaveIfDirty();

            string selectedFileName = null;
            if (ListV.SelectedItems.Count > 0 && ListV.SelectedItems[0].Tag is BookInfo sel)
                selectedFileName = sel.FileName;

            Cursor = Cursors.WaitCursor;
            try
            {
                string folderPath = TxtBoxPath.Text;
                if (string.IsNullOrEmpty(folderPath) || !Directory.Exists(folderPath)) return;

                _allLoadedBooks.Clear();

                string[] files = Directory.GetFiles(folderPath, "*.fb2", SearchOption.TopDirectoryOnly);
                toolSSLabelFiles.Text = $"Всего файлов: {files.Length}";
                UpdateTotalFilesCounter();

                if (files.Length > 0)
                {
                    await RunWithProgressAsync(files, f => _allLoadedBooks.Add(BookInfo.ParseFb2(f)));
                }

                ApplyComboBoxFilter();

                // Восстанавливаем выделение
                if (!string.IsNullOrEmpty(selectedFileName))
                {
                    bool restored = SelectBookByFileName(selectedFileName);

                    // Если книга исчезла из списка (удалена, отфильтрована) — очищаем форму
                    if (!restored)
                        ClearBookControls();
                }
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }

        /// <summary>
        /// Выделяет в ListView книгу с указанным именем файла.
        /// Возвращает true, если книга найдена и выделена; иначе false.
        /// </summary>
        private bool SelectBookByFileName(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return false;

            foreach (ListViewItem item in ListV.Items)
            {
                if (item.Tag is BookInfo b &&
                    string.Equals(b.FileName, fileName, StringComparison.OrdinalIgnoreCase))
                {
                    item.Selected = true;
                    item.Focused = true;
                    item.EnsureVisible();
                    ListV.Focus();
                    return true;
                }
            }
            return false;
        }

        //Вызов полного парсера при выборе книги
        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {
            _docCache.SaveIfDirty();
            if (ListV.SelectedItems.Count == 0) return;

            var selectedItem = ListV.SelectedItems[0];

            // Папки (в т.ч. "..") и прочие не-книги игнорируем — их нечего парсить.
            // Но перед этим очищаем поля формы, чтобы не висело содержимое прошлой книги.
            if (selectedItem.Tag is not BookInfo book)
            {
                ClearBookControls();
                return;
            }

            _docCache.Open(book.FilePath);
            Fb2FullDocument doc = Fb2ParserService.ParseBook(book.FilePath);
            if (doc == null) return;

            BindBookToControls(doc);
        }

        /// <summary>
        /// Очищает все контролы формы, связанные с книгой.
        /// Вызывается при выборе "..", папки или когда книга не выбрана.
        /// </summary>
        private void ClearBookControls()
        {
            // Текстовые поля
            textBoxBookName.Text = string.Empty;
            textBoxAuthor.Text = string.Empty;
            textBoxSeriaName.Text = string.Empty;
            textBoxNumberSeria.Text = string.Empty;
            textBoxSeriaIzdat.Text = string.Empty;
            textBoxImgCount.Text = string.Empty;
            textBoxCoverPage.Text = string.Empty;
            textBoxNewFileName.Text = string.Empty;
            textBoxKodGenre.Text = string.Empty;
            textBoxNameGenre.Text = string.Empty;

            // Поля авторов
            textBoxNewAuthorLastName.Text = string.Empty;
            textBoxNewAuthorFirstName.Text = string.Empty;
            textBoxNewAuthorMiddleName.Text = string.Empty;

            // Гриды
            DGVAuthors.DataSource = null;
            DGVGenres.DataSource = null;

            // WebBrowser — пустая страница вместо старого превью
            webBrowser1.DocumentText = string.Empty;
        }

        private void BindBookToControls(Fb2FullDocument doc)
        {
            // Текстовые поля
            textBoxBookName.Text = doc.Title;
            textBoxAuthor.Text = doc.FirstAuthorFormatted;
            textBoxSeriaName.Text = doc.SeriesName;
            textBoxNumberSeria.Text = doc.SeriesNumber;
            textBoxSeriaIzdat.Text = doc.PublisherSeries;
            textBoxImgCount.Text = doc.ImageCount.ToString();
            textBoxCoverPage.Text = string.IsNullOrEmpty(doc.CoverFileName) ? "Нет" : $"{doc.CoverFileName} ({doc.CoverImageDimensions})";
            textBoxNewFileName.Text = doc.NewFileName;

            var primaryGenre = doc.Genres.FirstOrDefault();
            textBoxKodGenre.Text = primaryGenre?.GenreCode ?? string.Empty;
            textBoxNameGenre.Text = primaryGenre?.GenreName ?? string.Empty;

            // ПЕРЕНЕСЕНО: сопоставление колонок (MapColumnsToPropertiesIfEmpty) теперь делается
            // один раз в Form1_Load, а не на каждый клик по книге — см. комментарий там.
            DGVAuthors.DataSource = null;
            DGVAuthors.DataSource = doc.Authors;

            DGVGenres.DataSource = null;
            DGVGenres.DataSource = doc.Genres;

            // Отображение обложки и превью в WebBrowser
            string oblPathTag = string.Empty;
            if (doc.CoverImageBytes != null && doc.CoverImageBytes.Length > 0)
            {
                string base64 = Convert.ToBase64String(doc.CoverImageBytes);
                oblPathTag = $"<img src='data:image/jpeg;base64,{base64}' align='left' width='150' height='235' style='margin-right:10px;'/>";
            }

            webBrowser1.DocumentText = $@"
        <html>
        <head>
            <meta charset='utf-8'/>
            <style>
                body {{ font-family: sans-serif; font-size: 11pt; color: #111; margin: 6px; background: Ivory; }}
                h2 {{ color: #800000; margin: 0 0 4px 0; font-size: 16pt; }}
                h3 {{ color: #800000; margin: 0 0 8px 0; font-size: 15pt; }}
                .content {{ font-size: 11pt; }}
            </style>
        </head>
        <body>
            {oblPathTag}
            <h2>{System.Net.WebUtility.HtmlEncode(doc.FirstAuthorFormatted)}</h2>
            <h3>{System.Net.WebUtility.HtmlEncode(doc.Title)}</h3>
            <div class='content'>
                {doc.AnnotationHtml}
                <div style='clear:both; height: 6px;'></div>
                {doc.BodyPreviewHtml}
            </div>
        </body>
        </html>";
        }

        // Сопоставляет колонки DataGridView со свойствами модели по имени (если DataPropertyName
        // ещё не задан явно в дизайнере). Вызывается один раз при загрузке формы для каждой сетки
        // (DGVAuthors, DGVGenres) — см. Form1_Load.
        private void MapColumnsToPropertiesIfEmpty(DataGridView dgv, Type itemType)
        {
            if (dgv == null || itemType == null) return;

            try
            {
                foreach (DataGridViewColumn col in dgv.Columns)
                {
                    // Не перезаписываем уже заданный DataPropertyName
                    if (!string.IsNullOrWhiteSpace(col.DataPropertyName))
                        continue;

                    // Ищем свойство по имени колонки (используется именно Name колонки)
                    var prop = itemType.GetProperty(col.Name);
                    if (prop != null)
                    {
                        col.DataPropertyName = prop.Name;
                    }
                    else
                    {
                        // Диагностика: поможет понять, какие имена колонок не соответствуют свойствам модели
                        System.Diagnostics.Debug.WriteLine($"MapColumns: '{dgv.Name}' column '{col.Name}' — свойство не найдено в {itemType.Name}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"MapColumns Error: {ex.Message}");
            }
        }

        private void TextBoxSeriaName_TextChanged(object sender, EventArgs e)
        {
            //textBoxSeriaName.Height = TextBoxTextHtight(textBoxSeriaName);
        }

        private void TextBoxNewFileName_TextChanged(object sender, EventArgs e)
        {
            textBoxNewFileName.Height = TextBoxTextHtight(textBoxNewFileName);
        }

        private int TextBoxTextHtight(TextBox textBox)
        {
            // Учитываем ширину и перенос строк
            int lines = textBox.GetLineFromCharIndex(textBox.TextLength) + 1;
            int lineHeight = textBox.Font.Height + 3;

            if (lines > 1)
            {
                // Устанавливаем новую высоту
                textBox.Height = (lines * lineHeight);
            }
            else if (lines <= 1)
            {
                textBox.Height = 22;
            }
            return textBox.Height;

        }

        // Пакетный вывод списка элементов с иконками книг без перерисовки экрана
        public void PopulateListView(List<BookInfo> books)
        {
            ListV.BeginUpdate();
            try
            {
                ListV.Items.Clear();

                // 1. Возвращаем элементы навигации
                DirectoryInsertFirstItemWithoutClear(); // Вставка ".."

                string currentPath = TxtBoxPath.Text;
                if (Directory.Exists(currentPath))
                {
                    foreach (string foundDirectory in Directory.GetDirectories(currentPath))
                    {
                        DirectoryToList(foundDirectory); // Вставка подпапок
                    }
                }

                // 2. Вывод книг с языками
                if (books != null && books.Count > 0)
                {
                    var items = new List<ListViewItem>(books.Count);

                    foreach (var book in books)
                    {
                        var item = new ListViewItem(book.FileName)
                        {
                            ImageKey = "fb2",
                            Tag = book
                        };

                        // 1. Автор (Колонка 2)
                        item.SubItems.Add(string.IsNullOrEmpty(book.Author) ? "" : book.Author);

                        // 2. Серия и Номер (Колонка 3)
                        item.SubItems.Add(string.IsNullOrEmpty(book.Seria) ? "" : book.Seria);

                        // 3. Название (Колонка 4)
                        item.SubItems.Add(string.IsNullOrEmpty(book.Title) ? "" : book.Title);

                        // 4. Жанр (Колонка 5)
                        item.SubItems.Add(string.IsNullOrEmpty(book.GenreName) ? "" : book.GenreName);

                        // 5. Размер (Колонка 6)
                        item.SubItems.Add(string.IsNullOrEmpty(book.Razmer) ? "" : book.Razmer);

                        // 6. Кодировка / Код (Колонка 7)
                        item.SubItems.Add(string.IsNullOrEmpty(book.KodePage) ? "" : book.KodePage);

                        // 7. Язык (Колонка 8 — "Язык")
                        item.SubItems.Add(string.IsNullOrEmpty(book.Language) ? "" : book.Language);

                        items.Add(item);
                    }
                    ListV.Items.AddRange(items.ToArray());
                }
            }
            finally
            {
                ListV.EndUpdate();
            }

            if (TextBoxFinded != null)
            {
                TextBoxFinded.Text = books != null ? books.Count.ToString() : "0";
            }
        }

        // Служебные методы вставки элементов навигации без сбивания ширины колонок
        public void DirectoryInsertFirstItemWithoutClear()
        {
            var lvItem = ListV.Items.Add("..");
            lvItem.ImageKey = "Up";
            lvItem.SubItems.AddRange(["", "", "", "", "", "", ""]); // 7 пустых SubItems
        }

        // Основная кнопка сохранения — у таблицы DGVGenres.
        private void BtnSaveChangeGenre_Click(object sender, EventArgs e) => SaveChangedGenre();

        // Кнопка-дублёр возле выпадающего меню жанров: пользователь выбрал новый жанр
        // в меню (оно пишет код в textBoxKodGenre) и сразу жмёт сохранить, не уводя
        // мышь обратно к таблице. Логика полностью совпадает с BtnSaveChangeGenre.

        private void BtnChangeGenreSave_Click(object sender, EventArgs e) => SaveChangedGenre();

        private void SaveChangedGenre()
        {
            try
            {
                string newGenreCode = textBoxKodGenre.Text;

                if (DGVGenres.SelectedRows.Count == 0) return;
                string oldGenreCode = DGVGenres.SelectedRows[0].Cells["GenreCode"].Value?.ToString();

                if (ListV_FocusedItemOrNull() is not { } focused) return;
                string fileName = Path.Combine(TxtBoxPath.Text, focused.SubItems[0].Text);

                if (Fb2Tools.ProcessGenres(_docCache, fileName, Fb2Tools.GenreOperation.Rename, oldGenreCode, newGenreCode))
                    RefreshBookAfterChange(fileName);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

        private void BtnAddGenre_Click(object sender, EventArgs e)
        {
            try
            {
                string newGenreCode = textBoxKodGenre.Text;

                if (ListV_FocusedItemOrNull() is not { } focused) return;
                string fileName = Path.Combine(TxtBoxPath.Text, focused.SubItems[0].Text);

                if (Fb2Tools.ProcessGenres(_docCache, fileName, Fb2Tools.GenreOperation.Add, newGenreCode))
                    RefreshBookAfterChange(fileName);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

        private void BtnDeleteGenre_Click(object sender, EventArgs e)
        {
            try
            {
                if (DGVGenres.SelectedRows.Count == 0) return;
                string genreCode = DGVGenres.SelectedRows[0].Cells["GenreCode"].Value?.ToString();

                if (ListV_FocusedItemOrNull() is not { } focused) return;
                string fileName = Path.Combine(TxtBoxPath.Text, focused.SubItems[0].Text);

                if (Fb2Tools.ProcessGenres(_docCache, fileName, Fb2Tools.GenreOperation.Delete, genreCode))
                    RefreshBookAfterChange(fileName);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

        private void BtnGenreMoveUp_Click(object sender, EventArgs e)
        {
            try
            {
                if (DGVGenres.SelectedRows.Count == 0) return;
                string genreCode = DGVGenres.SelectedRows[0].Cells["GenreCode"].Value?.ToString();
                int indx = DGVGenres.SelectedRows[0].Index;

                if (indx == 0)
                {
                    MessageBox.Show(
                        "Нельзя переместить вверх жанр, который уже находится там!" + Environment.NewLine + "Повторите попытку!",
                        "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                if (ListV_FocusedItemOrNull() is not { } focused) return;
                string fileName = Path.Combine(TxtBoxPath.Text, focused.SubItems[0].Text);

                if (Fb2Tools.ProcessGenres(_docCache, fileName, Fb2Tools.GenreOperation.MoveToFirst, genreCode))
                    RefreshBookAfterChange(fileName);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

        // ListV в изначальном коде — это твой listView1; используем listView1.FocusedItem напрямую,
        // проверяя на null, чтобы не дублировать try/catch в каждом обработчике.
        private ListViewItem ListV_FocusedItemOrNull() => ListV.FocusedItem;

        // Заполняет 3 текстбокса данными выделенного в DGVAuthors автора
        private void DGVAuthors_SelectionChanged(object sender, EventArgs e)
        {
            if (DGVAuthors.SelectedRows.Count == 0) return;

            var row = DGVAuthors.SelectedRows[0];
            textBoxNewAuthorLastName.Text = row.Cells["LastName"].Value?.ToString() ?? string.Empty;
            textBoxNewAuthorFirstName.Text = row.Cells["FirstName"].Value?.ToString() ?? string.Empty;
            textBoxNewAuthorMiddleName.Text = row.Cells["MiddleName"].Value?.ToString() ?? string.Empty;
        }

        // Сохранить изменения выбранного автора — значения теперь берутся
        // из текстбоксов (они синхронизированы с выделенной строкой через DGVAuthors_SelectionChanged)
        private void BtnSaveChangeAuthor_Click(object sender, EventArgs e)
        {
            try
            {
                if (DGVAuthors.SelectedRows.Count == 0) return;
                int index = DGVAuthors.SelectedRows[0].Index;

                string lastName = textBoxNewAuthorLastName.Text.Trim();
                string firstName = textBoxNewAuthorFirstName.Text.Trim();
                string middleName = textBoxNewAuthorMiddleName.Text.Trim();

                if (ListV_FocusedItemOrNull() is not { } focused) return;
                string fileName = Path.Combine(TxtBoxPath.Text, focused.SubItems[0].Text);

                if (Fb2Tools.ProcessAuthors(_docCache, fileName, Fb2Tools.AuthorOperation.Edit, index,
                        firstName, middleName, lastName))
                    RefreshBookAfterChange(fileName);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

        // Добавить нового автора — значения из тех же текстбоксов.
        // Если в гриде что-то выделено, поля к этому моменту заполнены данными выделенной
        // строки (см. DGVAuthors_SelectionChanged) — чтобы добавить НОВОГО автора,
        // пользователь должен сначала очистить/переписать поля, либо снять выделение в гриде.
        private void BtnAddAuthor_Click(object sender, EventArgs e)
        {
            try
            {
                string lastName = textBoxNewAuthorLastName.Text.Trim();
                string firstName = textBoxNewAuthorFirstName.Text.Trim();
                string middleName = textBoxNewAuthorMiddleName.Text.Trim();

                if (string.IsNullOrEmpty(lastName) && string.IsNullOrEmpty(firstName))
                {
                    MessageBox.Show("Укажите хотя бы фамилию или имя автора.",
                        "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                if (ListV_FocusedItemOrNull() is not { } focused) return;
                string fileName = Path.Combine(TxtBoxPath.Text, focused.SubItems[0].Text);

                if (Fb2Tools.ProcessAuthors(_docCache, fileName, Fb2Tools.AuthorOperation.Add,
                        firstName: firstName, middleName: middleName, lastName: lastName))
                {
                    RefreshBookAfterChange(fileName);

                    textBoxNewAuthorLastName.Clear();
                    textBoxNewAuthorFirstName.Clear();
                    textBoxNewAuthorMiddleName.Clear();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

        // Удалить выбранного автора
        private void BtnDeleteAuthor_Click(object sender, EventArgs e)
        {
            try
            {
                if (DGVAuthors.SelectedRows.Count == 0) return;
                int index = DGVAuthors.SelectedRows[0].Index;

                if (ListV_FocusedItemOrNull() is not { } focused) return;
                string fileName = Path.Combine(TxtBoxPath.Text, focused.SubItems[0].Text);

                if (Fb2Tools.ProcessAuthors(_docCache, fileName, Fb2Tools.AuthorOperation.Delete, index))
                    RefreshBookAfterChange(fileName);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

        // Переставить местами значения в текстбоксах (Имя <-> Фамилия, с учётом Отчества).
        // Работает над текстбоксами (не над гридом) — так правки видны сразу и попадают
        // в файл при следующем нажатии "Сохранить".
        private void BtnDGVPerestanovka_Click(object sender, EventArgs e)
        {
            try
            {
                string firstName = textBoxNewAuthorFirstName.Text;
                string middleName = textBoxNewAuthorMiddleName.Text;
                string lastName = textBoxNewAuthorLastName.Text;

                if (string.IsNullOrEmpty(middleName))
                {
                    textBoxNewAuthorFirstName.Text = lastName;
                    textBoxNewAuthorLastName.Text = firstName;
                }
                else
                {
                    textBoxNewAuthorFirstName.Text = middleName;
                    textBoxNewAuthorMiddleName.Text = lastName;
                    textBoxNewAuthorLastName.Text = firstName;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

        // Переместить выбранного автора на одну позицию вверх
        private void BtnAuthorMoveUp_Click(object sender, EventArgs e)
        {
            try
            {
                if (DGVAuthors.SelectedRows.Count == 0) return;
                int index = DGVAuthors.SelectedRows[0].Index;

                if (index == 0)
                {
                    MessageBox.Show(
                        "Нельзя переместить вверх автора, который уже находится там!" + Environment.NewLine + "Повторите попытку!",
                        "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                if (ListV_FocusedItemOrNull() is not { } focused) return;
                string fileName = Path.Combine(TxtBoxPath.Text, focused.SubItems[0].Text);

                if (Fb2Tools.ProcessAuthors(_docCache, fileName, Fb2Tools.AuthorOperation.MoveUp, index))
                    RefreshBookAfterChange(fileName);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

        // Единый метод обновления после ЛЮБОГО изменения книги (жанр/автор/название/серия).
        // Заменяет RefreshBookAfterGenreChange, RefreshBookAfterAuthorChange
        // и RefreshBookAndListColumn — их нужно удалить, а все вызовы заменить на этот.
        // Единый метод обновления после ЛЮБОГО изменения книги (жанр/автор/название/серия).
        // Единый метод обновления после ЛЮБОГО изменения книги (жанр/автор/название/серия).
        private void RefreshBookAfterChange(string fileName)
        {
            lvItem = ListV.FocusedItem;

            var xdoc = _docCache.Open(fileName);
            var doc = Fb2ParserService.ParseFromXDocument(xdoc, fileName);
            if (doc == null) return;

            BindBookToControls(doc); // сюда же входит textBoxNewFileName.Text = doc.NewFileName

            if (lvItem == null) return;

            lvItem.SubItems[1].Text = textBoxAuthor.Text; // Автор
            lvItem.SubItems[2].Text = string.IsNullOrEmpty(textBoxNumberSeria.Text)
                ? textBoxSeriaName.Text
                : $"{textBoxSeriaName.Text} [{textBoxNumberSeria.Text}]"; // Серия и номер
            lvItem.SubItems[3].Text = textBoxBookName.Text; // Название
            lvItem.SubItems[4].Text = textBoxNameGenre.Text; // Жанр
            lvItem.SubItems[7].Text = "ru"; // Язык
        }
        private void btnParameters_Click(object sender, EventArgs e)
        {
            // ПОЛНОЦЕННЫЙ ВАРИАНТ: передаём ссылку на текущий (реально открытый) Form1,
            // чтобы Form2 могла применить новые пути сразу, без перезапуска программы.
            Form2 form2 = new(this);
            form2.Show();
        }

        // Изменить/удалить издательскую серию (textBoxSeriaIzdat)
        private void BtnClearSeriaIsdat_Click(object sender, EventArgs e)
        {
            try
            {
                if (ListV_FocusedItemOrNull() is not { } focused) return;
                string fileName = Path.Combine(TxtBoxPath.Text, focused.SubItems[0].Text);

                bool isChanged = Fb2Tools.ClearSeriesIsdat(fileName);

                textBoxSeriaIzdat.Text = string.Empty;

                if (isChanged)
                    RefreshBookAfterChange(fileName);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

        // Сохранить изменённое название книги
        private void BtnSaveChangeNazvanie_Click(object sender, EventArgs e)
        {
            try
            {
                string newTitle = textBoxBookName.Text;

                if (ListV_FocusedItemOrNull() is not { } focused) return;
                string fileName = Path.Combine(TxtBoxPath.Text, focused.SubItems[0].Text);

                if (Fb2Tools.ProcessTitle(_docCache, fileName, newTitle))
                    RefreshBookAfterChange(fileName);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

        // Сохранить изменения серии (добавляет/меняет/удаляет — в зависимости от того,
        // задано ли имя серии)
        private void BtnSaveChangeSeria_Click(object sender, EventArgs e)
        {
            try
            {
                string seriesName = textBoxSeriaName.Text.Trim();
                string seriesNumber = textBoxNumberSeria.Text.Trim();

                if (ListV_FocusedItemOrNull() is not { } focused) return;
                string fileName = Path.Combine(TxtBoxPath.Text, focused.SubItems[0].Text);

                bool isChanged;
                string listDisplay;

                if (string.IsNullOrEmpty(seriesName))
                {
                    isChanged = Fb2Tools.ProcessSeries(_docCache, fileName, Fb2Tools.SeriesOperation.Delete);
                    listDisplay = string.Empty;

                    if (isChanged)
                        textBoxNumberSeria.Clear();
                }
                else
                {
                    isChanged = Fb2Tools.ProcessSeries(_docCache, fileName, Fb2Tools.SeriesOperation.Edit,
                        seriesName, seriesNumber);
                    listDisplay = string.IsNullOrEmpty(seriesNumber)
                        ? seriesName
                        : $"{seriesName} [{seriesNumber}]";
                }

                if (isChanged)
                    RefreshBookAfterChange(fileName);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

        private void BtnEditInNP_Click(object sender, EventArgs e) => OpenInEditor(dirNP);

        private void BtnEdinInFBE_Click(object sender, EventArgs e) => OpenInEditor(dirFBE);

        private void OpenInEditor(string editorPath)
        {
            if (ListV.FocusedItem == null) return;
            string path = Path.Combine(TxtBoxPath.Text, ListV.FocusedItem.Text);
            Process.Start(editorPath, AddQuotesIfRequired(path));
        }

        string AddQuotesIfRequired(string path) =>
            !string.IsNullOrEmpty(path) && path.Contains(" ") ? $"\"{path}\"" : path;

        private void textBoxNewAuthorFirstName_MouseUp(object sender, MouseEventArgs e)
        {
            lastFocusedTextBox = sender as TextBox;
            strSelectedText = lastFocusedTextBox.SelectedText;
        }

        private void textBoxNewAuthorMiddleName_MouseUp(object sender, MouseEventArgs e)
        {
            lastFocusedTextBox = sender as TextBox;
            strSelectedText = lastFocusedTextBox.SelectedText;
        }

        private void textBoxNewAuthorLastName_MouseUp(object sender, MouseEventArgs e)
        {
            lastFocusedTextBox = sender as TextBox;
            strSelectedText = lastFocusedTextBox.SelectedText;
        }

        private void textBoxBookName_MouseUp(object sender, MouseEventArgs e)
        {
            lastFocusedTextBox = sender as TextBox;
            strSelectedText = lastFocusedTextBox.SelectedText;
        }

        private void textBoxSeriaName_MouseUp(object sender, MouseEventArgs e)
        {
            lastFocusedTextBox = sender as TextBox;
            strSelectedText = lastFocusedTextBox.SelectedText;
        }

        private void VsePropisnie_Click(object sender, EventArgs e)
        {
            try
            {
                lastFocusedTextBox.Text = TitleFormat.ToUpperCase(lastFocusedTextBox.Text, strSelectedText);
            }
            catch (Exception ex)
            {
                string textMess = $"Не выбран нужный ТекстБокс!\n {ex.Message}";
                MessageBox.Show(textMess, "Смена регистра", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

        private void PervSlovoPropisnie_Click(object sender, EventArgs e)
        {
            try
            {
                lastFocusedTextBox.Text = TitleFormat.FirstWordToUpper(lastFocusedTextBox.Text, strSelectedText);
            }
            catch (Exception ex)
            {
                string textMess = $"Не выбран нужный ТекстБокс!\n {ex.Message}";
                MessageBox.Show(ex.Message, "Смена регистра", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

        private void VseSlovaSPropisnoy_Click(object sender, EventArgs e)
        {
            try
            {
                lastFocusedTextBox.Text = TitleFormat.CapitalizeWords(lastFocusedTextBox.Text, strSelectedText);
            }
            catch (Exception ex)
            {
                string textMess = $"Не выбран нужный ТекстБокс!\n {ex.Message}";
                MessageBox.Show(textMess, "Смена регистра", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

        private void KakVPredlogenii_Click(object sender, EventArgs e)
        {
            try
            {
                lastFocusedTextBox.Text = TitleFormat.SentenceCaseOrCapitalizeFirst(lastFocusedTextBox.Text, strSelectedText);
            }
            catch (Exception ex)
            {
                string textMess = $"Не выбран нужный ТекстБокс!\n {ex.Message}";
                MessageBox.Show(textMess, "Смена регистра", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

        private void VseStrochnie_Click(object sender, EventArgs e)
        {
            try
            {
                lastFocusedTextBox.Text = TitleFormat.ToLowerCase(lastFocusedTextBox.Text, strSelectedText);
            }
            catch (Exception ex)
            {
                string textMess = $"Не выбран нужный ТекстБокс!\n {ex.Message}";
                MessageBox.Show(textMess, "Смена регистра", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

        private void BtnPerestanovka_Click(object sender, EventArgs e)
        {
            try
            {
                lastFocusedTextBox.Text = TitleFormat.SwapWords(lastFocusedTextBox.Text, strSelectedText);
            }
            catch (Exception ex)
            {
                string textMess = $"Не выбран нужный ТекстБокс!\n {ex.Message}";
                MessageBox.Show(textMess, "Смена регистра", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

        private void BtnKavichki_Click(object sender, EventArgs e)
        {
            try
            {
                lastFocusedTextBox.Text = TitleFormat.KavichkiZamena(lastFocusedTextBox.Text, strSelectedText);
                strSelectedText = "";
            }
            catch (Exception ex)
            {
                string textMess = $"Не выбран нужный ТекстБокс!\n {ex.Message}";
                MessageBox.Show(textMess, "Смена регистра", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }

        }

        // Сохраняем список книг в файл XML (Сериализация)
        private void BtnSaveFileXML_Click(object sender, EventArgs e)
        {
            string lastFile = ListV.FocusedItem?.Text ?? "";

            // Книги, отфильтрованные сейчас (то, что видно в ListView)
            var visibleBooks = ListV.Items
                .Cast<ListViewItem>()
                .Where(it => string.Equals(it.ImageKey, "fb2", StringComparison.OrdinalIgnoreCase))
                .Select(it => it.Tag as BookInfo)
                .Where(bi => bi != null)
                .ToList();

            // Полный список в памяти
            var allBooks = _allLoadedBooks ?? new List<BookInfo>();

            List<BookInfo> booksToSave;

            // Есть ли смысл спрашивать: видимых меньше, чем всего
            if (allBooks.Count > 0 && visibleBooks.Count < allBooks.Count)
            {
                var result = MessageBox.Show(
                    $"В списке {allBooks.Count} книг, после фильтра видно {visibleBooks.Count}.\n\n" +
                    "Сохранить только текущую группу?\n" +
                    "«Да» — только группу, «Нет» — весь список.",
                    "Сохранение списка книг",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                booksToSave = (result == DialogResult.Yes) ? visibleBooks : allBooks;
            }
            else
            {
                // Фильтр не активен или всё совпадает — сохраняем всё
                booksToSave = allBooks.Count > 0 ? allBooks : visibleBooks;
            }

            SerDeSerListFiles clsSer = new(this);
            clsSer.ListSave(lastFile, booksToSave);

            MessageBox.Show(
                $"Сохранено книг: {booksToSave.Count}.\nФайл: BooksList-*.xml",
                "Сохранение списка книг",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // Загружаем сохраненный список файлов в ListView (ДеСериализация)
        private void BtnLoadFromXML_Click(object sender, EventArgs e)
        {
            OpenFileDialog fileDialog = new()
            {
                InitialDirectory = Application.StartupPath,
                Filter = "xml files (*.xml)|*.xml|All files (*.*)|*.*"
            };

            if (fileDialog.ShowDialog() != DialogResult.OK) return;

            SerDeSerListFiles clsSer = new(this);

            // 1. Сохраняем имя группы, которое LoadList сейчас выставит в comboBoxGenres,
            //    чтобы _allLoadedBooks наполнился ДО срабатывания фильтра.
            //    Проще всего — временно отключить обработчик.
            comboBoxGenres.SelectedIndexChanged -= comboBoxGenres_SelectedIndexChanged;
            List<BookInfo> loadedBooks;
            try
            {
                loadedBooks = clsSer.LoadList(fileDialog.FileName);
            }
            finally
            {
                comboBoxGenres.SelectedIndexChanged += comboBoxGenres_SelectedIndexChanged;
            }

            // 2. Наполняем память
            _allLoadedBooks.Clear();
            if (loadedBooks.Count > 0)
                _allLoadedBooks.AddRange(loadedBooks);

            // 3. Применяем фильтр вручную (по группе из XML)
            ApplyComboBoxFilter();

            // 4. Выделяем последнюю книгу
            if (!string.IsNullOrEmpty(clsSer.lastFN))
            {
                ListViewItem foundItem = ListV.FindItemWithText(clsSer.lastFN);
                if (foundItem != null)
                {
                    foundItem.Selected = true;
                    foundItem.Focused = true;
                    foundItem.EnsureVisible();
                    ListV.Focus();
                }
            }
        }

        private void BtnSetLangRu_Click(object sender, EventArgs e)
        {
            try
            {
                if (ListV_FocusedItemOrNull() is not { } focused) return;
                string fileName = Path.Combine(TxtBoxPath.Text, focused.SubItems[0].Text);

                if (Fb2Tools.SetLanguageRu(_docCache, fileName))
                    RefreshBookAfterChange(fileName);
                else
                    MessageBox.Show("Язык уже установлен как 'ru'.", "Язык",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

        private void BtnRenameFiles_Click(object sender, EventArgs e)
        {
            try
            {
                // 1. Книга выбрана?
                if (ListV.FocusedItem is not { } focused)
                {
                    MessageBox.Show("Книга не выбрана.", "Переименование",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                string oldName = focused.SubItems[0].Text;
                string oldPath = Path.Combine(TxtBoxPath.Text, oldName);

                // 2. Новое имя
                string newName = textBoxNewFileName.Text.Trim();

                if (string.IsNullOrWhiteSpace(newName))
                {
                    MessageBox.Show("Новое имя файла пустое.", "Переименование",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                // 3. Гарантируем расширение .fb2
                if (!newName.EndsWith(".fb2", StringComparison.OrdinalIgnoreCase))
                    newName += ".fb2";

                // 4. Чистим недопустимые символы
                newName = RemoveInvalidChars(newName);

                string newPath = Path.Combine(TxtBoxPath.Text, newName);

                // 5. Имя не изменилось?
                if (string.Equals(oldName, newName, StringComparison.OrdinalIgnoreCase))
                {
                    MessageBox.Show("Имя файла не изменилось.", "Переименование",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                // 6. Файл с таким именем уже есть?
                if (File.Exists(newPath))
                {
                    MessageBox.Show($"Файл «{newName}» уже существует.", "Переименование",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // 7. Старый файл существует?
                if (!File.Exists(oldPath))
                {
                    MessageBox.Show("Исходный файл не найден.", "Переименование",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // 8. Если книга открыта в кэше — сохранить и закрыть
                bool wasOpen = string.Equals(_docCache.CurrentPath, oldPath,
                                             StringComparison.OrdinalIgnoreCase);
                if (wasOpen)
                {
                    _docCache.SaveIfDirty();
                    _docCache.CloseWithoutSave();
                }

                // 9. Переименовываем на диске
                File.Move(oldPath, newPath);

                // 10. Обновляем BookInfo в кэше
                var cached = _allLoadedBooks.FirstOrDefault(b =>
                    string.Equals(b.FilePath, oldPath, StringComparison.OrdinalIgnoreCase));

                if (cached != null)
                {
                    cached.FileName = newName;
                    cached.FilePath = newPath;
                }

                // 11. Обновляем SubItems[0] — имя файла в ListView
                focused.SubItems[0].Text = newName;

                // 12. Обновляем остальные SubItems (автор, серия, название, жанр, язык)
                RefreshBookAfterChange(newPath);

                // 13. Страховка — выравниваем поле
                textBoxNewFileName.Text = newName;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

        private static string RemoveInvalidChars(string name)
        {
            string[] letters = { "\\", "/", "|", "<", ">", ":", "*", "?", "\"" };
            foreach (string letter in letters)
            {
                if (!name.Contains(letter)) continue;

                if (letter == "\\" || letter == "/" || letter == "|")
                    name = name.Replace(letter, " - ");
                else if (letter == "<" || letter == ">" || letter == "?" || letter == "\"")
                    name = name.Replace(letter, "");
                else
                    name = name.Replace(letter, ".");
            }
            return name;
        }

        private void BtnShowImg_Click(object sender, EventArgs e)
        {
            if (_docCache.CurrentDoc == null)
            {
                MessageBox.Show("Сначала выберите книгу.", "Картинки",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (var frm = new FormImages(_docCache, dirCover))
            {
                frm.ShowDialog(this);

                if (frm.Changed)
                {
                    // Обновить счётчик картинок на главной форме
                    var root = _docCache.CurrentDoc.Root;
                    if (root != null)
                    {
                        int count = root.Elements(
                            System.Xml.Linq.XName.Get(
                                "binary",
                                "http://www.gribuser.ru/xml/fictionbook/2.0")).Count();
                        textBoxImgCount.Text = count.ToString();
                    }
                }
            }
        }

        public void DirectoryToList(string dirPath)
        {
            var dirInfo = new DirectoryInfo(dirPath);
            var item = ListV.Items.Add(dirInfo.Name);
            item.ImageKey = "Folder";
            item.Tag = dirInfo.FullName;
            item.SubItems.AddRange(["", "", "", "", "", "", ""]); // 7 пустых SubItems
        }

        //Удаляет выделенные в ListView файлы с диска в Корзину и обновляет список
        private void BtnDelete_Click(object sender, EventArgs e)
        {
            if (ListV.SelectedItems.Count == 0) return;

            // 1. Динамический текст запроса в зависимости от числа выделенных строк
            //int count = ListV.SelectedItems.Count;
            //string message = count > 1
            //    ? $"Вы действительно хотите переместить в корзину эти файлы/папки ({count} шт.)?"
            //    : "Вы действительно хотите переместить в корзину эту книгу/папку?";

            //if (MessageBox.Show(message, "Удаление файлов", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            //{
            //    return;
            //}

            // 2. Фиксируем индекс первой выделенной строки перед удалением для последующего фокуса
            int lastSelectedIndex = ListV.SelectedIndices[0];

            // Клонируем список выделенных ListViewItem
            var itemsToDelete = ListV.SelectedItems.Cast<ListViewItem>().ToList();

            ListV.BeginUpdate();
            try
            {
                foreach (ListViewItem item in itemsToDelete)
                {
                    // Пропускаем служебный узел перехода наверх ".."
                    if (item.Text == "..") continue;

                    string fullPath = string.Empty;
                    bool isFolder = false;

                    // Определяем путь из Tag объекта или директории
                    if (item.Tag is BookInfo book)
                    {
                        fullPath = book.FilePath;
                        _allLoadedBooks?.Remove(book); // Исключаем из общего кэша памяти
                    }
                    else if (item.Tag is string folderPath)
                    {
                        fullPath = folderPath;
                        isFolder = true;
                    }
                    else
                    {
                        // Резервный расчет пути через текущую папку
                        fullPath = Path.Combine(TxtBoxPath.Text, item.Text);
                        isFolder = item.ImageKey == "Folder";
                    }

                    // 3. Отправляем в Корзину средствами Visual Basic API
                    if (!string.IsNullOrEmpty(fullPath) && (File.Exists(fullPath) || Directory.Exists(fullPath)))
                    {
                        try
                        {
                            if (isFolder)
                            {
                                FileSystem.DeleteDirectory(fullPath, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);

                                // Чистим память: убираем из _allLoadedBooks все книги,
                                // которые физически лежали внутри удалённой папки
                                _allLoadedBooks?.RemoveAll(b =>
                                    b.FilePath.StartsWith(fullPath + Path.DirectorySeparatorChar,
                                                          StringComparison.OrdinalIgnoreCase));
                            }
                            else
                            {
                                FileSystem.DeleteFile(fullPath, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
                            }

                            // Удаляем строку из UI
                            ListV.Items.Remove(item);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Не удалось отправить в корзину: {item.Text}\nОшибка: {ex.Message}",
                                            "Ошибка удаления", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                    else
                    {
                        // Если файла не существует на диске, удаляем фантомную строку
                        ListV.Items.Remove(item);
                    }
                }
            }
            finally
            {
                ListV.EndUpdate();
            }

            // 4.5. Пересчитываем общее количество .fb2 в текущей папке (для TextBoxObrabotano)
            UpdateTotalFilesCounter();

            // 4. Устанавливаем фокус на следующий/предыдущий элемент
            RestoreSelectionAndFocus(lastSelectedIndex);

            // 5. Синхронизируем счетчик найденных элементов
            if (TextBoxFinded != null)
            {
                int folderOffset = ListV.Items.Count > 0 && ListV.Items[0].Text == ".." ? 1 : 0;
                TextBoxFinded.Text = Math.Max(0, ListV.Items.Count - folderOffset).ToString();
            }
        }

        /// <summary>
        /// Пересчитывает общее количество .fb2 в текущей папке и обновляет
        /// TextBoxObrabotano и toolSSLabelFiles. Вызывается после операций,
        /// которые могут изменить число файлов (удаление, переименование и т.п.).
        /// </summary>
        private void UpdateTotalFilesCounter()
        {
            try
            {
                string path = TxtBoxPath.Text;
                int total = 0;

                if (!string.IsNullOrEmpty(path) && Directory.Exists(path))
                {
                    total = Directory.GetFiles(path, "*.fb2", SearchOption.TopDirectoryOnly).Length;
                }

                if (TextBoxObrabotano != null)
                    TextBoxObrabotano.Text = total.ToString();

                if (toolSSLabelFiles != null)
                    toolSSLabelFiles.Text = $"Всего файлов: {total}";
            }
            catch
            {
                // Пересчёт — вспомогательная операция. Если упала — оставляем
                // предыдущие значения счётчиков, не мешаем основной работе.
            }
        }

        // Корректно восстанавливает выделение и фокус на соседнем элементе после удаления
        private void RestoreSelectionAndFocus(int preferredIndex)
        {
            if (ListV.Items.Count == 0) return;

            // Корректируем индекс: если удален крайний элемент, берем новый последний
            int targetIndex = Math.Min(preferredIndex, ListV.Items.Count - 1);

            if (targetIndex >= 0)
            {
                var targetItem = ListV.Items[targetIndex];

                targetItem.Selected = true;
                targetItem.Focused = true;
                targetItem.EnsureVisible();
                ListV.Focus();
            }
        }

        //Дополнительно: Поддержка горячей клавиши Delete
        private void listView1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                BtnDelete_Click(sender, e);
                e.Handled = true;
            }
        }

        // Форма настроек
        private void BtnOpenForm2_Click(object sender, EventArgs e)
        {
            Form2 form2 = new();
            form2.Show();
        }

    }

}
