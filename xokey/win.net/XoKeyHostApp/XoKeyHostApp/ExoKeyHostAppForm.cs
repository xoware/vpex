using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.IO;
using System.Text;
using System.Net;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Windows.Forms;
using System.Management;
using Microsoft.Win32;
using CefSharp.WinForms;



namespace XoKeyHostApp
{
    public partial class ExoKeyHostAppForm : Form
    {
        

        XoKey xokey;
        private readonly WebView web_view;
        private bool USB_Dev_ID_Found = false;

        public ExoKeyHostAppForm()
        {

          //  Registry.SetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Internet Explorer\MAIN\FeatureControl\FEATURE_BROWSER_EMULATION", "WindowsFormsApplication1.exe", value);
            //Registry.SetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Internet Explorer\MAIN\FeatureControl\FEATURE_BROWSER_EMULATION",  "WindowsFormsApplication1.vshost.exe", value);

            
            
         //   Registry.SetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Internet Explorer\MAIN\FeatureControl\FEATURE_BROWSER_EMULATION",    System.AppDomain.CurrentDomain.FriendlyName, 10000);

            InitializeComponent();

            // Subscribe to Event(s) with the WindowsInterop Class
            WindowsInterop.SecurityAlertDialogWillBeShown +=
                new GenericDelegate<Boolean, Boolean>(this.WindowsInterop_SecurityAlertDialogWillBeShown);

            WindowsInterop.ConnectToDialogWillBeShown +=
                new GenericDelegate<String, String, Boolean>(this.WindowsInterop_ConnectToDialogWillBeShown);
           // System.Drawing.Icon ico = Properties.Resources.
           // this.Icon = ico;

            ServicePointManager.ServerCertificateValidationCallback = new System.Net.Security.RemoteCertificateValidationCallback(AcceptAllCertifications);

            web_view = new WebView("", Get_Chrome_Settings());
            web_view.Dock = DockStyle.Fill;
            tabPage1.Controls.Add(web_view);
            web_view.ConsoleMessage += web_view_ConsoleMessage;
            web_view.LoadCompleted += web_view_LoadCompleted;
            web_view.LocationChanged += web_view_LocationChanged; 
        }
        

        void web_view_LocationChanged(object sender, EventArgs e)
        {
            __Log_Msg(0, LogMsg.Priority.Debug, "LocationChanged: " + web_view.Location + "  e:" + e.ToString() );
        }

        void web_view_LoadCompleted(object sender, CefSharp.LoadCompletedEventArgs url)
        {
            if (url.Url.Contains("cef-error"))
            {
                __Log_Msg(0, LogMsg.Priority.Debug, "Error loading UI");
            }
            __Log_Msg(0, LogMsg.Priority.Debug, "LoadCompleted: " + url.Url);
            web_view.ExecuteScript("console.log('XOEvent:Cookie:' +document.cookie);");
            // disable right click
            web_view.ExecuteScript("window.oncontextmenu = function() { return false; };");

            
        }

        void Parse_Console_Log_Event(String Event_Msg)
        {
            const String Cookie_Marker = "XOEvent:Cookie:";

            if (Event_Msg.IndexOf(Cookie_Marker) == 0)
            {
                
                xokey.Set_Session_Cookie(Event_Msg.Substring(Cookie_Marker.Length));
            }
        }

        void web_view_ConsoleMessage(object sender, CefSharp.ConsoleMessageEventArgs e)
        {
            __Log_Msg(0, LogMsg.Priority.Debug, "Console: " + e.Message);
            if (e.Message.IndexOf("XOEvent:") != -1)
            {
                Parse_Console_Log_Event(e.Message);
            }
        }

        CefSharp.BrowserSettings Get_Chrome_Settings()
        {
            CefSharp.BrowserSettings settings = new CefSharp.BrowserSettings();

            return settings;
        }



        public bool AcceptAllCertifications(object sender, System.Security.Cryptography.X509Certificates.X509Certificate certification, System.Security.Cryptography.X509Certificates.X509Chain chain, System.Net.Security.SslPolicyErrors sslPolicyErrors)
        {   // Accept all SSL certs
            return true;
        }
        /*
        private void check_registry()
        { 
            
            // set browser emulation to IE10 http://msdn.microsoft.com/en-us/library/ee330730(v=vs.85).aspx
            const int IE_Val = 10000;
            int val = (int) Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Internet Explorer\MAIN\FeatureControl\FEATURE_BROWSER_EMULATION",
                        System.AppDomain.CurrentDomain.FriendlyName, 0);
            __Log_Msg(0, LogMsg.Priority.Debug, "FEATURE_BROWSER_EMULATION " + val.ToString());
            var ieVersion = Registry.LocalMachine.OpenSubKey(@"Software\Microsoft\Internet Explorer").GetValue("Version");
            __Log_Msg(0, LogMsg.Priority.Debug, "ieVersion " + ieVersion.ToString());
       //     __Log_Msg(0, LogMsg.Priority.Debug, "webBrowser.version " + webBrowser1.Version.ToString());

            if (val != IE_Val)
            {
                // set browser emulation http://msdn.microsoft.com/en-us/library/ee330730(v=vs.85).aspx
                Registry.SetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Internet Explorer\MAIN\FeatureControl\FEATURE_BROWSER_EMULATION",
                 System.AppDomain.CurrentDomain.FriendlyName, IE_Val);
                val = (int)Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Internet Explorer\MAIN\FeatureControl\FEATURE_BROWSER_EMULATION",
                        System.AppDomain.CurrentDomain.FriendlyName, 0);
                if (val == IE_Val)
                {
                    MessageBox.Show("Registry updated on 1st run, restart app");
                }
                else
                {
                    MessageBox.Show("Error updating registry");
                }


            }
 
        }
         **/
        private void EK_Address_Change_Handler(IPAddress addr)
        {
            __Log_Msg(0, LogMsg.Priority.Debug, "EK_Address_Change_Handler ip= "+ addr.ToString());
            Load_Url("https://" + addr.ToString() + "/ui/login.html");
        }
        private void ExoKeyHostAppForm_Load(object sender, EventArgs e)
        {
            __Log_Msg(0, LogMsg.Priority.Debug, "Startup " + System.AppDomain.CurrentDomain.FriendlyName);
           // check_registry();
            Set_Debug(Properties.Settings.Default.Debug);
            xokey = new XoKey(invoke => Invoke(invoke), Recv_Log_Msg);
            xokey.EK_IP_Address_Detected += EK_Address_Change_Handler;
            xokey.Startup();
            Search_For_ExoKey();
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
        private void Set_Debug(bool value)
        {
            debugToolStripMenuItem.Checked = value;
            Go_button.Visible = value;
            Location_textBox.Visible = value;

        }

        private void debugToolStripMenuItem_Click(object sender, EventArgs e)
        {
            debugToolStripMenuItem.Checked = !debugToolStripMenuItem.Checked;  // toggle
            Set_Debug(debugToolStripMenuItem.Checked);
            Properties.Settings.Default.Debug = debugToolStripMenuItem.Checked;
            Properties.Settings.Default.Save();
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
        private void Search_For_ExoKey()
        {
            Search_USB_Devices();
            while (!USB_Dev_ID_Found)
            {
                break; // FIXME remove this

                DialogResult result = MessageBox.Show("Please Insert ExoKey, wait for startup and click Retry, or abort to exit",
                    "ExoKey not found on USB port",
                    MessageBoxButtons.AbortRetryIgnore);

                if (result == System.Windows.Forms.DialogResult.Cancel || result == System.Windows.Forms.DialogResult.Abort)
                {
                    this.Close();
                    break;
                }
                if (result == System.Windows.Forms.DialogResult.Ignore)
                {
                    break;
                }
                 Search_USB_Devices();
            }

        }
        private void Search_USB_Devices()
        {

            ManagementObjectCollection collection;
            using (var searcher = new ManagementObjectSearcher(@"Select * From Win32_USBControllerDevice")) // Win32_USBDevice Win32_USBHub
                collection = searcher.Get();

            foreach (var device in collection)            
            {

//                Console.WriteLine(device.ToString());
                PropertyDataCollection properties = device.Properties;
                foreach (PropertyData property in properties)
                {
//                    Console.WriteLine("Name=" + property.Name + " Value = " +
//                        (property.Value == null ? "null" :property.Value.ToString()));

                    if (property.Value != null)
                    {
                        if (property.Value.ToString().Contains("VID_0525&PID_A4A2")) {
                            System.Diagnostics.Debug.WriteLine("Found: VID_0525&PID_A4A2");
                            USB_Dev_ID_Found = true;
                        }
                    }
                    /*
                    foreach (QualifierData q in property.Qualifiers)
                    {
    
                        Console.WriteLine("Name="+  property.Name +" qualifier="+ q.Name.ToString() + " Val=" + q.Value.ToString());

                    }
                    Console.WriteLine();
                     **/
                }
       
                /*
                devices.Add(new USBDeviceInfo(
                (string)device.GetPropertyValue("DeviceID"),
                (string)device.GetPropertyValue("PNPDeviceID"),
                (string)device.GetPropertyValue("Description")
                ));
                 * */
            }
            collection.Dispose();

     
         //   return devices;
        }
        private void saveLogExportFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog Save_File_Dialog = new SaveFileDialog();

            Save_File_Dialog.Filter = "csv files (*.csv)|*.csv ";
            Save_File_Dialog.FilterIndex = 1;
            Save_File_Dialog.RestoreDirectory = true;

            if (Save_File_Dialog.ShowDialog() == DialogResult.OK)
            {
                System.IO.Stream File_Stream;
                if ((File_Stream = Save_File_Dialog.OpenFile()) != null)
                {
                    lock (Log_dataGridView)
                    {
                        for (int i = (Log_dataGridView.RowCount - 1); i >= 0; i--)
                        {
                            String Buffer;

                            try
                            {
                                // time
                                Buffer = Log_dataGridView.Rows[i].Cells[(int)Date_Time_Col.Index].Value.ToString() + "\t";

                                // code
                                Buffer += Log_dataGridView.Rows[i].Cells[(int)Code_Col.Index].Value.ToString() + "\t";

                                // level
                                Buffer += (string)Log_dataGridView.Rows[i].Cells[(int)Level_Col.DisplayIndex].Value.ToString() + "\t";

                                // message
                                Buffer += (string)Log_dataGridView.Rows[i].Cells[(int)Message_Col.Index].Value.ToString() + "\r\n";

                                byte[] data = new UTF8Encoding(true).GetBytes(Buffer);
                                File_Stream.Write(data, 0, data.Length);
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine("Write log exception " + ex.Message);
                            }
                        }
                    }

                    File_Stream.Close();
                    MessageBox.Show("File saved to " + Save_File_Dialog.FileName);
                }
                else
                {
                    File_Stream = null;
                    MessageBox.Show("Error creating export file");
                }

            }

        }
        private void webBrowser1_Navigated(object sender, WebBrowserNavigatedEventArgs e)
        {
       //     Location_textBox.Text = webBrowser1.Url.ToString();
      //      __Log_Msg(0, LogMsg.Priority.Debug, "Navigated:" + webBrowser1.Url.ToString());
        }
        public void Load_Url(string address)
        {
            try
            {
                web_view.Load(address);
            }
            catch (System.UriFormatException)
            {
                __Log_Msg(0, LogMsg.Priority.Error, "URI Format Error");
                return;
            }
        }
        private void Navigate()
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
                web_view.Load(address);
            }
            catch (System.UriFormatException)
            {
                __Log_Msg(0, LogMsg.Priority.Error, "URI Format Error");
                return;
            }
            catch (System.Exception ex)
            {
                __Log_Msg(1, LogMsg.Priority.Error, ex.Message);
            }
        }
        private void Go_button_Click(object sender, EventArgs e)
        {
            Navigate();
        }

        private void statusStrip1_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {

        }

        private void webBrowser1_ProgressChanged(object sender, WebBrowserProgressChangedEventArgs e)
        {
            System.Text.StringBuilder messageBoxCS = new System.Text.StringBuilder();
            /*
            messageBoxCS.AppendFormat("{0} = {1} ", "CurrentProgress", e.CurrentProgress);
            messageBoxCS.AppendLine();
            messageBoxCS.AppendFormat(" {0} = {1}", "MaximumProgress", e.MaximumProgress);
            messageBoxCS.AppendLine();
            __Log_Msg(0, LogMsg.Priority.Debug, "ProgressChanged Event"+ messageBoxCS.ToString());
             */
            if (e.MaximumProgress > 0)
                toolStripProgressBar1.Value = (int)Math.Round((double)(e.CurrentProgress / e.MaximumProgress) * 100);
            else
                toolStripProgressBar1.Value = 100;
        }
        /*
        private void webBrowser1_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            __Log_Msg(0, LogMsg.Priority.Debug, "webBrowser1_DocumentCompleted" + e.Url.ToString());
            if (webBrowser1.Document.Cookie != null)
            {
                __Log_Msg(0, LogMsg.Priority.Debug, "Cookie: " + webBrowser1.Document.Cookie.ToString());
                xokey.Set_Session_Cookie(webBrowser1.Document.Cookie);
  
            }
        }
        */
    

        
        

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AboutBox1 box = new AboutBox1();

            box.ShowDialog();
        }

        private void ExoKeyHostAppForm_FormClosing(object sender, FormClosingEventArgs e)
        {
     //       web_view.Dispose();
            if (xokey != null)
            {
                xokey.Dispose();
                xokey = null;
            }
            this.Dispose();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void devToolsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            web_view.ShowDevTools();
        }
   
    }
}
