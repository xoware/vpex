namespace XoKeyHostApp
{
    partial class ExoKeyHostAppForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ExoKeyHostAppForm));
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.exportLogToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveToFileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.debugToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.devToolsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.helpToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.aboutToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.Log_tabPage = new System.Windows.Forms.TabPage();
            this.PopupErrors_checkBox = new System.Windows.Forms.CheckBox();
            this.Log_dataGridView = new System.Windows.Forms.DataGridView();
            this.Date_Time_Col = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Code_Col = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Level_Col = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Message_Col = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Location_textBox = new System.Windows.Forms.TextBox();
            this.Go_button = new System.Windows.Forms.Button();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.notifyIcon1 = new System.Windows.Forms.NotifyIcon(this.components);
            this.contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.menuStrip1.SuspendLayout();
            this.tabControl1.SuspendLayout();
            this.Log_tabPage.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.Log_dataGridView)).BeginInit();
            this.SuspendLayout();
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.toolsToolStripMenuItem,
            this.helpToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Padding = new System.Windows.Forms.Padding(4, 2, 0, 2);
            this.menuStrip1.Size = new System.Drawing.Size(875, 24);
            this.menuStrip1.TabIndex = 0;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.exitToolStripMenuItem,
            this.exportLogToolStripMenuItem});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
            this.fileToolStripMenuItem.Text = "File";
            // 
            // exitToolStripMenuItem
            // 
            this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            this.exitToolStripMenuItem.Size = new System.Drawing.Size(130, 22);
            this.exitToolStripMenuItem.Text = "Exit";
            this.exitToolStripMenuItem.Click += new System.EventHandler(this.exitToolStripMenuItem_Click);
            // 
            // exportLogToolStripMenuItem
            // 
            this.exportLogToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.saveToFileToolStripMenuItem});
            this.exportLogToolStripMenuItem.Name = "exportLogToolStripMenuItem";
            this.exportLogToolStripMenuItem.Size = new System.Drawing.Size(130, 22);
            this.exportLogToolStripMenuItem.Text = "Export Log";
            // 
            // saveToFileToolStripMenuItem
            // 
            this.saveToFileToolStripMenuItem.Name = "saveToFileToolStripMenuItem";
            this.saveToFileToolStripMenuItem.Size = new System.Drawing.Size(133, 22);
            this.saveToFileToolStripMenuItem.Text = "Save to File";
            this.saveToFileToolStripMenuItem.Click += new System.EventHandler(this.saveLogExportFileToolStripMenuItem_Click);
            // 
            // toolsToolStripMenuItem
            // 
            this.toolsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.debugToolStripMenuItem,
            this.devToolsToolStripMenuItem});
            this.toolsToolStripMenuItem.Name = "toolsToolStripMenuItem";
            this.toolsToolStripMenuItem.Size = new System.Drawing.Size(48, 20);
            this.toolsToolStripMenuItem.Text = "Tools";
            // 
            // debugToolStripMenuItem
            // 
            this.debugToolStripMenuItem.Name = "debugToolStripMenuItem";
            this.debugToolStripMenuItem.Size = new System.Drawing.Size(126, 22);
            this.debugToolStripMenuItem.Text = "Debug";
            this.debugToolStripMenuItem.Click += new System.EventHandler(this.debugToolStripMenuItem_Click);
            // 
            // devToolsToolStripMenuItem
            // 
            this.devToolsToolStripMenuItem.Name = "devToolsToolStripMenuItem";
            this.devToolsToolStripMenuItem.Size = new System.Drawing.Size(126, 22);
            this.devToolsToolStripMenuItem.Text = "Dev Tools";
            this.devToolsToolStripMenuItem.Click += new System.EventHandler(this.devToolsToolStripMenuItem_Click);
            // 
            // helpToolStripMenuItem
            // 
            this.helpToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.aboutToolStripMenuItem});
            this.helpToolStripMenuItem.Name = "helpToolStripMenuItem";
            this.helpToolStripMenuItem.Size = new System.Drawing.Size(44, 20);
            this.helpToolStripMenuItem.Text = "Help";
            // 
            // aboutToolStripMenuItem
            // 
            this.aboutToolStripMenuItem.Name = "aboutToolStripMenuItem";
            this.aboutToolStripMenuItem.Size = new System.Drawing.Size(107, 22);
            this.aboutToolStripMenuItem.Text = "About";
            this.aboutToolStripMenuItem.Click += new System.EventHandler(this.aboutToolStripMenuItem_Click);
            // 
            // tabControl1
            // 
            this.tabControl1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tabControl1.Controls.Add(this.tabPage1);
            this.tabControl1.Controls.Add(this.Log_tabPage);
            this.tabControl1.Location = new System.Drawing.Point(0, 25);
            this.tabControl1.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(875, 438);
            this.tabControl1.TabIndex = 1;
            // 
            // tabPage1
            // 
            this.tabPage1.Location = new System.Drawing.Point(4, 22);
            this.tabPage1.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.tabPage1.Size = new System.Drawing.Size(867, 412);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "Key";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // Log_tabPage
            // 
            this.Log_tabPage.Controls.Add(this.PopupErrors_checkBox);
            this.Log_tabPage.Controls.Add(this.Log_dataGridView);
            this.Log_tabPage.Location = new System.Drawing.Point(4, 22);
            this.Log_tabPage.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.Log_tabPage.Name = "Log_tabPage";
            this.Log_tabPage.Padding = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.Log_tabPage.Size = new System.Drawing.Size(867, 412);
            this.Log_tabPage.TabIndex = 1;
            this.Log_tabPage.Text = "Log";
            this.Log_tabPage.UseVisualStyleBackColor = true;
            // 
            // PopupErrors_checkBox
            // 
            this.PopupErrors_checkBox.AutoSize = true;
            this.PopupErrors_checkBox.Checked = true;
            this.PopupErrors_checkBox.CheckState = System.Windows.Forms.CheckState.Checked;
            this.PopupErrors_checkBox.Location = new System.Drawing.Point(15, 16);
            this.PopupErrors_checkBox.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.PopupErrors_checkBox.Name = "PopupErrors_checkBox";
            this.PopupErrors_checkBox.Size = new System.Drawing.Size(93, 17);
            this.PopupErrors_checkBox.TabIndex = 1;
            this.PopupErrors_checkBox.Text = "Popup Errors?";
            this.PopupErrors_checkBox.UseVisualStyleBackColor = true;
            // 
            // Log_dataGridView
            // 
            this.Log_dataGridView.AccessibleRole = System.Windows.Forms.AccessibleRole.TitleBar;
            this.Log_dataGridView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.Log_dataGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.Log_dataGridView.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.Date_Time_Col,
            this.Code_Col,
            this.Level_Col,
            this.Message_Col});
            this.Log_dataGridView.Location = new System.Drawing.Point(2, 38);
            this.Log_dataGridView.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.Log_dataGridView.Name = "Log_dataGridView";
            this.Log_dataGridView.RowTemplate.Height = 24;
            this.Log_dataGridView.Size = new System.Drawing.Size(865, 353);
            this.Log_dataGridView.TabIndex = 0;
            // 
            // Date_Time_Col
            // 
            this.Date_Time_Col.HeaderText = "Time";
            this.Date_Time_Col.Name = "Date_Time_Col";
            this.Date_Time_Col.ReadOnly = true;
            this.Date_Time_Col.Width = 175;
            // 
            // Code_Col
            // 
            this.Code_Col.HeaderText = "Code";
            this.Code_Col.Name = "Code_Col";
            this.Code_Col.ReadOnly = true;
            this.Code_Col.Width = 50;
            // 
            // Level_Col
            // 
            this.Level_Col.HeaderText = "Level";
            this.Level_Col.Name = "Level_Col";
            this.Level_Col.ReadOnly = true;
            this.Level_Col.Width = 75;
            // 
            // Message_Col
            // 
            this.Message_Col.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.Message_Col.HeaderText = "Message";
            this.Message_Col.Name = "Message_Col";
            this.Message_Col.ReadOnly = true;
            // 
            // Location_textBox
            // 
            this.Location_textBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.Location_textBox.Location = new System.Drawing.Point(273, 5);
            this.Location_textBox.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.Location_textBox.Name = "Location_textBox";
            this.Location_textBox.Size = new System.Drawing.Size(533, 20);
            this.Location_textBox.TabIndex = 2;
            this.Location_textBox.Text = "https://192.168.255.1/ek/login.html";
            this.Location_textBox.Enter += new System.EventHandler(this.Location_textBox_Enter);
            // 
            // Go_button
            // 
            this.Go_button.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.Go_button.Location = new System.Drawing.Point(238, 15);
            this.Go_button.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.Go_button.Name = "Go_button";
            this.Go_button.Size = new System.Drawing.Size(31, 19);
            this.Go_button.TabIndex = 3;
            this.Go_button.Text = "Go";
            this.Go_button.UseVisualStyleBackColor = true;
            this.Go_button.Visible = false;
            this.Go_button.Click += new System.EventHandler(this.Go_button_Click);
            // 
            // statusStrip1
            // 
            this.statusStrip1.Location = new System.Drawing.Point(0, 461);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Padding = new System.Windows.Forms.Padding(1, 0, 10, 0);
            this.statusStrip1.Size = new System.Drawing.Size(875, 22);
            this.statusStrip1.TabIndex = 4;
            this.statusStrip1.Text = "statusStrip1";
            this.statusStrip1.ItemClicked += new System.Windows.Forms.ToolStripItemClickedEventHandler(this.statusStrip1_ItemClicked);
            // 
            // notifyIcon1
            // 
            this.notifyIcon1.Icon = ((System.Drawing.Icon)(resources.GetObject("notifyIcon1.Icon")));
            this.notifyIcon1.Text = "ExoKey";
            this.notifyIcon1.Visible = true;
            this.notifyIcon1.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.notifyIcon1_MouseDoubleClick);
            // 
            // contextMenuStrip1
            // 
            this.contextMenuStrip1.Name = "contextMenuStrip1";
            this.contextMenuStrip1.Size = new System.Drawing.Size(61, 4);
            // 
            // ExoKeyHostAppForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
            this.ClientSize = new System.Drawing.Size(875, 483);
            this.Controls.Add(this.statusStrip1);
            this.Controls.Add(this.Go_button);
            this.Controls.Add(this.Location_textBox);
            this.Controls.Add(this.tabControl1);
            this.Controls.Add(this.menuStrip1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MainMenuStrip = this.menuStrip1;
            this.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.Name = "ExoKeyHostAppForm";
            this.Text = "ExoKey";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.ExoKeyHostAppForm_FormClosing);
            this.Load += new System.EventHandler(this.ExoKeyHostAppForm_Load);
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.tabControl1.ResumeLayout(false);
            this.Log_tabPage.ResumeLayout(false);
            this.Log_tabPage.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.Log_dataGridView)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem toolsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem helpToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem aboutToolStripMenuItem;
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.TabPage Log_tabPage;
        private System.Windows.Forms.TextBox Location_textBox;
        private System.Windows.Forms.ToolStripMenuItem debugToolStripMenuItem;
        private System.Windows.Forms.CheckBox PopupErrors_checkBox;
        private System.Windows.Forms.DataGridView Log_dataGridView;
        private System.Windows.Forms.Button Go_button;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.DataGridViewTextBoxColumn Date_Time_Col;
        private System.Windows.Forms.DataGridViewTextBoxColumn Code_Col;
        private System.Windows.Forms.DataGridViewTextBoxColumn Level_Col;
        private System.Windows.Forms.DataGridViewTextBoxColumn Message_Col;
        private System.Windows.Forms.ToolStripMenuItem exportLogToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem saveToFileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem devToolsToolStripMenuItem;
        private System.Windows.Forms.NotifyIcon notifyIcon1;
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip1;
    }
}

