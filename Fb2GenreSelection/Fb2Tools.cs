using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xml.Linq;
using System.IO;

namespace Fb2GenreSelection
{
    /// <summary>
    /// Статический класс для работы с содержимым fb2-файлов: жанры, авторы,
    /// название книги, серия (title-info) и издательская серия (publish-info).
    /// Не зависит от формы, кроме одного MessageBox.Show в DeleteGenre —
    /// работает напрямую с XML файла на диске. Форма после каждого Process...
    /// сама решает, обновлять ли UI (см. GenreButtonHandlers.cs / AuthorButtonHandlers.cs /
    /// TitleSeriesButtonHandlers.cs / SeriaIsdatButtonHandler.cs — там для этого заново
    /// парсится файл через Fb2ParserService.ParseBook и вызывается BindBookToControls).
    /// </summary>
    public static class Fb2Tools
    {
        // Единственное объявление пространства имён fb2 на весь класс —
        // раньше было два одинаковых поля (Ns для жанров, FbNs для авторов), свели в одно.
        private static readonly XNamespace FbNs = "http://www.gribuser.ru/xml/fictionbook/2.0";

        // ===================== Жанры =====================

        public enum GenreOperation
        {
            Delete,
            Rename,
            MoveToFirst,
            Add,
            DeleteProchie
        }

        /// <summary>
        /// Открывает fb2-файл, выполняет операцию над жанрами и сохраняет файл,
        /// если были изменения. Возвращает true, если файл был изменён и сохранён.
        /// </summary>
        public static bool ProcessGenres(Fb2DocumentCache cache, string fileName,
                    GenreOperation operation, string genreCode, string newGenreCode = "")
        {
            XDocument doc = cache.Open(fileName);

            var genreElements = doc.Root
                ?.Element(FbNs + "description")
                ?.Element(FbNs + "title-info")
                ?.Elements(FbNs + "genre")
                .ToList();

            if (genreElements == null || genreElements.Count == 0) return false;

            bool isChanged = operation switch
            {
                GenreOperation.Delete => DeleteGenre(genreElements, genreCode),
                GenreOperation.Rename => RenameGenre(genreElements, genreCode, newGenreCode),
                GenreOperation.MoveToFirst => MoveGenreToFirst(genreElements, genreCode),
                GenreOperation.Add => AddGenre(genreElements, genreCode),
                _ => false
            };

            if (!isChanged) return false;

            cache.MarkDirty();   // ← было doc.Save(fileName)
            return true;
        }

        private static bool RenameGenre(List<XElement> genreList, string oldCode, string newCode)
        {
            var target = genreList.FirstOrDefault(e => e.Value == oldCode);
            if (target == null) return false;

            target.Value = newCode;
            return true;
        }

        private static bool DeleteGenre(List<XElement> genreList, string delCode)
        {
            if (genreList.Count <= 1) return false;   // нельзя удалять последний

            var target = genreList.FirstOrDefault(e => e.Value == delCode);
            if (target == null) return false;

            target.Remove();
            return true;
        }

        private static bool MoveGenreToFirst(List<XElement> genreList, string code)
        {
            var target = genreList.FirstOrDefault(e => e.Value == code);
            if (target == null || target == genreList.First()) return false;

            target.Remove();
            genreList.First().AddBeforeSelf(target);
            return true;
        }

        private static bool AddGenre(List<XElement> genreList, string code)
        {
            if (genreList.Any(e => e.Value == code)) return false;

            genreList.Last().AddAfterSelf(new XElement(FbNs + "genre", code));
            return true;
        }

        // ===================== Авторы =====================

        public enum AuthorOperation
        {
            Edit,
            Add,
            Delete,
            MoveUp
        }

        /// <summary>
        /// Выполняет операцию над авторами книги (fb2) и сохраняет файл.
        /// index используется для Edit/Delete/MoveUp (позиция автора в документе,
        /// совпадает с индексом строки в DGVAuthors, т.к. doc.Authors строится в том же порядке).
        /// firstName/middleName/lastName — новые значения для Edit/Add.
        /// Возвращает true, если файл был изменён.
        /// </summary>
        public static bool ProcessAuthors(Fb2DocumentCache cache, string fileName, AuthorOperation operation,
            int index = -1, string firstName = null, string middleName = null, string lastName = null)
        {
            var doc = cache.Open(fileName);
            bool isChanged;

            switch (operation)
            {
                case AuthorOperation.Edit:
                    isChanged = EditAuthor(doc, index, firstName, middleName, lastName);
                    break;
                case AuthorOperation.Add:
                    isChanged = AddAuthor(doc, firstName, middleName, lastName);
                    break;
                case AuthorOperation.Delete:
                    isChanged = DeleteAuthor(doc, index);
                    break;
                case AuthorOperation.MoveUp:
                    isChanged = MoveAuthorUp(doc, index);
                    break;
                default:
                    isChanged = false;
                    break;
            }

            if (isChanged) cache.MarkDirty();   // ← было doc.Save(fileName)
            return isChanged;
        }

        private static XElement CreateAuthorElement(string firstName, string middleName, string lastName)
        {
            var author = new XElement(FbNs + "author");
            if (!string.IsNullOrWhiteSpace(firstName)) author.Add(new XElement(FbNs + "first-name", firstName.Trim()));
            if (!string.IsNullOrWhiteSpace(middleName)) author.Add(new XElement(FbNs + "middle-name", middleName.Trim()));
            if (!string.IsNullOrWhiteSpace(lastName)) author.Add(new XElement(FbNs + "last-name", lastName.Trim()));
            return author;
        }

        private static bool EditAuthor(XDocument doc, int index, string firstName, string middleName, string lastName)
        {
            var authors = doc.Descendants(FbNs + "author").ToList();
            if (index < 0 || index >= authors.Count) return false;
            authors[index].ReplaceWith(CreateAuthorElement(firstName, middleName, lastName));
            return true;
        }

        private static bool AddAuthor(XDocument doc, string firstName, string middleName, string lastName)
        {
            var titleInfo = doc.Descendants(FbNs + "title-info").FirstOrDefault();
            var bookTitle = titleInfo?.Element(FbNs + "book-title");
            if (bookTitle == null) return false;
            bookTitle.AddBeforeSelf(CreateAuthorElement(firstName, middleName, lastName));
            return true;
        }

        private static bool DeleteAuthor(XDocument doc, int index)
        {
            var authors = doc.Descendants(FbNs + "author").ToList();
            if (index < 0 || index >= authors.Count) return false;
            authors[index].Remove();
            return true;
        }

        private static bool MoveAuthorUp(XDocument doc, int index)
        {
            var authors = doc.Descendants(FbNs + "author").ToList();
            if (index <= 0 || index >= authors.Count) return false;

            var target = authors[index];
            target.Remove();
            authors[index - 1].AddBeforeSelf(target);
            return true;
        }

        // ===================== Название книги =====================

        /// <summary>
        /// Изменяет (или создаёт, если отсутствует) book-title в title-info.
        /// Возвращает true, если файл был изменён.
        /// </summary>
        public static bool ProcessTitle(Fb2DocumentCache cache, string fileName, string newTitle)
        {
            var doc = cache.Open(fileName);

            var titleInfo = doc.Descendants(FbNs + "title-info").FirstOrDefault();
            if (titleInfo == null) return false;

            var bookTitle = titleInfo.Element(FbNs + "book-title");
            if (bookTitle != null)
            {
                bookTitle.Value = newTitle ?? string.Empty;
            }
            else
            {
                var anchor = titleInfo.Element(FbNs + "annotation")
                             ?? titleInfo.Element(FbNs + "date");

                var newBookTitle = new XElement(FbNs + "book-title", newTitle ?? string.Empty);

                if (anchor != null)
                    anchor.AddBeforeSelf(newBookTitle);
                else
                    titleInfo.Add(newBookTitle);
            }

            cache.MarkDirty();   // ← было doc.Save(fileName)
            return true;
        }

        // ===================== Серия (title-info/sequence) =====================

        public enum SeriesOperation
        {
            Add,
            Edit,
            Delete
        }

        /// <summary>
        /// Добавляет/изменяет/удаляет серию (узел sequence в title-info).
        /// Правила: если seriesName пусто — серия удаляется целиком (без номера).
        /// Если seriesNumber пусто, а имя задано — атрибут number очищается/убирается.
        /// Возвращает true, если файл был изменён.
        /// </summary>
        public static bool ProcessSeries(Fb2DocumentCache cache, string fileName, SeriesOperation operation,
            string seriesName = null, string seriesNumber = null)
        {
            var doc = cache.Open(fileName);
            bool isChanged;

            switch (operation)
            {
                case SeriesOperation.Add:
                case SeriesOperation.Edit:
                    isChanged = SetSeries(doc, seriesName, seriesNumber);
                    break;
                case SeriesOperation.Delete:
                    isChanged = DeleteSeries(doc);
                    break;
                default:
                    isChanged = false;
                    break;
            }

            if (isChanged) cache.MarkDirty();   // ← было doc.Save(fileName)
            return isChanged;
        }

        private static bool SetSeries(XDocument doc, string seriesName, string seriesNumber)
        {
            var titleInfo = doc.Descendants(FbNs + "title-info").FirstOrDefault();
            if (titleInfo == null) return false;

            var sequence = titleInfo.Element(FbNs + "sequence");

            if (string.IsNullOrWhiteSpace(seriesName))
            {
                if (sequence == null) return false;
                sequence.Remove();
                return true;
            }

            if (sequence == null)
            {
                sequence = new XElement(FbNs + "sequence");
                titleInfo.Add(sequence);
            }

            sequence.SetAttributeValue("name", seriesName.Trim());

            if (!string.IsNullOrWhiteSpace(seriesNumber))
                sequence.SetAttributeValue("number", seriesNumber.Trim());
            else
                sequence.SetAttributeValue("number", null);

            return true;
        }

        private static bool DeleteSeries(XDocument doc)
        {
            var titleInfo = doc.Descendants(FbNs + "title-info").FirstOrDefault();
            var sequence = titleInfo?.Element(FbNs + "sequence");
            if (sequence == null) return false;

            sequence.Remove();
            return true;
        }

        // ===================== Язык =====================

        /// <summary>
        /// Устанавливает &lt;lang&gt;ru&lt;/lang&gt; в title-info.
        /// Если узел уже есть — перезаписывает значение.
        /// Если узла нет — создаёт после coverpage (или перед src-lang/sequence,
        /// если coverpage отсутствует).
        /// Возвращает true, если документ был изменён.
        /// Файл на диск не пишет — только cache.MarkDirty(),
        /// сохранение произойдёт по обычной схеме Fb2DocumentCache.
        /// </summary>
        public static bool SetLanguageRu(Fb2DocumentCache cache, string fileName)
        {
            var doc = cache.Open(fileName);

            var titleInfo = doc.Descendants(FbNs + "title-info").FirstOrDefault();
            if (titleInfo == null) return false;

            var langNode = titleInfo.Element(FbNs + "lang");

            if (langNode != null)
            {
                if (langNode.Value == "ru") return false;   // уже ru — ничего не делаем
                langNode.Value = "ru";
            }
            else
            {
                var newLang = new XElement(FbNs + "lang", "ru");

                // Приоритет — после coverpage
                var coverpage = titleInfo.Element(FbNs + "coverpage");
                if (coverpage != null)
                {
                    coverpage.AddAfterSelf(newLang);
                }
                else
                {
                    // Иначе — перед src-lang или sequence
                    var anchor = titleInfo.Element(FbNs + "src-lang")
                                 ?? titleInfo.Element(FbNs + "sequence");

                    if (anchor != null)
                        anchor.AddBeforeSelf(newLang);
                    else
                        titleInfo.Add(newLang);
                }
            }

            cache.MarkDirty();
            return true;
        }

        // ===================== Издательская серия (publish-info/sequence) =====================

        /// <summary>
        /// Удаляет издательскую серию (publish-info/sequence), если она есть.
        /// Возвращает true, если узел был найден и удалён.
        /// </summary>
        public static bool ClearSeriesIsdat(string fileName)
        {
            var doc = XDocument.Load(fileName);

            var publishInfo = doc.Descendants(FbNs + "publish-info").FirstOrDefault();
            var publishsequence = publishInfo?.Element(FbNs + "publisher");
            if (publishsequence == null) return false;

            publishsequence.Remove();
            doc.Save(fileName);
            return true;
        }

        // ===================== Определение языка =====================

        /// <summary>
        /// Определяет язык книги по первым ~5000 символам текста из &lt;body&gt;.
        /// Возвращает "ru", если текст похож на русский, иначе пустую строку.
        /// Файл не изменяет, кэш не трогает — чистая функция анализа.
        /// </summary>
        public static string DetectLanguage(string fileName)
        {
            try
            {
                string sample = ReadBodySample(fileName, 5000);
                return IsRussian(sample) ? "ru" : string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Читает файл в нужной кодировке и возвращает первые maxChars символов
        /// текста из первого &lt;body&gt;, без XML-тегов.
        /// </summary>
        private static string ReadBodySample(string filePath, int maxChars)
        {
            string encName = BookInfo.GetFb2Encoding(filePath);
            Encoding enc = encName == "utf-8"
                ? Encoding.UTF8
                : Encoding.GetEncoding(1251);

            string text;
            try { text = File.ReadAllText(filePath, enc); }
            catch { return string.Empty; }

            // Первое вхождение <body ...> ... </body>
            int bodyStart = text.IndexOf("<body", StringComparison.OrdinalIgnoreCase);
            if (bodyStart < 0) return string.Empty;
            int bodyEnd = text.IndexOf("</body>", bodyStart, StringComparison.OrdinalIgnoreCase);
            if (bodyEnd < 0) bodyEnd = text.Length;

            string body = text.Substring(bodyStart, bodyEnd - bodyStart);

            var sb = new StringBuilder(maxChars);
            bool inTag = false;
            foreach (char ch in body)
            {
                if (ch == '<') { inTag = true; continue; }
                if (ch == '>') { inTag = false; continue; }
                if (inTag) continue;

                sb.Append(ch);
                if (sb.Length >= maxChars) break;
            }
            return sb.ToString();
        }

        /// <summary>
        /// Эвристика «русский / не русский» по соотношению русских букв и латиницы
        /// с отсечкой по маркерам других славянских языков (укр/бел).
        /// </summary>
        private static bool IsRussian(string sample)
        {
            if (string.IsNullOrWhiteSpace(sample)) return false;

            int r = 0, nr = 0, other = 0;

            foreach (char ch in sample)
            {
                int code = (int)ch;

                // Маркеры других славянских: І і Ї ї Є є Ґ ґ Ў ў
                switch (code)
                {
                    case 1030:
                    case 1110:
                    case 1031:
                    case 1111:
                    case 1028:
                    case 1108:
                    case 1168:
                    case 1169:
                    case 1038:
                    case 1118:
                        other++;
                        continue;
                }

                // Русские: А-Я (1040-1071), а-я (1072-1103), Ё (1025), ё (1105)
                if ((code >= 1040 && code <= 1103) || code == 1025 || code == 1105)
                {
                    r++;
                }
                // Латиница: A-Z (65-90), a-z (97-122)
                else if ((code >= 65 && code <= 90) || (code >= 97 && code <= 122))
                {
                    nr++;
                }
            }

            if (other > 20) return false;   // слишком много украинских/белорусских маркеров
            return r >= nr;                 // русский, если русских букв не меньше латиницы
        }

    }
}
