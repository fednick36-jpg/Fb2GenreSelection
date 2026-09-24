using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Linq;

namespace Fb2GenreSelection
{
    public class SerDeSerListFiles
    {
        private readonly Form1 _frm;
        public string lastFN = "";

        public SerDeSerListFiles(Form1 frm)
        {
            _frm = frm;
        }

        // Сохранение списка книг с обработкой исключений
        // Сохранение списка книг с обработкой исключений
        public void ListSave(string lastFileName, IEnumerable<BookInfo> booksToSave)
        {
            try
            {
                lastFN = lastFileName;

                string dirPath = _frm.TxtBoxPath.Text;
                string pathFn = string.IsNullOrEmpty(dirPath) ? "Default" : dirPath;
                int lastSlash = pathFn.LastIndexOfAny(['\\', '/']);
                if (lastSlash >= 0 && lastSlash < pathFn.Length - 1)
                {
                    pathFn = pathFn.Substring(lastSlash + 1);
                }

                var bookList = booksToSave?.ToList() ?? new List<BookInfo>();

                XElement books = new("books",
                    new XElement("dir", dirPath),
                    new XElement("last-file", lastFN),
                    new XElement("grupp", _frm.comboBoxGenres.Text),
                    new XElement("obrabot", _frm.TextBoxObrabotano.Text),
                    new XElement("find", _frm.TextBoxFinded.Text),
                    from bi in bookList
                    where bi != null
                    select new XElement("book",
                        new XElement("nameFile", bi.FileName ?? ""),
                        new XElement("author", bi.Author ?? ""),
                        new XElement("seria", bi.Seria ?? ""),
                        new XElement("bookName", bi.Title ?? ""),
                        new XElement("genre", bi.GenreName ?? ""),
                        new XElement("genreKod", bi.GenreKod ?? ""),
                        new XElement("allGenreCodes", bi.AllGenreCodes != null
                            ? string.Join(",", bi.AllGenreCodes)
                            : ""),
                        new XElement("razmerFile", bi.Razmer ?? ""),
                        new XElement("kod", bi.KodePage ?? ""),
                        new XElement("lang", bi.Language ?? "")
                    )
                );

                string myFile = Path.Combine(Application.StartupPath, $"BooksList-{pathFn}.xml");
                books.Save(myFile, SaveOptions.None);
            }
            catch (UnauthorizedAccessException)
            {
                MessageBox.Show("Нет прав на запись файла в папку приложения.", "Ошибка сохранения", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (IOException ex)
            {
                MessageBox.Show($"Ошибка ввода-вывода при сохранении: {ex.Message}", "Ошибка сохранения", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Произошла непредвиденная ошибка при сохранении: {ex.Message}", "Ошибка сохранения", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Загрузка списка книг из XML. Возвращает список книг для _allLoadedBooks.
        // Побочные эффекты (TxtBoxPath, comboBoxGenres, TextBoxObrabotano, TextBoxFinded)
        // остаются — Form1 сам решит, что с ними делать.
        // Загрузка списка книг из XML в ListView.
        public List<BookInfo> LoadList(string myFile)
        {
            var result = new List<BookInfo>();

            try
            {
                if (!File.Exists(myFile))
                {
                    MessageBox.Show("Файл не найден!", "Загрузка списка",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return result;
                }

                XDocument doc = XDocument.Load(myFile);

                if (doc.Root == null)
                {
                    MessageBox.Show("XML-файл поврежден или имеет неверный формат.",
                        "Ошибка загрузки", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return result;
                }

                // Чтение основных параметров
                lastFN = doc.Root.Element("last-file")?.Value ?? "";
                _frm.TxtBoxPath.Text = doc.Root.Element("dir")?.Value ?? "";
                _frm.comboBoxGenres.Text = doc.Root.Element("grupp")?.Value ?? "";
                _frm.TextBoxObrabotano.Text = doc.Root.Element("obrabot")?.Value ?? "";
                _frm.TextBoxFinded.Text = doc.Root.Element("find")?.Value ?? "";

                string baseDir = _frm.TxtBoxPath.Text;

                // Загрузка книг
                foreach (XElement book in doc.Root.Elements("book"))
                {
                    string nameFile = book.Element("nameFile")?.Value ?? "";
                    string author = book.Element("author")?.Value ?? "";
                    string seria = book.Element("seria")?.Value ?? "";
                    string bookName = book.Element("bookName")?.Value ?? "";
                    string genreName = book.Element("genre")?.Value ?? "";
                    string genreKod = book.Element("genreKod")?.Value ?? "";
                    string allCodesStr = book.Element("allGenreCodes")?.Value ?? "";
                    string razm = book.Element("razmerFile")?.Value ?? "";
                    string kodePage = book.Element("kod")?.Value ?? "";
                    string lang = book.Element("lang")?.Value ?? "";

                    var allCodes = new HashSet<string>(
                        allCodesStr.Split(',')
                                   .Select(s => s.Trim())
                                   .Where(s => s.Length > 0),
                        StringComparer.OrdinalIgnoreCase);

                    var bookInfo = new BookInfo
                    {
                        FileName = nameFile,
                        FilePath = Path.Combine(baseDir, nameFile),
                        Author = author,
                        Seria = seria,
                        Title = bookName,
                        GenreName = genreName,
                        GenreKod = genreKod,
                        AllGenreCodes = allCodes,
                        Razmer = razm,
                        KodePage = kodePage,
                        Language = lang,
                        HasError = false
                    };

                    result.Add(bookInfo);
                }
            }
            catch (System.Xml.XmlException ex)
            {
                MessageBox.Show($"Ошибка чтения XML (файл поврежден): {ex.Message}",
                    "Ошибка загрузки", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (IOException ex)
            {
                MessageBox.Show($"Ошибка доступа к файлу: {ex.Message}",
                    "Ошибка загрузки", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Произошла непредвиденная ошибка при загрузке: {ex.Message}",
                    "Ошибка загрузки", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            return result;
        }
    }
}