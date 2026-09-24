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
        public static Dictionary<string, List<(string Name, string Code)>> genreMap = [];

        // Обратный индекс code → name для быстрого GetGenreName.
        // Заполняется в LoadGenres.
        private static readonly Dictionary<string, string> _codeToName
            = new(StringComparer.OrdinalIgnoreCase);

        public static void LoadGenres(string xmlPath)
        {
            XmlDocument doc = new();
            doc.Load(xmlPath);

            genreMap.Clear();
            _codeToName.Clear();  // ← добавить

            var groups = doc.SelectNodes("/genres/group").Cast<XmlNode>();
            foreach (var group in groups)
            {
                string groupName = group.Attributes["name"]?.Value.Trim();
                if (string.IsNullOrWhiteSpace(groupName)) continue;

                var genres = new List<(string, string)>();
                foreach (XmlNode genreNode in group.ChildNodes)
                {
                    string genreName = genreNode.Attributes["name"]?.Value.Trim();
                    string genreCode = genreNode.Attributes["code"]?.Value.Trim();
                    if (!string.IsNullOrWhiteSpace(genreName) && !string.IsNullOrWhiteSpace(genreCode))
                    {
                        genres.Add((genreName, genreCode));
                        _codeToName[genreCode] = genreName;  // ← добавить
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
