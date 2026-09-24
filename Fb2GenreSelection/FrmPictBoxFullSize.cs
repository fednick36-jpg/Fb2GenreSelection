using System;
using System.Windows.Forms;

namespace Fb2GenreSelection
{
    public partial class FrmPictBoxFullSize : Form
    {
        public FrmPictBoxFullSize()
        {
            InitializeComponent();
        }

        private void PictBoxFS_Click(object sender, EventArgs e)
        {
            Close();
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Escape)
            {
                Close();
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }
    }
}