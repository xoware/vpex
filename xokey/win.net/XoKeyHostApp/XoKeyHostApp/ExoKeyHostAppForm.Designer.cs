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
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.exportLogToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveToFileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.debugToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.helpToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.aboutToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.webBrowser1 = new System.Windows.Forms.WebBrowser();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.PopupErrors_checkBox = new System.Windows.Forms.CheckBox();
            this.Log_dataGridView = new System.Windows.Forms.DataGridView();
            this.Date_Time_Col = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Code_Col = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Level_Col = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Message_Col = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Location_textBox = new System.Windows.Forms.TextBox();
            this.Go_button = new System.Windows.Forms.Button();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.toolStripStatusLabel1 = new System.Windows.Forms.ToolStripStatusLabel();
            this.toolStripProgressBar1 = new System.Windows.Forms.ToolStripProgressBar();
            this.menuStrip1.SuspendLayout();
            this.tabControl1.SuspendLayout();
            this.tabPage1.SuspendLayout();
            this.tabPage2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.Log_dataGridView)).BeginInit();
            this.statusStrip1.SuspendLayout();
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
            this.menuStrip1.Size = new System.Drawing.Size(1087, 28);
            this.menuStrip1.TabIndex = 0;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.exitToolStripMenuItem,
            this.exportLogToolStripMenuItem});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(44, 24);
            this.fileToolStripMenuItem.Text = "File";
            // 
            // exitToolStripMenuItem
            // 
            this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            this.exitToolStripMenuItem.Size = new System.Drawing.Size(150, 24);
            this.exitToolStripMenuItem.Text = "Exit";
            // 
            // exportLogToolStripMenuItem
            // 
            this.exportLogToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.saveToFileToolStripMenuItem});
            this.exportLogToolStripMenuItem.Name = "exportLogToolStripMenuItem";
            this.exportLogToolStripMenuItem.Size = new System.Drawing.Size(150, 24);
            this.exportLogToolStripMenuItem.Text = "Export Log";
            // 
            // saveToFileToolStripMenuItem
            // 
            this.saveToFileToolStripMenuItem.Name = "saveToFileToolStripMenuItem";
            this.saveToFileToolStripMenuItem.Size = new System.Drawing.Size(154, 24);
            this.saveToFileToolStripMenuItem.Text = "Save to File";
            this.saveToFileToolStripMenuItem.Click += new System.EventHandler(this.saveLogExportFileToolStripMenuItem_Click);
            // 
            // toolsToolStripMenuItem
            // 
            this.toolsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.debugToolStripMenuItem});
            this.toolsToolStripMenuItem.Name = "toolsToolStripMenuItem";
            this.toolsToolStripMenuItem.Size = new System.Drawing.Size(57, 24);
            this.toolsToolStripMenuItem.Text = "Tools";
            // 
            // debugToolStripMenuItem
            // 
            this.debugToolStripMenuItem.Name = "debugToolStripMenuItem";
            this.debugToolStripMenuItem.Size = new System.Drawing.Size(123, 24);
            this.debugToolStripMenuItem.Text = "Debug";
            this.debugToolStripMenuItem.Click += new System.EventHandler(this.debugToolStripMenuItem_Click);
            // 
            // helpToolStripMenuItem
            // 
            this.helpToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.aboutToolStripMenuItem});
            this.helpToolStripMenuItem.Name = "helpToolStripMenuItem";
            this.helpToolStripMenuItem.Size = new System.Drawing.Size(53, 24);
            this.helpToolStripMenuItem.Text = "Help";
            // 
            // aboutToolStripMenuItem
            // 
            this.aboutToolStripMenuItem.Name = "aboutToolStripMenuItem";
            this.aboutToolStripMenuItem.Size = new System.Drawing.Size(119, 24);
            this.aboutToolStripMenuItem.Text = "About";
            // 
            // tabControl1
            // 
            this.tabControl1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tabControl1.Controls.Add(this.tabPage1);
            this.tabControl1.Controls.Add(this.tabPage2);
            this.tabControl1.Location = new System.Drawing.Point(0, 64);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(1087, 434);
            this.tabControl1.TabIndex = 1;
            // 
            // tabPage1
            // 
            this.tabPage1.Controls.Add(this.webBrowser1);
            this.tabPage1.Location = new System.Drawing.Point(4, 25);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage1.Size = new System.Drawing.Size(1079, 405);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "Key";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // webBrowser1
            // 
            this.webBrowser1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.webBrowser1.Location = new System.Drawing.Point(3, 3);
            this.webBrowser1.MinimumSize = new System.Drawing.Size(20, 20);
            this.webBrowser1.Name = "webBrowser1";
            this.webBrowser1.Size = new System.Drawing.Size(1073, 399);
            this.webBrowser1.TabIndex = 0;
            this.webBrowser1.DocumentCompleted += new System.Windows.Forms.WebBrowserDocumentCompletedEventHandler(this.webBrowser1_DocumentCompleted);
            this.webBrowser1.Navigated += new System.Windows.Forms.WebBrowserNavigatedEventHandler(this.webBrowser1_Navigated);
            this.webBrowser1.ProgressChanged += new System.Windows.Forms.WebBrowserProgressChangedEventHandler(this.webBrowser1_ProgressChanged);
            // 
            // tabPage2
            // 
            this.tabPage2.Controls.Add(this.PopupErrors_checkBox);
            this.tabPage2.Controls.Add(this.Log_dataGridView);
            this.tabPage2.Location = new System.Drawing.Point(4, 25);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage2.Size = new System.Drawing.Size(1079, 405);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "Log";
            this.tabPage2.UseVisualStyleBackColor = true;
            // 
            // PopupErrors_checkBox
            // 
            this.PopupErrors_checkBox.AutoSize = true;
            this.PopupErrors_checkBox.Checked = true;
            this.PopupErrors_checkBox.CheckState = System.Windows.Forms.CheckState.Checked;
            this.PopupErrors_checkBox.Location = new System.Drawing.Point(20, 20);
            this.PopupErrors_checkBox.Name = "PopupErrors_checkBox";
            this.PopupErrors_checkBox.Size = new System.Drawing.Size(122, 21);
            this.PopupErrors_checkBox.TabIndex = 1;
            this.PopupErrors_checkBox.Text = "Popup Errors?";
            this.PopupErrors_checkBox.UseVisualStyleBackColor = true;
            // 
            // Log_dataGridView
            // 
            this.Log_dataGridView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.Log_dataGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.Log_dataGridView.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.Date_Time_Col,
            this.Code_Col,
            this.Level_Col,
            this.Message_Col});
            this.Log_dataGridView.Location = new System.Drawing.Point(0, 53);
            this.Log_dataGridView.Name = "Log_dataGridView";
            this.Log_dataGridView.RowTemplate.Height = 24;
            this.Log_dataGridView.Size = new System.Drawing.Size(1079, 380);
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
            this.Location_textBox.Location = new System.Drawing.Point(219, 36);
            this.Location_textBox.Name = "Location_textBox";
            this.Location_textBox.Size = new System.Drawing.Size(861, 22);
            this.Location_textBox.TabIndex = 2;
            this.Location_textBox.Text = "https://192.168.255.1/ui/login.html";
            // 
            // Go_button
            // 
            this.Go_button.Location = new System.Drawing.Point(172, 36);
            this.Go_button.Name = "Go_button";
            this.Go_button.Size = new System.Drawing.Size(41, 23);
            this.Go_button.TabIndex = 3;
            this.Go_button.Text = "Go";
            this.Go_button.UseVisualStyleBackColor = true;
            this.Go_button.Click += new System.EventHandler(this.Go_button_Click);
            // 
            // statusStrip1
            // 
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripStatusLabel1,
            this.toolStripProgressBar1});
            this.statusStrip1.Location = new System.Drawing.Point(0, 498);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new System.Drawing.Size(1087, 25);
            this.statusStrip1.TabIndex = 4;
            this.statusStrip1.Text = "statusStrip1";
            this.statusStrip1.ItemClicked += new System.Windows.Forms.ToolStripItemClickedEventHandler(this.statusStrip1_ItemClicked);
            // 
            // toolStripStatusLabel1
            // 
            this.toolStripStatusLabel1.Name = "toolStripStatusLabel1";
            this.toolStripStatusLabel1.Size = new System.Drawing.Size(180, 20);
            this.toolStripStatusLabel1.Text = "Web_toolStripStatusLabel";
            // 
            // toolStripProgressBar1
            // 
            this.toolStripProgressBar1.Name = "toolStripProgressBar1";
            this.toolStripProgressBar1.Size = new System.Drawing.Size(100, 19);
            // 
            // ExoKeyHostAppForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1087, 523);
            this.Controls.Add(this.statusStrip1);
            this.Controls.Add(this.Go_button);
            this.Controls.Add(this.Location_textBox);
            this.Controls.Add(this.tabControl1);
            this.Controls.Add(this.menuStrip1);
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "ExoKeyHostAppForm";
            this.Text = "XoKey";
            this.Load += new System.EventHandler(this.ExoKeyHostAppForm_Load);
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.tabControl1.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            this.tabPage2.ResumeLayout(false);
            this.tabPage2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.Log_dataGridView)).EndInit();
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
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
        private System.Windows.Forms.WebBrowser webBrowser1;
        private System.Windows.Forms.TabPage tabPage2;
        private System.Windows.Forms.TextBox Location_textBox;
        private System.Windows.Forms.ToolStripMenuItem debugToolStripMenuItem;
        private System.Windows.Forms.CheckBox PopupErrors_checkBox;
        private System.Windows.Forms.DataGridView Log_dataGridView;
        private System.Windows.Forms.Button Go_button;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel1;
        private System.Windows.Forms.DataGridViewTextBoxColumn Date_Time_Col;
        private System.Windows.Forms.DataGridViewTextBoxColumn Code_Col;
        private System.Windows.Forms.DataGridViewTextBoxColumn Level_Col;
        private System.Windows.Forms.DataGridViewTextBoxColumn Message_Col;
        private System.Windows.Forms.ToolStripMenuItem exportLogToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem saveToFileToolStripMenuItem;
        private System.Windows.Forms.ToolStripProgressBar toolStripProgressBar1;
    }
}

