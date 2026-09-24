using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace Fb2GenreSelection
{
    public class BookInfo
    {
        // === ПОЛЯ/СВОЙСТВА (С сохраненными именами) ===
        public string FilePath { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string Author { get; set; } = "Неизвестен";
        public string Seria { get; set; } = string.Empty;
        public string Title { get; set; } = "Без названия";

        public string GenreKod { get; set; } = string.Empty;
        public string GenreName { get; set; } = string.Empty;
        public string Razmer { get; set; } = "0 KB";
        public string KodePage { get; set; } = "Unknown";
        public string Language { get; set; } = "Не указан";

        // Алиасы для новых модулей
        public string PrimaryGenreCode => GenreKod;
        public string PrimaryGenreName => GenreName;
        public string SizeKb => Razmer;
        public string EncodingName => KodePage;

        public HashSet<string> AllGenreCodes { get; set; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        public bool HasError { get; set; } = false;
        private static readonly XNamespace ns = "http://www.gribuser.ru/xml/fictionbook/2.0";

        /// <summary>
        /// Парсит отдельный FB2 файл (Метод объекта bookInfo.ParseFb2)
        /// </summary>
        public static BookInfo ParseFb2(string filePath)
        {
            if (!File.Exists(filePath))
            {
                return new BookInfo
                {
                    FilePath = filePath,
                    FileName = Path.GetFileName(filePath),
                    Author = "Файл не найден",
                    HasError = true
                };
            }

            //XNamespace ns = "http://www.gribuser.ru/xml/fictionbook/2.0";
            var doc = XDocument.Load(filePath);
            var titleInfo = doc.Root?.Element(ns + "description")?.Element(ns + "title-info");

            // 1. Автор
            string author = "Неизвестен";
            var firstAuthor = titleInfo?.Element(ns + "author");
            if (firstAuthor != null)
            {
                string fName = firstAuthor.Element(ns + "first-name")?.Value?.Trim();
                string lName = firstAuthor.Element(ns + "last-name")?.Value?.Trim();
                author = !string.IsNullOrEmpty(lName) ? $"{lName} {fName}".Trim() : fName ?? "Неизвестен";
            }

            // 2. Серия
            string seria = string.Empty;
            var sequence = titleInfo?.Element(ns + "sequence");
            if (sequence != null)
            {
                string sName = sequence.Attribute("name")?.Value ?? string.Empty;
                string sNum = sequence.Attribute("number")?.Value ?? string.Empty;
                seria = string.IsNullOrEmpty(sNum) ? sName : $"{sName} [{sNum}]";
            }

            // 3. Жанры
            var genreElements = titleInfo?.Elements(ns + "genre")
                .Select(g => g.Value.Trim())
                .Where(g => !string.IsNullOrEmpty(g))
                .ToList() ?? new List<string>();

            string firstGenreCode = genreElements.FirstOrDefault() ?? string.Empty;

            // 4. Язык книги (<lang>)
            string lang = titleInfo?.Element(ns + "lang")?.Value?.Trim();
            if (string.IsNullOrEmpty(lang)) lang = "";

            double kb = new FileInfo(filePath).Length / 1024.0;

            return new BookInfo
            {
                FilePath = filePath,
                FileName = Path.GetFileName(filePath),
                Author = author,
                Seria = seria,
                Title = titleInfo?.Element(ns + "book-title")?.Value ?? "Без названия",
                GenreKod = firstGenreCode,
                GenreName = GenreData.GetGenreName(firstGenreCode),
                AllGenreCodes = new HashSet<string>(genreElements, StringComparer.OrdinalIgnoreCase),
                Razmer = $"{kb:F2}",
                KodePage = GetFb2Encoding(filePath),
                Language = lang, // <-- ВАЖНО: Добавлено присвоение языка!
                HasError = false
            };
        }

        /// <summary>
        /// Сканирует папку и возвращает список книг с поддержкой индикатора Progress
        /// (Метод объекта bookInfo.GetBooksFromFolder)
        /// </summary>
        public static List<BookInfo> GetBooksFromFolder(string folderPath, IProgress<(int Current, int Total)> progress = null)
        {
            var books = new List<BookInfo>();
            if (!Directory.Exists(folderPath)) return books;

            var files = Directory.GetFiles(folderPath, "*.fb2");
            int totalFiles = files.Length;

            for (int i = 0; i < totalFiles; i++)
            {
                string file = files[i];
                try
                {
                    var book = ParseFb2(file);
                    if (string.IsNullOrEmpty(book.Language))
                        book.Language = Fb2Tools.DetectLanguage(file);
                    books.Add(book);
                }
                catch (Exception ex)
                {
                    double kb = new FileInfo(file).Length / 1024.0;
                    books.Add(new BookInfo
                    {
                        FilePath = file,
                        FileName = Path.GetFileName(file),
                        Author = "Ошибка парсинга",
                        Title = ex.Message,
                        GenreName = "Ошибка",
                        HasError = true,
                        Razmer = $"{kb:F2}",
                        KodePage = "Unknown",
                        Language = "Unknown"
                    });
                }

                // Уведомление UI о прогрессе
                progress?.Report((i + 1, totalFiles));
            }

            return books;
        }

        // Статическая альтернатива для обратной совместимости
        //public static List<BookInfo> ScanFolder(string folderPath)
        //{
        //    var instance = new BookInfo();
        //    return instance.GetBooksFromFolder(folderPath);
        //}

        public static string GetFb2Encoding(string filePath)
        {
            using (var reader = new StreamReader(filePath, Encoding.Default))
            {
                for (int i = 0; i < 3; i++)
                {
                    string line = reader.ReadLine();
                    if (line == null) break;
                    if (line.IndexOf("encoding=\"utf-8\"", StringComparison.OrdinalIgnoreCase) >= 0) return "utf-8";
                    if (line.IndexOf("encoding=\"windows-1251\"", StringComparison.OrdinalIgnoreCase) >= 0) return "Win-1251";
                }
            }
            return "Unknown";
        }
    }
}
