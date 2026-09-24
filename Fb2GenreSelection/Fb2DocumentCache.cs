using System.IO;
using System.Xml.Linq;

namespace Fb2GenreSelection
{
    /// <summary>
    /// Держит XDocument текущей открытой книги в памяти.
    /// Все правки идут в память, на диск — только при переключении книги
    /// или при закрытии программы.
    /// </summary>
    public class Fb2DocumentCache
    {
        private XDocument _currentDoc;
        private string _currentPath;
        private bool _isDirty;

        public string CurrentPath => _currentPath;
        public bool IsDirty => _isDirty;
        public XDocument CurrentDoc => _currentDoc;

        /// <summary>
        /// Открыть документ для книги. Если это та же книга — вернуть уже загруженный.
        /// Если другая — сохранить предыдущую (если была изменена) и загрузить новую.
        /// </summary>
        public XDocument Open(string filePath)
        {
            if (_currentPath == filePath && _currentDoc != null)
                return _currentDoc;

            SaveIfDirty();

            _currentDoc = XDocument.Load(filePath);
            _currentPath = filePath;
            _isDirty = false;
            return _currentDoc;
        }

        /// <summary>
        /// Пометить документ как изменённый. На диск не пишет — только флаг.
        /// </summary>
        public void MarkDirty()
        {
            _isDirty = true;
        }

        /// <summary>
        /// Сохранить текущий документ, если были правки.
        /// </summary>
        public void SaveIfDirty()
        {
            if (_isDirty && _currentDoc != null && !string.IsNullOrEmpty(_currentPath))
            {
                _currentDoc.Save(_currentPath);
                _isDirty = false;
            }
        }

        /// <summary>
        /// Сбросить кэш без сохранения. Используется, когда файл удаляется
        /// или переименовывается — писать в него уже нельзя.
        /// </summary>
        public void CloseWithoutSave()
        {
            _currentDoc = null;
            _currentPath = null;
            _isDirty = false;
        }
    }
}