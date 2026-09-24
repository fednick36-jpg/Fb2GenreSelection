namespace Fb2GenreSelection
{
    partial class FormImages
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this._statusStrip = new System.Windows.Forms.StatusStrip();
            this._statusCount = new System.Windows.Forms.ToolStripStatusLabel();
            this._statusChecked = new System.Windows.Forms.ToolStripStatusLabel();
            this._pictureBox = new System.Windows.Forms.PictureBox();
            this._listView = new System.Windows.Forms.ListView();
            this.columnHeader1 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader2 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader3 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader4 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader5 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader6 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this._infoLabel = new System.Windows.Forms.Label();
            this.panel1 = new System.Windows.Forms.Panel();
            this._textBoxNameImg = new System.Windows.Forms.TextBox();
            this._cmbTemplates = new System.Windows.Forms.ComboBox();
            this._btnRename = new System.Windows.Forms.Button();
            this._btnExit = new System.Windows.Forms.Button();
            this._btnSaveToDisk = new System.Windows.Forms.Button();
            this._btnDelete = new System.Windows.Forms.Button();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.tableLayoutPanel1.SuspendLayout();
            this._statusStrip.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this._pictureBox)).BeginInit();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 2;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 350F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Controls.Add(this._statusStrip, 0, 3);
            this.tableLayoutPanel1.Controls.Add(this._pictureBox, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this._listView, 1, 0);
            this.tableLayoutPanel1.Controls.Add(this._infoLabel, 0, 2);
            this.tableLayoutPanel1.Controls.Add(this.panel1, 1, 2);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 4;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 37F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(934, 581);
            this.tableLayoutPanel1.TabIndex = 0;
            // 
            // _statusStrip
            // 
            this._statusStrip.ImageScalingSize = new System.Drawing.Size(24, 24);
            this._statusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this._statusCount,
            this._statusChecked});
            this._statusStrip.Location = new System.Drawing.Point(0, 557);
            this._statusStrip.Name = "_statusStrip";
            this._statusStrip.Size = new System.Drawing.Size(350, 24);
            this._statusStrip.TabIndex = 0;
            this._statusStrip.Text = "statusStrip1";
            // 
            // _statusCount
            // 
            this._statusCount.Name = "_statusCount";
            this._statusCount.Size = new System.Drawing.Size(71, 19);
            this._statusCount.Text = "Картинок: 0";
            // 
            // _statusChecked
            // 
            this._statusChecked.BorderSides = System.Windows.Forms.ToolStripStatusLabelBorderSides.Left;
            this._statusChecked.Name = "_statusChecked";
            this._statusChecked.Size = new System.Drawing.Size(78, 19);
            this._statusChecked.Text = "Выделено: 0";
            // 
            // _pictureBox
            // 
            this._pictureBox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this._pictureBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this._pictureBox.Location = new System.Drawing.Point(3, 3);
            this._pictureBox.Name = "_pictureBox";
            this.tableLayoutPanel1.SetRowSpan(this._pictureBox, 2);
            this._pictureBox.Size = new System.Drawing.Size(344, 508);
            this._pictureBox.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this._pictureBox.TabIndex = 1;
            this._pictureBox.TabStop = false;
            this._pictureBox.Click += new System.EventHandler(this.PictureBox_Click);
            // 
            // _listView
            // 
            this._listView.BackColor = System.Drawing.Color.Ivory;
            this._listView.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this._listView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader1,
            this.columnHeader2,
            this.columnHeader3,
            this.columnHeader4,
            this.columnHeader5,
            this.columnHeader6});
            this._listView.Dock = System.Windows.Forms.DockStyle.Fill;
            this._listView.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this._listView.FullRowSelect = true;
            this._listView.GridLines = true;
            this._listView.HideSelection = false;
            this._listView.Location = new System.Drawing.Point(353, 3);
            this._listView.Name = "_listView";
            this.tableLayoutPanel1.SetRowSpan(this._listView, 2);
            this._listView.Size = new System.Drawing.Size(578, 508);
            this._listView.TabIndex = 2;
            this._listView.UseCompatibleStateImageBehavior = false;
            this._listView.View = System.Windows.Forms.View.Details;
            this._listView.SelectedIndexChanged += new System.EventHandler(this.ListView_SelectedIndexChanged);
            // 
            // columnHeader1
            // 
            this.columnHeader1.Text = "Имя";
            this.columnHeader1.Width = 228;
            // 
            // columnHeader2
            // 
            this.columnHeader2.Text = "Тип";
            this.columnHeader2.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.columnHeader2.Width = 70;
            // 
            // columnHeader3
            // 
            this.columnHeader3.Text = "ШхВ";
            this.columnHeader3.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.columnHeader3.Width = 90;
            // 
            // columnHeader4
            // 
            this.columnHeader4.Text = "Вес";
            this.columnHeader4.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.columnHeader4.Width = 70;
            // 
            // columnHeader5
            // 
            this.columnHeader5.Text = "Ссылок";
            this.columnHeader5.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // columnHeader6
            // 
            this.columnHeader6.Text = "Статус";
            this.columnHeader6.Width = 110;
            // 
            // _infoLabel
            // 
            this._infoLabel.BackColor = System.Drawing.Color.Ivory;
            this._infoLabel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this._infoLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this._infoLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F);
            this._infoLabel.Location = new System.Drawing.Point(3, 514);
            this._infoLabel.Name = "_infoLabel";
            this._infoLabel.Size = new System.Drawing.Size(344, 37);
            this._infoLabel.TabIndex = 3;
            this._infoLabel.Text = "Выберите картинку справа";
            this._infoLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this._textBoxNameImg);
            this.panel1.Controls.Add(this._cmbTemplates);
            this.panel1.Controls.Add(this._btnRename);
            this.panel1.Controls.Add(this._btnExit);
            this.panel1.Controls.Add(this._btnSaveToDisk);
            this.panel1.Controls.Add(this._btnDelete);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel1.Location = new System.Drawing.Point(353, 517);
            this.panel1.Name = "panel1";
            this.tableLayoutPanel1.SetRowSpan(this.panel1, 2);
            this.panel1.Size = new System.Drawing.Size(578, 61);
            this.panel1.TabIndex = 4;
            // 
            // _textBoxNameImg
            // 
            this._textBoxNameImg.BackColor = System.Drawing.Color.MintCream;
            this._textBoxNameImg.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this._textBoxNameImg.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this._textBoxNameImg.Location = new System.Drawing.Point(55, 10);
            this._textBoxNameImg.Name = "_textBoxNameImg";
            this._textBoxNameImg.Size = new System.Drawing.Size(173, 23);
            this._textBoxNameImg.TabIndex = 5;
            this.toolTip1.SetToolTip(this._textBoxNameImg, "Отображается имя картинки");
            // 
            // _cmbTemplates
            // 
            this._cmbTemplates.BackColor = System.Drawing.Color.MintCream;
            this._cmbTemplates.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this._cmbTemplates.FormattingEnabled = true;
            this._cmbTemplates.Items.AddRange(new object[] {
            "",
            "cover",
            "img_",
            "i_"});
            this._cmbTemplates.Location = new System.Drawing.Point(234, 10);
            this._cmbTemplates.Name = "_cmbTemplates";
            this._cmbTemplates.Size = new System.Drawing.Size(90, 24);
            this._cmbTemplates.TabIndex = 4;
            this.toolTip1.SetToolTip(this._cmbTemplates, "Выбор имен для картинок");
            this._cmbTemplates.SelectedIndexChanged += new System.EventHandler(this.CmbTemplates_SelectedIndexChanged);
            // 
            // _btnRename
            // 
            this._btnRename.BackColor = System.Drawing.Color.Gainsboro;
            this._btnRename.BackgroundImage = global::Fb2GenreSelection.Properties.Resources.BtnFonBlue28;
            this._btnRename.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this._btnRename.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this._btnRename.ForeColor = System.Drawing.SystemColors.ControlText;
            this._btnRename.Image = global::Fb2GenreSelection.Properties.Resources.Rename28;
            this._btnRename.Location = new System.Drawing.Point(330, 10);
            this._btnRename.Name = "_btnRename";
            this._btnRename.Size = new System.Drawing.Size(40, 40);
            this._btnRename.TabIndex = 3;
            this.toolTip1.SetToolTip(this._btnRename, "Переименвывет картинку(и)");
            this._btnRename.UseVisualStyleBackColor = false;
            this._btnRename.Click += new System.EventHandler(this.BtnRename_Click);
            // 
            // _btnExit
            // 
            this._btnExit.BackColor = System.Drawing.Color.WhiteSmoke;
            this._btnExit.BackgroundImage = global::Fb2GenreSelection.Properties.Resources.BtnFonRed28;
            this._btnExit.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this._btnExit.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this._btnExit.ForeColor = System.Drawing.SystemColors.ControlText;
            this._btnExit.Image = global::Fb2GenreSelection.Properties.Resources.Exit28;
            this._btnExit.Location = new System.Drawing.Point(529, 10);
            this._btnExit.Name = "_btnExit";
            this._btnExit.Size = new System.Drawing.Size(40, 40);
            this._btnExit.TabIndex = 2;
            this.toolTip1.SetToolTip(this._btnExit, "Закрыть форму");
            this._btnExit.UseVisualStyleBackColor = false;
            this._btnExit.Click += new System.EventHandler(this.BtnExit_Click);
            // 
            // _btnSaveToDisk
            // 
            this._btnSaveToDisk.BackColor = System.Drawing.Color.Gainsboro;
            this._btnSaveToDisk.BackgroundImage = global::Fb2GenreSelection.Properties.Resources.BtnFonBlue28;
            this._btnSaveToDisk.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this._btnSaveToDisk.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this._btnSaveToDisk.ForeColor = System.Drawing.SystemColors.ControlText;
            this._btnSaveToDisk.Image = global::Fb2GenreSelection.Properties.Resources.ToDisk30;
            this._btnSaveToDisk.Location = new System.Drawing.Point(376, 10);
            this._btnSaveToDisk.Name = "_btnSaveToDisk";
            this._btnSaveToDisk.Size = new System.Drawing.Size(40, 40);
            this._btnSaveToDisk.TabIndex = 1;
            this.toolTip1.SetToolTip(this._btnSaveToDisk, "Сохранить картинку на диск");
            this._btnSaveToDisk.UseVisualStyleBackColor = false;
            this._btnSaveToDisk.Click += new System.EventHandler(this.BtnSaveToDisk_Click);
            // 
            // _btnDelete
            // 
            this._btnDelete.BackColor = System.Drawing.Color.Red;
            this._btnDelete.BackgroundImage = global::Fb2GenreSelection.Properties.Resources.BtnFonRed28;
            this._btnDelete.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this._btnDelete.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this._btnDelete.ForeColor = System.Drawing.SystemColors.ButtonHighlight;
            this._btnDelete.Image = global::Fb2GenreSelection.Properties.Resources.ImgDelete_28;
            this._btnDelete.Location = new System.Drawing.Point(9, 10);
            this._btnDelete.Name = "_btnDelete";
            this._btnDelete.Size = new System.Drawing.Size(40, 40);
            this._btnDelete.TabIndex = 0;
            this.toolTip1.SetToolTip(this._btnDelete, "Удаление выделенных картинок");
            this._btnDelete.UseVisualStyleBackColor = false;
            this._btnDelete.Click += new System.EventHandler(this.BtnDelete_Click);
            // 
            // FormImages
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(934, 581);
            this.Controls.Add(this.tableLayoutPanel1);
            this.Name = "FormImages";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Картинки книги";
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this._statusStrip.ResumeLayout(false);
            this._statusStrip.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this._pictureBox)).EndInit();
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.StatusStrip _statusStrip;
        private System.Windows.Forms.PictureBox _pictureBox;
        private System.Windows.Forms.ListView _listView;
        private System.Windows.Forms.Label _infoLabel;
        private System.Windows.Forms.ToolStripStatusLabel _statusCount;
        private System.Windows.Forms.ToolStripStatusLabel _statusChecked;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Button _btnDelete;
        private System.Windows.Forms.Button _btnExit;
        private System.Windows.Forms.Button _btnSaveToDisk;
        private System.Windows.Forms.Button _btnRename;
        private System.Windows.Forms.ColumnHeader columnHeader1;
        private System.Windows.Forms.ColumnHeader columnHeader2;
        private System.Windows.Forms.ColumnHeader columnHeader3;
        private System.Windows.Forms.ColumnHeader columnHeader4;
        private System.Windows.Forms.ColumnHeader columnHeader5;
        private System.Windows.Forms.ColumnHeader columnHeader6;
        private System.Windows.Forms.ToolTip toolTip1;
        private System.Windows.Forms.ComboBox _cmbTemplates;
        private System.Windows.Forms.TextBox _textBoxNameImg;
    }
}