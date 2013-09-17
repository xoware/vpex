using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace XoKeyHostApp
{
    public partial class ExoKeyHostAppForm : Form
    {
        public ExoKeyHostAppForm()
        {
            InitializeComponent();

            // Subscribe to Event(s) with the WindowsInterop Class
            WindowsInterop.SecurityAlertDialogWillBeShown +=
                new GenericDelegate<Boolean, Boolean>(this.WindowsInterop_SecurityAlertDialogWillBeShown);

            WindowsInterop.ConnectToDialogWillBeShown +=
                new GenericDelegate<String, String, Boolean>(this.WindowsInterop_ConnectToDialogWillBeShown);
            
        }
        private void ExoKeyHostAppForm_Load(object sender, EventArgs e)
        {
            __Log_Msg(0, LogMsg.Priority.Debug, "Startup");
        }
        private void __Log_Msg(int code, LogMsg.Priority level, String message)
        {
            LogMsg Msg = new LogMsg(message, level, code);
            Recv_Log_Msg(Msg);
        }
        // Asyncronously pass the message to the main thread
        public void Recv_Log_Msg(LogMsg Msg)
        {

            // Add_Log_Entry_Callback logger = new Add_Log_Entry_Callback(Add_Log_Entry);
            //logger.Invoke(Msg);
            //   Log_dataGridView.Invoke(new Add_Log_Entry_Callback(Add_Log_Entry),
            //      new object[] { Msg });

            try
            {

                Log_Msg_Handler callback = new Log_Msg_Handler(Add_Log_Entry);
                Log_dataGridView.Invoke(callback, new object[] { Msg });
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine("Recv_Log_Msg exception " + e.Message);
            }

        }
        private Boolean WindowsInterop_SecurityAlertDialogWillBeShown(Boolean blnIsSSLDialog)
        {
            // Return true to ignore and not show the 
            // "Security Alert" dialog to the user
            return true;
        }

        private Boolean WindowsInterop_ConnectToDialogWillBeShown(ref String sUsername, ref String sPassword)
        {
            // (Fill in the blanks in order to be able 
            // to return the appropriate Username and Password)
            sUsername = "";
            sPassword = "";

            // Return true to auto populate credentials and not 
            // show the "Connect To ..." dialog to the user
            return true;
        }
        private void debugToolStripMenuItem_Click(object sender, EventArgs e)
        {
            debugToolStripMenuItem.Checked = !debugToolStripMenuItem.Checked;  // toggle

            Go_button.Visible = debugToolStripMenuItem.Checked;
            Location_textBox.Visible = debugToolStripMenuItem.Checked;
        }

        public void Add_Log_Entry(LogMsg Msg)
        {

            if (PopupErrors_checkBox.Checked && Msg.Level <= LogMsg.Priority.Error)
                MessageBox.Show(Msg.Message);



            //Log_Message_List.Add(Msg);
            lock (Log_dataGridView)
            {
                Log_dataGridView.Rows.Insert(0, 1);
                Log_dataGridView.Rows[0].Cells[0].Value = Msg.Time;
                Log_dataGridView.Rows[0].Cells[1].Value = Msg.Code;
                Log_dataGridView.Rows[0].Cells[2].Value = Msg.Level;
                Log_dataGridView.Rows[0].Cells[3].Value = Msg.Message;
            }
            //Log_dataGridView.Invoke(new Update_Log_Data_Grid_Callback(Update_Log_Data_Grid) );


        }

        private void webBrowser1_Navigated(object sender, WebBrowserNavigatedEventArgs e)
        {
            Location_textBox.Text = webBrowser1.Url.ToString();
        }

        private void Go_button_Click(object sender, EventArgs e)
        {
            String address = Location_textBox.Text;

            if (String.IsNullOrEmpty(address)) return;
            if (address.Equals("about:blank")) return;
            if (!address.StartsWith("http://") &&
                !address.StartsWith("https://"))
            {
                address = "http://" + address;
            }
            __Log_Msg(0, LogMsg.Priority.Debug, "Navigate:" + address);
            try
            {
                webBrowser1.Navigate(new Uri(address));
            }
            catch (System.UriFormatException)
            {
                return;
            }
        }

        private void statusStrip1_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {

        }

        private void webBrowser1_ProgressChanged(object sender, WebBrowserProgressChangedEventArgs e)
        {
            System.Text.StringBuilder messageBoxCS = new System.Text.StringBuilder();
            messageBoxCS.AppendFormat("{0} = {1}", "CurrentProgress", e.CurrentProgress);
            messageBoxCS.AppendLine();
            messageBoxCS.AppendFormat("{0} = {1}", "MaximumProgress", e.MaximumProgress);
            messageBoxCS.AppendLine();
            __Log_Msg(0, LogMsg.Priority.Debug, "ProgressChanged Event"+ messageBoxCS.ToString());
        }



   
    }
}
