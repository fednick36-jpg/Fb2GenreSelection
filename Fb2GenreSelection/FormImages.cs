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
    public partial class FormImages : Form
    {
        // === Входные данные ===
        private readonly Fb2DocumentCache _docCache;
        private readonly string _dirCover;

        // === Модель картинок (загружается один раз в LoadImages) ===
        private List<Fb2Image> _images = new List<Fb2Image>();

        // === Флаг: что-то поменяли в форме ===
        public bool Changed { get; private set; }

        // === Запрет на рекурсивные события (при программном заполнении TextBox/ComboBox) ===
        private bool _suppressEvents = false;

        // ============================================================
        //  КОНСТРУКТОР
        // ============================================================

        public FormImages(Fb2DocumentCache docCache, string dirCover)
        {
            InitializeComponent();
            _docCache = docCache ?? throw new ArgumentNullException(nameof(docCache));
            _dirCover = dirCover ?? string.Empty;

            LoadImages();
            RefreshList();
            UpdateStatus();

            // Выделить обложку (или первую картинку, если обложки нет)
            SelectInitialImage();
        }

        // ============================================================
        //  ЗАГРУЗКА КАРТИНОК
        // ============================================================

        private void LoadImages()
        {
            if (_docCache.CurrentDoc == null)
            {
                _images = new List<Fb2Image>();
                return;
            }
            _images = Fb2ImageService.LoadAll(_docCache.CurrentDoc);
        }

        /// <summary>
        /// Выделить при открытии формы обложку (или первую картинку, если обложки нет).
        /// </summary>
        private void SelectInitialImage()
        {
            if (_listView.Items.Count == 0) return;

            int index = 0;
            for (int i = 0; i < _listView.Items.Count; i++)
            {
                var img = _listView.Items[i].Tag as Fb2Image;
                if (img != null && img.IsCover)
                {
                    index = i;
                    break;
                }
            }

            _listView.Items[index].Selected = true;
            _listView.Items[index].Focused = true;
            _listView.EnsureVisible(index);
        }

        // ============================================================
        //  ОБНОВЛЕНИЕ СПИСКА (без чекбоксов)
        // ============================================================

        private void RefreshList()
        {
            _listView.BeginUpdate();
            _listView.Items.Clear();

            foreach (var img in _images)
            {
                var item = new ListViewItem(img.Id);           // колонка «Имя»
                item.SubItems.Add(img.Format);                 // «Тип»
                item.SubItems.Add(img.Dimensions);             // «ШхВ»
                item.SubItems.Add(img.SizeDisplay);            // «Вес»
                item.SubItems.Add(img.RefCount.ToString());    // «Ссылок»
                item.SubItems.Add(img.Status);                 // «Статус»

                item.Tag = img;

                // Подсветка по статусу
                if (!img.IsReferenced)
                    item.BackColor = Color.MistyRose;
                else if (img.IsCover)
                    item.BackColor = Color.Honeydew;
                else
                    item.BackColor = Color.Ivory;

                _listView.Items.Add(item);
            }

            _listView.EndUpdate();
        }

        // ============================================================
        //  СТАТУС-СТРОКА
        // ============================================================

        private void UpdateStatus()
        {
            _statusCount.Text = $"Картинок: {_images.Count}";
            _statusChecked.Text = $"Выделено: {_listView.SelectedItems.Count}";
        }

        // ============================================================
        //  ВЫБОР КАРТИНКИ В СПИСКЕ
        // ============================================================

        private void ListView_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateStatus();

            if (_listView.SelectedItems.Count == 0)
            {
                SetPicture(null);
                _infoLabel.Text = "Выберите картинку справа";
                _textBoxNameImg.Text = string.Empty;
                return;
            }

            // Для превью и info берём первую выделенную
            var item = _listView.SelectedItems[0];
            var img = item.Tag as Fb2Image;
            if (img == null) return;

            SetPicture(img);

            _infoLabel.Text =
                $"{img.Dimensions} : {img.SizeDisplay} : {img.Format} : " +
                $"ссылок {img.RefCount} ({img.Status})";

            // Показать имя без расширения
            _suppressEvents = true;
            _textBoxNameImg.Text = StripExtension(img.Id);
            _suppressEvents = false;
        }

        private void PictureBox_Click(object sender, EventArgs e)
        {
            if (_pictureBox.Image == null) return;

            var frm = new FrmPictBoxFullSize();
            frm.Owner = this;                       // ← родитель

            double pictWidth = _pictureBox.Image.Width;
            double pictHeight = _pictureBox.Image.Height;

            if (pictHeight > 900)
            {
                frm.AutoSize = false;
                frm.Width = Convert.ToInt32(Math.Round(900 / (pictHeight / pictWidth)));
                frm.Height = 900;
                frm.pictBoxFS.Width = frm.ClientSize.Width;
                frm.pictBoxFS.Height = frm.ClientSize.Height;
                frm.pictBoxFS.SizeMode = PictureBoxSizeMode.Zoom;
            }

            frm.pictBoxFS.Image = _pictureBox.Image;
            frm.Show();
        }

        // ============================================================
        //  ВЫБОР ШАБЛОНА В COMBOBOX
        // ============================================================

        private void CmbTemplates_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_suppressEvents) return;
            if (_listView.SelectedItems.Count == 0) return;

            string template = _cmbTemplates.Text ?? string.Empty;
            if (template.Length == 0) return;

            // Сколько выделено?
            int selectedCount = CountRenameableSelected();

            _suppressEvents = true;

            if (selectedCount == 1)
            {
                // Одиночное: img_001 или cover
                if (string.Equals(template, "cover", StringComparison.OrdinalIgnoreCase))
                {
                    _textBoxNameImg.Text = "cover";
                }
                else
                {
                    _textBoxNameImg.Text = template + "001";
                }
            }
            else
            {
                // Групповое: просто шаблон, число добавится при переименовании
                _textBoxNameImg.Text = template;
            }

            _suppressEvents = false;
        }

        // ============================================================
        //  УДАЛЕНИЕ ВЫДЕЛЕННЫХ
        // ============================================================

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            if (_listView.SelectedItems.Count == 0)
            {
                MessageBox.Show("Не выделено ни одной картинки.",
                    "Картинки", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var selectedImages = new List<Fb2Image>();
            int firstRemovedIndex = -1;

            for (int i = 0; i < _listView.Items.Count; i++)
            {
                var item = _listView.Items[i];
                if (!item.Selected) continue;

                if (item.Tag is Fb2Image img)
                {
                    selectedImages.Add(img);
                    if (firstRemovedIndex < 0) firstRemovedIndex = i;
                }
            }

            if (selectedImages.Count == 0) return;

            // В документе не может быть больше одной картинки с IsCover == true
            var coverImg = selectedImages.FirstOrDefault(i => i.IsCover);
            bool coverHasDuplicates = coverImg != null && coverImg.RefCount > 1;

            // Обложку с дублями исключаем из "полного" удаления — для неё
            // отдельная операция ниже (DeleteExtraCoverReferences)
            var idsToFullyDelete = selectedImages
                .Where(i => !(coverHasDuplicates && ReferenceEquals(i, coverImg)))
                .Select(i => i.Id)
                .ToList();

            int totalRefsToFullyDelete = selectedImages
                .Where(i => !(coverHasDuplicates && ReferenceEquals(i, coverImg)))
                .Sum(i => i.RefCount);

            // === Текст подтверждения ===
            var messageParts = new List<string>();

            if (coverHasDuplicates)
            {
                int extraRefs = coverImg.RefCount - 1; // минус сама ссылка в <coverpage>
                messageParts.Add(
                    $"Обложка книги (id: {coverImg.Id}) продублирована в тексте — " +
                    $"на неё ссылаются ещё {extraRefs} раз(а) помимо coverpage.\n" +
                    $"Обложка и её ссылка в <coverpage> НЕ будут удалены — " +
                    $"удалятся только {extraRefs} лишних ссылок <image> в тексте.");
            }
            else if (coverImg != null)
            {
                messageParts.Add(
                    "ВНИМАНИЕ!\n\n" +
                    $"Среди выделенных есть ОБЛОЖКА книги (id: {coverImg.Id}).\n\n" +
                    "Если её удалить:\n" +
                    "  • в списке книг пропадёт превью;\n" +
                    "  • читалки не покажут обложку;\n" +
                    "  • восстановить можно будет только из резервной копии.");
            }

            if (idsToFullyDelete.Count > 0)
            {
                string part = $"Полностью удалить {idsToFullyDelete.Count} картинок?";
                if (totalRefsToFullyDelete > 0)
                    part += $"\nВместе с ними будут удалены {totalRefsToFullyDelete} ссылок <image> в тексте.";
                else
                    part += "\nНа эти картинки нет ссылок — они удалятся только сами.";

                messageParts.Add(part);
            }

            messageParts.Add("После сохранения отменить будет нельзя.\n\nПродолжить?");

            var icon = (coverImg != null && !coverHasDuplicates)
                ? MessageBoxIcon.Warning
                : MessageBoxIcon.Question;

            string title = coverImg != null ? "Удаление обложки" : "Удаление картинок";

            var result = MessageBox.Show(
                string.Join("\n\n", messageParts),
                title, MessageBoxButtons.YesNo, icon);

            if (result != DialogResult.Yes) return;

            var doc = _docCache.CurrentDoc;
            if (doc == null) return;

            if (idsToFullyDelete.Count > 0)
                Fb2ImageService.DeleteImages(doc, idsToFullyDelete);

            if (coverHasDuplicates)
                Fb2ImageService.DeleteExtraCoverReferences(doc, coverImg.Id);

            _docCache.MarkDirty();
            Changed = true;

            LoadImages();
            RefreshList();
            UpdateStatus();

            if (_listView.Items.Count > 0 && firstRemovedIndex >= 0)
            {
                int newIndex = firstRemovedIndex;
                if (newIndex >= _listView.Items.Count)
                    newIndex = _listView.Items.Count - 1;

                _listView.Items[newIndex].Selected = true;
                _listView.Items[newIndex].Focused = true;
                _listView.EnsureVisible(newIndex);
            }
            else
            {
                SetPicture(null);
                _infoLabel.Text = "Выберите картинку справа";
                _textBoxNameImg.Text = string.Empty;
            }
        }

        // ============================================================
        //  ПЕРЕИМЕНОВАНИЕ
        // ============================================================

        private void BtnRename_Click(object sender, EventArgs e)
        {
            if (_listView.SelectedItems.Count == 0)
            {
                MessageBox.Show("Не выделено ни одной картинки.",
                    "Переименование", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string text = (_textBoxNameImg.Text ?? string.Empty).Trim();

            if (text.Length == 0)
            {
                MessageBox.Show("Введите имя картинки.",
                    "Переименование", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (HasBadChars(text))
            {
                MessageBox.Show(
                    "Имя не должно содержать пробелы и символы: # < > & \" / \\ :",
                    "Переименование", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var selectedImages = new List<Fb2Image>();
            foreach (ListViewItem item in _listView.SelectedItems)
            {
                if (item.Tag is Fb2Image img)
                    selectedImages.Add(img);
            }

            if (selectedImages.Count == 0) return;

            if (selectedImages.Count == 1)
                RenameSingle(selectedImages[0], text);
            else
                RenameGroup(selectedImages, text);
        }

        /// <summary>
        /// Обрезает известное расширение (с точкой, например ".jpg"),
        /// если пользователь ввёл его вручную вместе с именем.
        /// </summary>
        private static string StripKnownExtension(string name, string ext)
        {
            if (!string.IsNullOrEmpty(ext) &&
                !string.IsNullOrEmpty(name) &&
                name.EndsWith(ext, StringComparison.OrdinalIgnoreCase))
            {
                return name.Substring(0, name.Length - ext.Length);
            }
            return name;
        }

        /// <summary>
        /// Одиночное переименование: newNameRaw = то, что ввёл пользователь,
        /// может быть как "cover", так и "cover.jpg" — расширение обрежется
        /// и заменится "родным" расширением старого id.
        /// </summary>
        private void RenameSingle(Fb2Image img, string newNameRaw)
        {
            string oldId = img.Id;
            string ext = GetExtension(oldId);

            string newName = StripKnownExtension(newNameRaw, ext);
            string newId = newName + ext;

            if (string.Equals(newId, oldId, StringComparison.OrdinalIgnoreCase))
                return; // не изменилось

            if (IdExists(newId))
            {
                MessageBox.Show(
                    $"Картинка с id \"{newId}\" уже существует.",
                    "Переименование", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var doc = _docCache?.CurrentDoc;
            if (doc == null) return;

            Fb2ImageService.RenameImage(doc, oldId, newId);

            // Точечное обновление модели вместо полного LoadImages()
            img.Id = newId;

            _docCache.MarkDirty();
            Changed = true;

            RefreshList();
            UpdateStatus();
            SelectById(newId);
        }

        /// <summary>
        /// Групповое переименование: prefixRaw — префикс шаблона ("img_", "i_").
        /// Нумерация 001, 002, ... Обложка НЕ пропускается автоматически —
        /// иногда две картинки одновременно являются кандидатами на обложку
        /// (дубликаты разного размера), и их осознанно переименовывают вместе,
        /// чтобы затем разобрать вручную. Диалог подтверждения (п. 4) явно
        /// предупреждает, если среди выделенных есть обложка.
        /// </summary>
        private void RenameGroup(List<Fb2Image> images, string prefixRaw)
        {
            var doc = _docCache?.CurrentDoc;
            if (doc == null) return;
            if (images == null || images.Count == 0) return;

            string prefix = prefixRaw.Trim();
            if (string.IsNullOrEmpty(prefix)) prefix = "img_";

            // 1. План переименований — участвуют все выделенные, включая обложку
            var plan = new List<(Fb2Image img, string oldId, string newId)>();
            int counter = 1;

            foreach (var img in images)
            {
                string ext = GetExtension(img.Id);
                string newId = prefix + counter.ToString("D3") + ext;
                plan.Add((img, img.Id, newId));
                counter++;
            }

            // 2. Внутренние дубли в самом плане
            var newIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var p in plan)
            {
                if (!newIds.Add(p.newId))
                {
                    MessageBox.Show(
                        $"Внутренний конфликт: id \"{p.newId}\" встречается дважды.",
                        "Переименование", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }

            // 3. Конфликты с "чужими" существующими id — O(n) через HashSet
            var oldIdsInPlan = new HashSet<string>(
                plan.Select(p => p.oldId), StringComparer.OrdinalIgnoreCase);

            var otherExistingIds = new HashSet<string>(
                _images.Where(i => !oldIdsInPlan.Contains(i.Id)).Select(i => i.Id),
                StringComparer.OrdinalIgnoreCase);

            foreach (var p in plan)
            {
                if (otherExistingIds.Contains(p.newId))
                {
                    MessageBox.Show(
                        $"Картинка с id \"{p.newId}\" уже существует.",
                        "Переименование", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }

            // 4. Подтверждение — если среди выделенных есть обложка, явно предупреждаем
            bool includesCover = plan.Any(p => p.img.IsCover);

            string confirmText =
                $"Переименовать {plan.Count} картинок?\n\n" +
                $"Например: {plan[0].oldId} → {plan[0].newId}";

            if (includesCover)
                confirmText += "\n\nВнимание: среди выделенных есть обложка — она тоже будет переименована.";

            var result = MessageBox.Show(confirmText,
                "Групповое переименование",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result != DialogResult.Yes) return;

            // 5. Применяем — XML правит сервис, модель обновляем точечно
            foreach (var p in plan)
            {
                Fb2ImageService.RenameImage(doc, p.oldId, p.newId);
                p.img.Id = p.newId;
            }

            _docCache.MarkDirty();
            Changed = true;

            RefreshList();
            UpdateStatus();
            SelectById(plan[0].newId);
        }

        // ============================================================
        //  СОХРАНЕНИЕ НА ДИСК
        // ============================================================

        private void BtnSaveToDisk_Click(object sender, EventArgs e)
        {
            if (_listView.SelectedItems.Count == 0)
            {
                MessageBox.Show("Не выделено ни одной картинки.",
                    "Сохранение", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var item = _listView.SelectedItems[0];
            var img = item.Tag as Fb2Image;
            if (img?.Data == null)
            {
                MessageBox.Show("Нет данных картинки.",
                    "Сохранение", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Расширение из старого id
            string ext = GetExtension(img.Id);          // ".jpg" или ".png"
            if (string.IsNullOrEmpty(ext)) ext = ".jpg"; // fallback

            // Имя файла — всегда cover0.{ext}
            string fileName = "cover0" + ext;

            // Начальная папка и имя для диалога
            string initialDir = !string.IsNullOrEmpty(_dirCover) && Directory.Exists(_dirCover)
                ? _dirCover
                : null;

            using (var sfd = new SaveFileDialog())
            {
                sfd.Title = "Сохранить картинку";
                sfd.FileName = fileName;
                sfd.Filter = "Все файлы (*.*)|*.*";
                sfd.OverwritePrompt = true;   // спросить, если файл есть
                if (initialDir != null)
                    sfd.InitialDirectory = initialDir;

                if (sfd.ShowDialog(this) == DialogResult.OK)
                {
                    try
                    {
                        File.WriteAllBytes(sfd.FileName, img.Data);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка сохранения: {ex.Message}",
                            "Сохранение", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        // ============================================================
        //  ВЫХОД
        // ============================================================

        private void BtnExit_Click(object sender, EventArgs e)
        {
            Close();
        }

        // ============================================================
        //  ВСПОМОГАТЕЛЬНЫЕ
        // ============================================================

        private void SetPicture(Fb2Image img)
        {
            var old = _pictureBox.Image;
            _pictureBox.Image = null;
            old?.Dispose();

            if (img?.Data == null || img.Data.Length == 0) return;

            try
            {
                using (var ms = new MemoryStream(img.Data))
                using (var loaded = Image.FromStream(ms,
                    useEmbeddedColorManagement: false,
                    validateImageData: false))
                {
                    _pictureBox.Image = new Bitmap(loaded);
                }
            }
            catch
            {
                // Битые данные — не показываем
            }
        }

        /// <summary>Отделить расширение от id: "i_001.jpg" → ".jpg".</summary>
        private static string GetExtension(string id)
        {
            if (string.IsNullOrEmpty(id)) return string.Empty;
            int dot = id.LastIndexOf('.');
            return dot >= 0 ? id.Substring(dot) : string.Empty;
        }

        /// <summary>Отделить имя без расширения: "i_001.jpg" → "i_001".</summary>
        private static string StripExtension(string id)
        {
            if (string.IsNullOrEmpty(id)) return string.Empty;
            int dot = id.LastIndexOf('.');
            return dot >= 0 ? id.Substring(0, dot) : id;
        }

        /// <summary>Проверка запрещённых символов.</summary>
        private static bool HasBadChars(string s)
        {
            if (string.IsNullOrEmpty(s)) return false;
            return s.IndexOfAny(new[] { ' ', '#', '<', '>', '&', '"', '/', '\\', ':' }) >= 0;
        }

        /// <summary>Есть ли картинка с таким id.</summary>
        private bool IdExists(string id)
        {
            foreach (var img in _images)
            {
                if (string.Equals(img.Id, id, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        /// <summary>Сколько выделенных картинок можно переименовать (без обложки, если их > 1).</summary>
        private int CountRenameableSelected()
        {
            int count = 0;
            foreach (ListViewItem item in _listView.SelectedItems)
            {
                var img = item.Tag as Fb2Image;
                if (img == null) continue;
                if (img.IsCover && _listView.SelectedItems.Count > 1) continue;
                count++;
            }
            return count;
        }

        /// <summary>Выделить строку по id.</summary>
        private void SelectById(string id)
        {
            for (int i = 0; i < _listView.Items.Count; i++)
            {
                var it = _listView.Items[i];
                var im = it.Tag as Fb2Image;
                if (im != null && string.Equals(im.Id, id, StringComparison.OrdinalIgnoreCase))
                {
                    _listView.SelectedItems.Clear();
                    it.Selected = true;
                    it.Focused = true;
                    _listView.EnsureVisible(i);
                    break;
                }
            }
        }

        // ============================================================
        //  ОСВОБОЖДЕНИЕ РЕСУРСОВ
        // ============================================================

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            var img = _pictureBox?.Image;
            if (_pictureBox != null) _pictureBox.Image = null;
            img?.Dispose();

            base.OnFormClosed(e);
        }
    }
}