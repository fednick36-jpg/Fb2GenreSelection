using System;

namespace Fb2GenreSelection
{
    /// <summary>
    /// Модель одной картинки из &lt;binary&gt; FB2.
    /// Используется формой картинок для отображения и правок.
    /// </summary>
    public class Fb2Image
    {
        /// <summary>Id картинки — атрибут id у &lt;binary&gt;. Например "i_001.jpg".</summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>Content-type — атрибут content-type. Например "image/jpeg".</summary>
        public string ContentType { get; set; } = string.Empty;

        /// <summary>Байты картинки (декодированные из base64).</summary>
        public byte[] Data { get; set; }

        /// <summary>Ширина в пикселях (0, если не удалось определить).</summary>
        public int Width { get; set; }

        /// <summary>Высота в пикселях (0, если не удалось определить).</summary>
        public int Height { get; set; }

        /// <summary>Короткий формат: "jpg", "png", "gif" — из content-type.</summary>
        public string Format { get; set; } = string.Empty;

        /// <summary>Сколько &lt;image&gt; ссылается на этот id (0 = не используется).</summary>
        public int RefCount { get; set; }

        /// <summary>true, если на картинку ссылается &lt;coverpage&gt;.</summary>
        public bool IsCover { get; set; }

        /// <summary>SHA1-хэш байтов — для поиска дубликатов (используем позже).</summary>
        public string Hash { get; set; } = string.Empty;

        // === Вычисляемые свойства для UI ===

        /// <summary>Размер в байтах.</summary>
        public long SizeBytes => Data?.Length ?? 0;

        /// <summary>Есть ли ссылки на картинку.</summary>
        public bool IsReferenced => RefCount > 0;

        /// <summary>Статус для отображения в списке.</summary>
        public string Status =>
            IsCover ? "Обложка" :
            !IsReferenced ? "Не используется" :
            "Используется";

        /// <summary>Размер в удобном виде: "8 B", "53.5 KB", "1.20 MB".</summary>
        public string SizeDisplay
        {
            get
            {
                long size = SizeBytes;
                if (size < 1024) return $"{size}";
                if (size < 1024 * 1024) return $"{size / 1024.0:F2}";
                return $"{size / (1024.0 * 1024.0):F2}";
            }
        }

        /// <summary>Размеры в виде "380×400" или "—", если неизвестны.</summary>
        public string Dimensions =>
            Width > 0 && Height > 0 ? $"{Width}×{Height}" : "—";
    }
}