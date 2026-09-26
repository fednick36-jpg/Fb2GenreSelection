using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Fb2GenreSelection
{
    public static class GenreData
    {
        // 1. Изменили тип кортежа на 3 элемента: Name, Code, Abrev
        public static Dictionary<string, List<(string Name, string Code, string Abrev)>> genreMap = new();

        // Обратный индекс code → name для быстрого GetGenreName
        private static readonly Dictionary<string, string> _codeToName
            = new(StringComparer.OrdinalIgnoreCase);

        public static void LoadGenres(string xmlPath)
        {
            XmlDocument doc = new();
            doc.Load(xmlPath);

            genreMap.Clear();
            _codeToName.Clear();

            var groups = doc.SelectNodes("/genres/group").Cast<XmlNode>();
            foreach (var group in groups)
            {
                string groupName = group.Attributes["name"]?.Value.Trim();
                if (string.IsNullOrWhiteSpace(groupName)) continue;

                var genres = new List<(string Name, string Code, string Abrev)>();
                foreach (XmlNode genreNode in group.ChildNodes)
                {
                    string genreName = genreNode.Attributes["name"]?.Value.Trim();
                    string genreCode = genreNode.Attributes["code"]?.Value.Trim();

                    // 2. Считываем атрибут abrev из тега <genre>
                    string genreAbrev = genreNode.Attributes["abrev"]?.Value.Trim();
                    if (string.IsNullOrWhiteSpace(genreAbrev))
                    {
                        genreAbrev = genreName; // Запасной вариант, если abrev забыли указать
                    }

                    if (!string.IsNullOrWhiteSpace(genreName) && !string.IsNullOrWhiteSpace(genreCode))
                    {
                        genres.Add((genreName, genreCode, genreAbrev));
                        _codeToName[genreCode] = genreName;
                    }
                }
                genreMap[groupName] = genres;
            }
        }

        public static string GetGenreName(string kodGenre)
        {
            if (string.IsNullOrEmpty(kodGenre)) return "(Жанр не найден)";
            return _codeToName.TryGetValue(kodGenre, out var name) ? name : "(Жанр не найден)";
        }

        public static HashSet<string> GetGroupCodes(string groupName)
        {
            if (genreMap.TryGetValue(groupName, out var list))
            {
                return new HashSet<string>(list.Select(g => g.Code), StringComparer.OrdinalIgnoreCase);
            }
            return new HashSet<string>();
        }
    }
}