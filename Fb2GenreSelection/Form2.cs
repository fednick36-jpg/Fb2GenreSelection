using System;
using System.Windows.Forms;


namespace Fb2GenreSelection
{
    public partial class Form2 : Form
    {
        OpenFileDialog OFD = new();
        GenreSelectionConfig GSC = new();

        // ПОЛНОЦЕННЫЙ ВАРИАНТ: вместо удалённого лишнего "Form1 form1 = new Form1()"
        // теперь Form2 хранит ссылку на РЕАЛЬНО открытое окно программы, переданное
        // через конструктор. owner может быть null — например, если кто-то откроет
        // Form2 через дизайнер Visual Studio (там нужен parameterless-конструктор) —
        // в этом случае просто не будет мгновенного применения настроек, но сохранение
        // в XML по-прежнему работает.
        readonly Form1 form1;

        public Form2(Form1 owner = null)
        {
            InitializeComponent();
            form1 = owner;
        }

        private void Form2_Load_1(object sender, EventArgs e)
        {
            // ИСПРАВЛЕНИЕ: ReadPathXxx() может вернуть null (узел отсутствует в XML
            // или файл конфигурации не найден) — "?? string.Empty" подстраховывает
            // от неожиданного поведения Control.Text при присвоении null.
            textBoxPathFBE.Text = GSC.ReadPathFBE() ?? string.Empty;
            textBoxPathNP.Text = GSC.ReadPathNP() ?? string.Empty;
            textBoxPathFSV.Text = GSC.ReadPathFSV() ?? string.Empty;
            textBoxPathBook.Text = GSC.ReadPathBook() ?? string.Empty;
            textBoxPathCover.Text = GSC.ReadPathCover() ?? string.Empty;
        }

        private void BtnOpenDirFBE_Click(object sender, EventArgs e)
        {
            DialogResult result = OFD.ShowDialog();

            if (result == DialogResult.OK)
            {
                textBoxPathFBE.Text = OFD.FileName;
            }

        }

        private void BtnOpenDirNP_Click(object sender, EventArgs e)
        {
            DialogResult result = OFD.ShowDialog();

            if (result == DialogResult.OK)
            {
                textBoxPathNP.Text = OFD.FileName;
            }
        }

        private void BtnOpenDirFSV_Click(object sender, EventArgs e)
        {
            DialogResult result = OFD.ShowDialog();

            if (result == DialogResult.OK)
            {
                textBoxPathFSV.Text = OFD.FileName;
            }
        }

        private void BtnOpenDirCover_Click(object sender, EventArgs e)
        {
            // ИСПРАВЛЕНИЕ: раньше использовался form1.FBD — диалог выбора папки лишнего
            // экземпляра Form1. Так как выбор папки никак не зависит от состояния главной
            // формы, используем собственный FolderBrowserDialog, а не чужой чужого объекта.
            using var fbd = new FolderBrowserDialog();
            DialogResult result = fbd.ShowDialog();

            if (result == DialogResult.OK)
            {
                textBoxPathCover.Text = fbd.SelectedPath;
            }

        }

        private void BtnOpenDirBook_Click(object sender, EventArgs e)
        {
            using var fbd = new FolderBrowserDialog();
            DialogResult result = fbd.ShowDialog();

            if (result == DialogResult.OK)
            {
                textBoxPathBook.Text = fbd.SelectedPath;
            }

        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            GSC.WritePaths(textBoxPathFBE.Text, textBoxPathNP.Text, textBoxPathFSV.Text, textBoxPathBook.Text, textBoxPathCover.Text);

            // ПОЛНОЦЕННЫЙ ВАРИАНТ: применяем новые пути к реально открытому Form1 сразу
            // после сохранения в XML — теперь работающее окно программы получает новые
            // пути без перезапуска. Делаем это здесь (а не в каждом BtnOpenDirXxx_Click),
            // чтобы кнопка "Отмена" по-прежнему ничего не применяла, если пользователь
            // передумал.
            if (form1 != null)
            {
                form1.dirFBE = textBoxPathFBE.Text;
                form1.dirNP = textBoxPathNP.Text;
                form1.dirFSV = textBoxPathFSV.Text;
                form1.dirBook = textBoxPathBook.Text;
                form1.dirCover = textBoxPathCover.Text;

                MessageBox.Show("Настройки сохранены и применены.", "Настройки", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                // Резервный случай: Form2 открыли без ссылки на реальный Form1
                // (например, из дизайнера) — тогда, как и раньше, нужен перезапуск.
                MessageBox.Show("Чтобы изменения вступили в силу\n ПЕРЕЗАПУСТИТЕ ПРОГРАММУ!", "Настройки", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

            Close();
        }

        private void BtnCansel_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
