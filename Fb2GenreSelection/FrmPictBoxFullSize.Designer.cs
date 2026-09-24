namespace Fb2GenreSelection
{
    partial class FrmPictBoxFullSize
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
            this.pictBoxFS = new System.Windows.Forms.PictureBox();
            ((System.ComponentModel.ISupportInitialize)(this.pictBoxFS)).BeginInit();
            this.SuspendLayout();
            // 
            // pictBoxFS
            // 
            this.pictBoxFS.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pictBoxFS.InitialImage = null;
            this.pictBoxFS.Location = new System.Drawing.Point(0, 0);
            this.pictBoxFS.Name = "pictBoxFS";
            this.pictBoxFS.Size = new System.Drawing.Size(314, 488);
            this.pictBoxFS.SizeMode = System.Windows.Forms.PictureBoxSizeMode.AutoSize;
            this.pictBoxFS.TabIndex = 0;
            this.pictBoxFS.TabStop = false;
            this.pictBoxFS.Click += new System.EventHandler(this.PictBoxFS_Click);
            // 
            // FrmPictBoxFullSize
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.ClientSize = new System.Drawing.Size(314, 488);
            this.Controls.Add(this.pictBoxFS);
            this.KeyPreview = true;
            this.Name = "FrmPictBoxFullSize";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Полный размер";
            ((System.ComponentModel.ISupportInitialize)(this.pictBoxFS)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        public System.Windows.Forms.PictureBox pictBoxFS;
    }
}