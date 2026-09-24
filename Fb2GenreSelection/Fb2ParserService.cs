using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xml.Linq;

namespace Fb2GenreSelection
{
    // Модель автора
    public class AuthorItem
    {
        public string FirstName { get; set; } = string.Empty;
        public string MiddleName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Nickname { get; set; } = string.Empty;
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;

        public string FullName
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(LastName))
                    return $"{LastName} {FirstName} {MiddleName}".Trim();
                if (!string.IsNullOrWhiteSpace(Nickname))
                    return Nickname;
                return $"{FirstName} {MiddleName}".Trim();
            }
        }
    }

    // Модель жанра
    public class GenreItem
    {
        // ПЕРЕИМЕНОВАНО: Code/Name -> GenreCode/GenreName. Form1.Form1_Load привязывает
        // колонки DGVGenres через DataPropertyName = "GenreCode"/"GenreName" — под старые
        // имена свойств биндинг DataGridView просто не находил бы данные (пустые колонки,
        // без исключения на этапе компиляции — тихий баг, а не ошибка сборки).
        public string GenreCode { get; set; } = string.Empty;
        public string GenreName { get; set; } = string.Empty;
        public int MatchPercent { get; set; }
    }

    // Полный документ
    public class Fb2FullDocument
    {
        public string FilePath { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string SeriesName { get; set; } = string.Empty;
        public string SeriesNumber { get; set; } = string.Empty;
        public string PublisherSeries { get; set; } = string.Empty;
        public string AnnotationHtml { get; set; } = string.Empty;
        public string HistoryHtml { get; set; } = string.Empty;

        public string CoverFileName { get; set; } = string.Empty;
        public string CoverImageDimensions { get; set; } = "0x0";
        public byte[] CoverImageBytes { get; set; }
        public int ImageCount { get; set; }

        // ДОБАВЛЕНО: Form1.BindBookToControls обращается к doc.BodyPreviewHtml напрямую
        // при сборке HTML для webBrowser1 — раньше это был локальный string внутри
        // приватного BindToUI и наружу не отдавался вообще, то есть Form1 просто не
        // скомпилировался бы.
        public string BodyPreviewHtml { get; set; } = string.Empty;

        public List<AuthorItem> Authors { get; set; } = new List<AuthorItem>();
        public List<GenreItem> Genres { get; set; } = new List<GenreItem>();

        public string FirstAuthorFormatted
        {
            get
            {
                var first = Authors.FirstOrDefault();
                if (first == null) return "Неизвестен";
                return string.IsNullOrWhiteSpace(first.LastName)
                    ? first.FirstName
                    : $"{first.LastName} {first.FirstName}".Trim();
            }
        }

        public string NewFileName
        {
            get
            {
                string seriaPart = string.IsNullOrWhiteSpace(SeriesName)
                    ? string.Empty
                    : string.IsNullOrWhiteSpace(SeriesNumber) ? $"_{SeriesName}" : $"_{SeriesName} [{SeriesNumber}]";

                string genrePart = Genres.Count > 0 ? $"_{Genres[0].GenreName}" : string.Empty;

                return $"{FirstAuthorFormatted}{seriaPart}_{Title}{genrePart}.fb2";
            }
        }
    }

    // Сервис парсинга и связывания с интерфейсом
    public static class Fb2ParserService
    {
        private static readonly XNamespace Ns = "http://www.gribuser.ru/xml/fictionbook/2.0";
        private static readonly XNamespace Xlink = "http://www.w3.org/1999/xlink";

        // ГЛАВНОЕ ИЗМЕНЕНИЕ: новый Form1 (Fb2GenreSelection) вызывает
        // Fb2ParserService.ParseBook(filePath) и сам раскладывает результат по контролам
        // Возвращает полную модель книги или null, если файл не найден.
        // Form1 сам раскладывает результат по контролам через BindBookToControls.
        // Возвращает null, если файл не найден — Form1.listView1_SelectedIndexChanged
        // проверяет "if (doc == null) return;".

        public static Fb2FullDocument ParseBook(string filePath)
        {
            if (!File.Exists(filePath)) return null;

            var xdoc = XDocument.Load(filePath);
            return ParseFromXDocument(xdoc, filePath);
        }

        public static Fb2FullDocument ParseFromXDocument(XDocument xdoc, string filePath)
        {
            var root = xdoc.Root;
            if (root == null) return null;

            var description = root.Element(Ns + "description");
            var titleInfo = description?.Element(Ns + "title-info");
            var documentInfo = description?.Element(Ns + "document-info");
            var publishInfo = description?.Element(Ns + "publish-info");

            var binaryNodes = root.Elements(Ns + "binary").ToList();

            var doc = new Fb2FullDocument
            {
                FilePath = filePath,
                Title = titleInfo?.Element(Ns + "book-title")?.Value?.Trim() ?? string.Empty,
                AnnotationHtml = titleInfo?.Element(Ns + "annotation")?.ToString() ?? string.Empty,
                HistoryHtml = documentInfo?.Element(Ns + "history")?.ToString() ?? string.Empty,
                PublisherSeries = publishInfo?.Element(Ns + "publisher")?.Value?.Trim() ?? string.Empty,
                ImageCount = binaryNodes.Count
            };

            // 1. Серия
            var sequence = titleInfo?.Element(Ns + "sequence");
            if (sequence != null)
            {
                doc.SeriesName = sequence.Attribute("name")?.Value?.Trim() ?? string.Empty;
                doc.SeriesNumber = sequence.Attribute("number")?.Value?.Trim() ?? string.Empty;
            }

            // 2. Авторы + 3. Жанры
            if (titleInfo != null)
            {
                foreach (var authorNode in titleInfo.Elements(Ns + "author"))
                {
                    doc.Authors.Add(new AuthorItem
                    {
                        FirstName = authorNode.Element(Ns + "first-name")?.Value?.Trim() ?? string.Empty,
                        MiddleName = authorNode.Element(Ns + "middle-name")?.Value?.Trim() ?? string.Empty,
                        LastName = authorNode.Element(Ns + "last-name")?.Value?.Trim() ?? string.Empty,
                        Nickname = authorNode.Element(Ns + "nickname")?.Value?.Trim() ?? string.Empty,
                        Id = authorNode.Element(Ns + "id")?.Value?.Trim() ?? string.Empty,
                        Email = authorNode.Element(Ns + "email")?.Value?.Trim() ?? string.Empty
                    });
                }

                foreach (var genreNode in titleInfo.Elements(Ns + "genre"))
                {
                    string code = genreNode.Value?.Trim() ?? string.Empty;
                    if (string.IsNullOrEmpty(code)) continue;

                    int match = 0;
                    var matchAttr = genreNode.Attribute("match");
                    if (matchAttr != null) int.TryParse(matchAttr.Value, out match);

                    doc.Genres.Add(new GenreItem
                    {
                        GenreCode = code,
                        GenreName = GenreData.GetGenreName(code),
                        MatchPercent = match
                    });
                }
            }

            // 4. Обложка
            ExtractCoverData(binaryNodes, titleInfo, doc);

            // 5. Превью текста книги
            doc.BodyPreviewHtml = ExtractBodyPreview(root, 45);

            return doc;
        }

        private static void ExtractCoverData(List<XElement> binaryNodes, XElement titleInfo, Fb2FullDocument target)
        {
            var coverpage = titleInfo?.Element(Ns + "coverpage");
            var image = coverpage?.Element(Ns + "image");
            string href = image?.Attribute(Xlink + "href")?.Value;

            if (string.IsNullOrEmpty(href)) return;

            string binaryId = href.TrimStart('#');
            target.CoverFileName = binaryId;

            var binaryNode = binaryNodes
                .FirstOrDefault(b => string.Equals((string)b.Attribute("id"), binaryId, StringComparison.OrdinalIgnoreCase));

            if (binaryNode == null) return;

            try
            {
                target.CoverImageBytes = Convert.FromBase64String(binaryNode.Value.Trim());

                // ОПТИМИЗАЦИЯ 2: useEmbeddedColorManagement:false, validateImageData:false —
                // нам нужны только Width/Height, полная валидация/цветоуправление тут лишние
                // и на больших обложках заметно медленнее.
                using (var ms = new MemoryStream(target.CoverImageBytes))
                using (var img = Image.FromStream(ms, useEmbeddedColorManagement: false, validateImageData: false))
                {
                    target.CoverImageDimensions = $"{img.Width}x{img.Height}";
                }
            }
            catch
            {
                target.CoverImageBytes = null;
                target.CoverImageDimensions = "Ошибка";
            }
        }

        private static string ExtractBodyPreview(XElement root, int maxParagraphs = 45)
        {
            var bodyNode = root.Element(Ns + "body");
            if (bodyNode == null) return string.Empty;

            // Небольшой запас ёмкости — обычно превью укладывается в несколько КБ,
            // это избавляет StringBuilder от пары ранних переаллокаций.
            var sb = new StringBuilder(4096);
            int count = 0;

            foreach (var pNode in bodyNode.Descendants(Ns + "p"))
            {
                string text = pNode.Value?.Trim();
                if (string.IsNullOrWhiteSpace(text)) continue;

                sb.Append("<p style='margin: 0 0 6px 0; line-height: 1.3;'>")
                  .Append(System.Net.WebUtility.HtmlEncode(text))
                  .Append("</p>");

                count++;
                if (count >= maxParagraphs) break; // раннее прерывание — уже было в исходнике, это правильно
            }

            return sb.ToString();
        }

    }
}
