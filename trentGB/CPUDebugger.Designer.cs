namespace trentGB
{
    partial class CPUDebugger
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
            this.displayBox = new System.Windows.Forms.RichTextBox();
            this.yesBtn = new System.Windows.Forms.Button();
            this.noBtn = new System.Windows.Forms.Button();
            this.continueBtn = new System.Windows.Forms.Button();
            this.contAddrTxtBox = new System.Windows.Forms.TextBox();
            this.Watch = new System.Windows.Forms.Button();
            this.watchAddrTxtBox = new System.Windows.Forms.TextBox();
            this.watchAddrListBox = new System.Windows.Forms.ListView();
            this.columnHeader1 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader4 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.memoryListBox = new System.Windows.Forms.ListView();
            this.columnHeader2 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader3 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.watchViewContextMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.removeSelectedToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.watchViewContextMenu.SuspendLayout();
            this.SuspendLayout();
            // 
            // displayBox
            // 
            this.displayBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.displayBox.BackColor = System.Drawing.SystemColors.ControlDarkDark;
            this.displayBox.ForeColor = System.Drawing.Color.White;
            this.displayBox.Location = new System.Drawing.Point(13, 13);
            this.displayBox.Name = "displayBox";
            this.displayBox.ReadOnly = true;
            this.displayBox.Size = new System.Drawing.Size(254, 672);
            this.displayBox.TabIndex = 0;
            this.displayBox.Text = "";
            // 
            // yesBtn
            // 
            this.yesBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.yesBtn.BackColor = System.Drawing.SystemColors.ControlDarkDark;
            this.yesBtn.DialogResult = System.Windows.Forms.DialogResult.Yes;
            this.yesBtn.ForeColor = System.Drawing.Color.White;
            this.yesBtn.Location = new System.Drawing.Point(20, 734);
            this.yesBtn.Name = "yesBtn";
            this.yesBtn.Size = new System.Drawing.Size(75, 28);
            this.yesBtn.TabIndex = 1;
            this.yesBtn.Text = "Yes";
            this.yesBtn.UseVisualStyleBackColor = false;
            // 
            // noBtn
            // 
            this.noBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.noBtn.BackColor = System.Drawing.SystemColors.ControlDarkDark;
            this.noBtn.DialogResult = System.Windows.Forms.DialogResult.No;
            this.noBtn.ForeColor = System.Drawing.Color.White;
            this.noBtn.Location = new System.Drawing.Point(105, 734);
            this.noBtn.Name = "noBtn";
            this.noBtn.Size = new System.Drawing.Size(75, 28);
            this.noBtn.TabIndex = 2;
            this.noBtn.Text = "No";
            this.noBtn.UseVisualStyleBackColor = false;
            // 
            // continueBtn
            // 
            this.continueBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.continueBtn.BackColor = System.Drawing.SystemColors.ControlDarkDark;
            this.continueBtn.DialogResult = System.Windows.Forms.DialogResult.Ignore;
            this.continueBtn.ForeColor = System.Drawing.Color.White;
            this.continueBtn.Location = new System.Drawing.Point(20, 700);
            this.continueBtn.Name = "continueBtn";
            this.continueBtn.Size = new System.Drawing.Size(75, 28);
            this.continueBtn.TabIndex = 3;
            this.continueBtn.Text = "Continue";
            this.continueBtn.UseVisualStyleBackColor = false;
            this.continueBtn.Click += new System.EventHandler(this.continueBtn_Click);
            // 
            // contAddrTxtBox
            // 
            this.contAddrTxtBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.contAddrTxtBox.BackColor = System.Drawing.SystemColors.ControlDarkDark;
            this.contAddrTxtBox.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
            this.contAddrTxtBox.ForeColor = System.Drawing.Color.White;
            this.contAddrTxtBox.Location = new System.Drawing.Point(105, 705);
            this.contAddrTxtBox.MaxLength = 4;
            this.contAddrTxtBox.Name = "contAddrTxtBox";
            this.contAddrTxtBox.Size = new System.Drawing.Size(75, 20);
            this.contAddrTxtBox.TabIndex = 4;
            this.contAddrTxtBox.Validating += new System.ComponentModel.CancelEventHandler(this.contAddrTxtBox_Validating);
            // 
            // Watch
            // 
            this.Watch.BackColor = System.Drawing.SystemColors.ControlDarkDark;
            this.Watch.ForeColor = System.Drawing.Color.White;
            this.Watch.Location = new System.Drawing.Point(652, 699);
            this.Watch.Name = "Watch";
            this.Watch.Size = new System.Drawing.Size(88, 26);
            this.Watch.TabIndex = 7;
            this.Watch.Text = "Watch";
            this.Watch.UseVisualStyleBackColor = false;
            this.Watch.Click += new System.EventHandler(this.Watch_Click);
            // 
            // watchAddrTxtBox
            // 
            this.watchAddrTxtBox.BackColor = System.Drawing.SystemColors.ControlDarkDark;
            this.watchAddrTxtBox.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
            this.watchAddrTxtBox.ForeColor = System.Drawing.Color.White;
            this.watchAddrTxtBox.Location = new System.Drawing.Point(533, 700);
            this.watchAddrTxtBox.MaxLength = 4;
            this.watchAddrTxtBox.Name = "watchAddrTxtBox";
            this.watchAddrTxtBox.Size = new System.Drawing.Size(113, 20);
            this.watchAddrTxtBox.TabIndex = 8;
            this.watchAddrTxtBox.Validating += new System.ComponentModel.CancelEventHandler(this.watchAddrTxtBox_Validating);
            // 
            // watchAddrListBox
            // 
            this.watchAddrListBox.BackColor = System.Drawing.SystemColors.ControlDarkDark;
            this.watchAddrListBox.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader1,
            this.columnHeader4});
            this.watchAddrListBox.ContextMenuStrip = this.watchViewContextMenu;
            this.watchAddrListBox.ForeColor = System.Drawing.Color.White;
            this.watchAddrListBox.FullRowSelect = true;
            this.watchAddrListBox.HideSelection = false;
            this.watchAddrListBox.Location = new System.Drawing.Point(533, 13);
            this.watchAddrListBox.Name = "watchAddrListBox";
            this.watchAddrListBox.ShowGroups = false;
            this.watchAddrListBox.Size = new System.Drawing.Size(230, 672);
            this.watchAddrListBox.TabIndex = 9;
            this.watchAddrListBox.UseCompatibleStateImageBehavior = false;
            this.watchAddrListBox.View = System.Windows.Forms.View.Details;
            // 
            // columnHeader1
            // 
            this.columnHeader1.Text = "Key";
            this.columnHeader1.Width = 104;
            // 
            // columnHeader4
            // 
            this.columnHeader4.Text = "Value";
            this.columnHeader4.Width = 103;
            // 
            // memoryListBox
            // 
            this.memoryListBox.BackColor = System.Drawing.SystemColors.ControlDarkDark;
            this.memoryListBox.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader2,
            this.columnHeader3});
            this.memoryListBox.ForeColor = System.Drawing.Color.White;
            this.memoryListBox.FullRowSelect = true;
            this.memoryListBox.HideSelection = false;
            this.memoryListBox.Location = new System.Drawing.Point(274, 13);
            this.memoryListBox.MultiSelect = false;
            this.memoryListBox.Name = "memoryListBox";
            this.memoryListBox.Size = new System.Drawing.Size(253, 672);
            this.memoryListBox.TabIndex = 10;
            this.memoryListBox.UseCompatibleStateImageBehavior = false;
            this.memoryListBox.View = System.Windows.Forms.View.Details;
            // 
            // columnHeader2
            // 
            this.columnHeader2.Text = "Key";
            this.columnHeader2.Width = 104;
            // 
            // columnHeader3
            // 
            this.columnHeader3.Text = "Value";
            this.columnHeader3.Width = 113;
            // 
            // watchViewContextMenu
            // 
            this.watchViewContextMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.removeSelectedToolStripMenuItem});
            this.watchViewContextMenu.Name = "watchViewContextMenu";
            this.watchViewContextMenu.Size = new System.Drawing.Size(165, 26);
            // 
            // removeSelectedToolStripMenuItem
            // 
            this.removeSelectedToolStripMenuItem.Name = "removeSelectedToolStripMenuItem";
            this.removeSelectedToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.removeSelectedToolStripMenuItem.Text = "Remove Selected";
            this.removeSelectedToolStripMenuItem.Click += new System.EventHandler(this.removeSelectedToolStripMenuItem_Click);
            // 
            // CPUDebugger
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.ClientSize = new System.Drawing.Size(794, 771);
            this.Controls.Add(this.memoryListBox);
            this.Controls.Add(this.watchAddrListBox);
            this.Controls.Add(this.watchAddrTxtBox);
            this.Controls.Add(this.Watch);
            this.Controls.Add(this.contAddrTxtBox);
            this.Controls.Add(this.continueBtn);
            this.Controls.Add(this.noBtn);
            this.Controls.Add(this.yesBtn);
            this.Controls.Add(this.displayBox);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MinimumSize = new System.Drawing.Size(292, 492);
            this.Name = "CPUDebugger";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "CPUDebugger";
            this.watchViewContextMenu.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.RichTextBox displayBox;
        private System.Windows.Forms.Button yesBtn;
        private System.Windows.Forms.Button noBtn;
        private System.Windows.Forms.Button continueBtn;
        private System.Windows.Forms.TextBox contAddrTxtBox;
        private System.Windows.Forms.Button Watch;
        private System.Windows.Forms.TextBox watchAddrTxtBox;
        private System.Windows.Forms.ListView watchAddrListBox;
        private System.Windows.Forms.ColumnHeader columnHeader1;
        private System.Windows.Forms.ListView memoryListBox;
        private System.Windows.Forms.ColumnHeader columnHeader2;
        private System.Windows.Forms.ColumnHeader columnHeader4;
        private System.Windows.Forms.ColumnHeader columnHeader3;
        private System.Windows.Forms.ContextMenuStrip watchViewContextMenu;
        private System.Windows.Forms.ToolStripMenuItem removeSelectedToolStripMenuItem;
    }
}