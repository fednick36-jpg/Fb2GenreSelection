using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Xml.Linq;

namespace Fb2GenreSelection
{
    /// <summary>
    /// Сервис чтения и правки картинок (&lt;binary&gt;) в XDocument.
    /// Все операции — в памяти. На диск пишет Fb2DocumentCache.
    /// </summary>
    public static class Fb2ImageService
    {
        private static readonly XNamespace Ns = "http://www.gribuser.ru/xml/fictionbook/2.0";
        private static readonly XNamespace Xlink = "http://www.w3.org/1999/xlink";

        // ============================================================
        //  ЧТЕНИЕ
        // ============================================================

        /// <summary>
        /// Загрузить все картинки из документа: &lt;binary&gt; + подсчёт ссылок &lt;image&gt;.
        /// </summary>
        public static List<Fb2Image> LoadAll(XDocument doc)
        {
            var result = new List<Fb2Image>();
            if (doc?.Root == null) return result;

            // 1. Узнать id обложки, чтобы пометить её
            string coverId = GetCoverId(doc);

            // 2. Собрать все <binary> в список + словарь по id (для быстрого поиска)
            var binaryNodes = doc.Root.Elements(Ns + "binary").ToList();
            var byId = new Dictionary<string, Fb2Image>(StringComparer.OrdinalIgnoreCase);

            foreach (var bin in binaryNodes)
            {
                string id = (string)bin.Attribute("id");
                if (string.IsNullOrEmpty(id)) continue;

                var img = new Fb2Image
                {
                    Id = id,
                    ContentType = (string)bin.Attribute("content-type") ?? string.Empty,
                    IsCover = string.Equals(id, coverId, StringComparison.OrdinalIgnoreCase)
                };

                img.Format = ExtractFormat(img.ContentType);

                // Декодировать base64 + измерить + посчитать хэш
                try
                {
                    string base64 = bin.Value?.Trim() ?? string.Empty;
                    img.Data = Convert.FromBase64String(base64);

                    using (var ms = new MemoryStream(img.Data))
                    using (var bitmap = Image.FromStream(ms,
                        useEmbeddedColorManagement: false,
                        validateImageData: false))
                    {
                        img.Width = bitmap.Width;
                        img.Height = bitmap.Height;
                    }

                    using (var sha = SHA1.Create())
                    {
                        img.Hash = Convert.ToBase64String(sha.ComputeHash(img.Data));
                    }
                }
                catch
                {
                    // Битый base64 или битая картинка — оставляем пустые поля
                    img.Data = null;
                    img.Width = 0;
                    img.Height = 0;
                }

                result.Add(img);
                byId[id] = img;
            }

            // 3. Пройтись по всем <image> и увеличить RefCount
            //    Descendants — потому что <image> может быть не только в <body>,
            //    но и в <coverpage>, <annotation> и т.д. Ссылка внутри <coverpage>
            //    тоже считается — поэтому у обложки RefCount >= 1 всегда, а если
            //    RefCount > 1, значит она ещё и продублирована где-то в тексте.
            foreach (var imageNode in doc.Root.Descendants(Ns + "image"))
            {
                string href = GetHref(imageNode);
                if (string.IsNullOrEmpty(href)) continue;

                string refId = href.TrimStart('#');
                if (byId.TryGetValue(refId, out var target))
                {
                    target.RefCount++;
                }
            }

            return result;
        }

        /// <summary>
        /// Получить id обложки из &lt;coverpage&gt;. Пустая строка, если обложки нет.
        /// </summary>
        public static string GetCoverId(XDocument doc)
        {
            string href = GetHref(GetCoverImageNode(doc));
            if (string.IsNullOrEmpty(href)) return string.Empty;

            return href.TrimStart('#');
        }

        /// <summary>
        /// Найти сам узел &lt;image&gt; внутри &lt;coverpage&gt; (не строку id, а сам
        /// элемент) — нужен, чтобы отличить его от других &lt;image&gt; с тем же href
        /// при точечном удалении дублей.
        /// </summary>
        private static XElement GetCoverImageNode(XDocument doc)
        {
            if (doc?.Root == null) return null;

            var titleInfo = doc.Root
                .Element(Ns + "description")?
                .Element(Ns + "title-info");

            var coverpage = titleInfo?.Element(Ns + "coverpage");
            return coverpage?.Element(Ns + "image");
        }

        // ============================================================
        //  ПРАВКИ
        // ============================================================

        /// <summary>
        /// Удалить картинку: сам &lt;binary&gt; и все &lt;image&gt;, которые на неё ссылаются.
        /// </summary>
        public static void DeleteImage(XDocument doc, string id)
        {
            if (doc?.Root == null || string.IsNullOrEmpty(id)) return;

            // 1. Удалить все <image> с href="#id"
            var imagesToRemove = doc.Root
                .Descendants(Ns + "image")
                .Where(img =>
                {
                    string href = GetHref(img);
                    return !string.IsNullOrEmpty(href) &&
                           string.Equals(href.TrimStart('#'), id,
                               StringComparison.OrdinalIgnoreCase);
                })
                .ToList();

            foreach (var img in imagesToRemove)
            {
                img.Remove();
            }

            // 2. Удалить сам <binary> с этим id
            var binaryToRemove = doc.Root
                .Elements(Ns + "binary")
                .FirstOrDefault(b => string.Equals((string)b.Attribute("id"), id,
                    StringComparison.OrdinalIgnoreCase));

            binaryToRemove?.Remove();
        }

        /// <summary>
        /// Удалить несколько картинок сразу.
        /// </summary>
        public static void DeleteImages(XDocument doc, IEnumerable<string> ids)
        {
            if (doc?.Root == null || ids == null) return;

            // Копируем в список — иначе будем менять коллекцию во время обхода
            foreach (var id in ids.ToList())
            {
                DeleteImage(doc, id);
            }
        }

        /// <summary>
        /// Удалить только "лишние" ссылки &lt;image&gt; на id обложки, не трогая
        /// сам &lt;binary&gt; и ссылку внутри &lt;coverpage&gt;. Используется, когда
        /// одна и та же картинка одновременно является обложкой и продублирована
        /// в тексте книги (RefCount &gt; 1) — обложку в этом случае удалять нельзя,
        /// а лишние вхождения в теле убрать нужно.
        /// Возвращает количество удалённых ссылок.
        /// </summary>
        public static int DeleteExtraCoverReferences(XDocument doc, string coverId)
        {
            if (doc?.Root == null || string.IsNullOrEmpty(coverId)) return 0;

            var coverImageNode = GetCoverImageNode(doc);

            var extraNodes = doc.Root
                .Descendants(Ns + "image")
                .Where(imgNode =>
                {
                    if (ReferenceEquals(imgNode, coverImageNode)) return false; // саму ссылку в coverpage не трогаем

                    string href = GetHref(imgNode);
                    return !string.IsNullOrEmpty(href) &&
                           string.Equals(href.TrimStart('#'), coverId,
                               StringComparison.OrdinalIgnoreCase);
                })
                .ToList();

            foreach (var node in extraNodes)
            {
                node.Remove();
            }

            return extraNodes.Count;
        }

        /// <summary>
        /// Переименовать картинку: поменять id у &lt;binary&gt; и обновить все ссылки &lt;image&gt;.
        /// </summary>
        public static void RenameImage(XDocument doc, string oldId, string newId)
        {
            if (doc?.Root == null) return;
            if (string.IsNullOrEmpty(oldId) || string.IsNullOrEmpty(newId)) return;
            if (string.Equals(oldId, newId, StringComparison.OrdinalIgnoreCase)) return;

            // 1. Найти <binary> и поменять атрибут id
            var binaryNode = doc.Root
                .Elements(Ns + "binary")
                .FirstOrDefault(b => string.Equals((string)b.Attribute("id"), oldId,
                    StringComparison.OrdinalIgnoreCase));

            if (binaryNode == null) return;
            binaryNode.SetAttributeValue("id", newId);

            // 2. Обновить все <image>, ссылающиеся на старый id
            foreach (var imageNode in doc.Root.Descendants(Ns + "image"))
            {
                var hrefAttr = GetHrefAttribute(imageNode);
                if (hrefAttr == null) continue;

                string href = hrefAttr.Value;
                if (string.IsNullOrEmpty(href)) continue;

                if (string.Equals(href.TrimStart('#'), oldId,
                    StringComparison.OrdinalIgnoreCase))
                {
                    hrefAttr.Value = "#" + newId;
                }
            }
        }

        // ============================================================
        //  ВСПОМОГАТЕЛЬНЫЕ
        // ============================================================

        /// <summary>
        /// Прочитать href у &lt;image&gt;. FB2 использует XLink (l:href),
        /// но некоторые файлы пишут просто href — поддерживаем оба.
        /// </summary>
        private static string GetHref(XElement imageNode)
        {
            if (imageNode == null) return null;
            return GetHrefAttribute(imageNode)?.Value;
        }

        /// <summary>
        /// Найти атрибут href (XLink или простой). Возвращает сам атрибут,
        /// чтобы можно было не только прочитать, но и изменить.
        /// </summary>
        private static XAttribute GetHrefAttribute(XElement imageNode)
        {
            if (imageNode == null) return null;

            // 1. Попробовать XLink
            var xlinkAttr = imageNode.Attribute(Xlink + "href");
            if (xlinkAttr != null) return xlinkAttr;

            // 2. Попробовать простой href
            var plainAttr = imageNode.Attribute("href");
            if (plainAttr != null) return plainAttr;

            // 3. Универсальный поиск: любой атрибут с локальным именем "href"
            return imageNode.Attributes()
                .FirstOrDefault(a => a.Name.LocalName == "href");
        }

        /// <summary>
        /// Извлечь короткий формат из content-type: "image/jpeg" -> "jpg".
        /// </summary>
        private static string ExtractFormat(string contentType)
        {
            if (string.IsNullOrEmpty(contentType)) return string.Empty;

            string lower = contentType.ToLowerInvariant();

            if (lower.Contains("jpeg") || lower.Contains("jpg")) return "jpg";
            if (lower.Contains("png")) return "png";
            if (lower.Contains("gif")) return "gif";
            if (lower.Contains("bmp")) return "bmp";
            if (lower.Contains("webp")) return "webp";
            if (lower.Contains("svg")) return "svg";

            // Фоллбэк: часть после "/"
            int slash = lower.IndexOf('/');
            return slash >= 0 && slash < lower.Length - 1
                ? lower.Substring(slash + 1)
                : lower;
        }
    }
}
