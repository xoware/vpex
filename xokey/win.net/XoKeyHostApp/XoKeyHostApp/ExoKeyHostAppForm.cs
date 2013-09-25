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
using Microsoft.Win32;



namespace XoKeyHostApp
{
    public partial class ExoKeyHostAppForm : Form
    {
        private string Session_Cookie = "";
        private IPAddress XoKey_IP = IPAddress.Parse("192.168.255.1");
        private IPAddress Client_USB_IP = IPAddress.Parse("192.168.255.2");
        BackgroundWorker bw = null;
        Xoware.SocksServerLib.SocksListener Socks_Listener = null;

        public ExoKeyHostAppForm()
        {

          //  Registry.SetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Internet Explorer\MAIN\FeatureControl\FEATURE_BROWSER_EMULATION", "WindowsFormsApplication1.exe", value);
            //Registry.SetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Internet Explorer\MAIN\FeatureControl\FEATURE_BROWSER_EMULATION",  "WindowsFormsApplication1.vshost.exe", value);

            // set browser emulation to IE9 http://msdn.microsoft.com/en-us/library/ee330730(v=vs.85).aspx
            
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
        }

        public bool AcceptAllCertifications(object sender, System.Security.Cryptography.X509Certificates.X509Certificate certification, System.Security.Cryptography.X509Certificates.X509Chain chain, System.Net.Security.SslPolicyErrors sslPolicyErrors)
        {   // Accept all SSL certs
            return true;
        }
        private void check_registry()
        {
            const int IE_Val = 10000;
            int val = (int) Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Internet Explorer\MAIN\FeatureControl\FEATURE_BROWSER_EMULATION",
                        System.AppDomain.CurrentDomain.FriendlyName, 0);
            __Log_Msg(0, LogMsg.Priority.Debug, "FEATURE_BROWSER_EMULATION " + val.ToString());
            var ieVersion = Registry.LocalMachine.OpenSubKey(@"Software\Microsoft\Internet Explorer").GetValue("Version");
            __Log_Msg(0, LogMsg.Priority.Debug, "ieVersion " + ieVersion.ToString());
            __Log_Msg(0, LogMsg.Priority.Debug, "webBrowser.version " + webBrowser1.Version.ToString());

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
        private void ExoKeyHostAppForm_Load(object sender, EventArgs e)
        {
            __Log_Msg(0, LogMsg.Priority.Debug, "Startup " + System.AppDomain.CurrentDomain.FriendlyName);
            check_registry();
            Set_Debug(Properties.Settings.Default.Debug);
            if (!Properties.Settings.Default.Debug)
                Navigate();
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
            Location_textBox.Text = webBrowser1.Url.ToString();
            __Log_Msg(0, LogMsg.Priority.Debug, "Navigated:" + webBrowser1.Url.ToString());
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
                webBrowser1.Navigate(new Uri(address));
            }
            catch (System.UriFormatException)
            {
                __Log_Msg(0, LogMsg.Priority.Error, "URI Format Error");
                return;
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
        private Cookie Cookie_Str_To_Cookie(String Cook_Str)
        {

            string []vals = Cook_Str.Split('=');

            if (vals.Length != 2)
            {
                __Log_Msg(0, LogMsg.Priority.Error, "Parsing cookie string" + Cook_Str);
                return null;

            }
            Cookie cook = new Cookie(vals[0], vals[1]);
            cook.Domain = XoKey_IP.ToString();
            cook.Path = "/";
            return cook;
        }
        private void webBrowser1_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            __Log_Msg(0, LogMsg.Priority.Debug, "webBrowser1_DocumentCompleted" + e.Url.ToString());
            if (webBrowser1.Document.Cookie != null)
            {
                __Log_Msg(0, LogMsg.Priority.Debug, "Cookie: " + webBrowser1.Document.Cookie.ToString());
                Session_Cookie = webBrowser1.Document.Cookie;
                if (bw == null)
                {
                    bw = new BackgroundWorker();
                    bw.DoWork += Background_Init_Socks;
                    bw.RunWorkerAsync();
                }
            }
        }

        private void Ping_For_Client_IP()
        {
            WebRequest wr = WebRequest.Create("https://"+ XoKey_IP.ToString() + "/api/Ping");
            wr.Method = "GET";
            WebResponse response = wr.GetResponse();
            __Log_Msg(0, LogMsg.Priority.Debug, "Ping_For_Client_IP: status=" + ((HttpWebResponse)response).StatusDescription);

            // Get the stream containing content returned by the server.
            Stream dataStream = response.GetResponseStream();
            // Open the stream using a StreamReader for easy access.
//            StreamReader reader = new StreamReader(dataStream);
            // Read the content.
  //          string responseFromServer = reader.ReadToEnd();
  //          __Log_Msg(0, LogMsg.Priority.Debug, "Ping_For_Client_IP: responseFromServer=" + responseFromServer);

            DataContractJsonSerializer jsonSerializer = new DataContractJsonSerializer(typeof(XoKeyApi.PingResponse));
            object objResp = jsonSerializer.ReadObject(dataStream);
            XoKeyApi.PingResponse ping_response = objResp as XoKeyApi.PingResponse;

            if (ping_response != null && ping_response.client_ip.Length > 4)
            {
                Client_USB_IP = IPAddress.Parse(ping_response.client_ip);
            }
      //      reader.Close(); // cleanup
            response.Close(); // cleanup;
        }
        private void Set_XoKey_Socks_Server()
        {
            try
            {
                HttpWebRequest wr = (HttpWebRequest ) WebRequest.Create("https://" + XoKey_IP.ToString() + "/api/SetSocksServer?host="
                        + Client_USB_IP.ToString() + "&port=" + Properties.Settings.Default.Socks_Port);
                wr.Method = "GET";
                wr.CookieContainer = new CookieContainer();
                Cookie cook = Cookie_Str_To_Cookie(Session_Cookie);
                wr.CookieContainer.Add(cook);

                WebResponse response = wr.GetResponse();
                __Log_Msg(0, LogMsg.Priority.Debug, "Set_XoKey_Socks_Server: status=" + ((HttpWebResponse)response).StatusDescription);

                // Get the stream containing content returned by the server.
                Stream dataStream = response.GetResponseStream();

                DataContractJsonSerializer jsonSerializer = new DataContractJsonSerializer(typeof(XoKeyApi.RespMsg));
                object objResp = jsonSerializer.ReadObject(dataStream);
                XoKeyApi.RespMsg resp_msg = objResp as XoKeyApi.RespMsg;

                if (resp_msg.ack.status != 0)
                {
                    __Log_Msg(1, LogMsg.Priority.Error, "Set_XoKey_Socks_Server: Error setting SOCKS proxy");
                }
                else
                {
                    __Log_Msg(1, LogMsg.Priority.Debug, "Set_XoKey_Socks_Server: Set SOCKS proxy OK");
                }


                response.Close(); // cleanup;
            }
            catch (Exception ex)
            {
                __Log_Msg(1, LogMsg.Priority.Error, "Set_XoKey_Socks_Server: Exception setting SOCKS proxy " + ex.Message.ToString());
                throw;
            }
        }
      

        private bool Check_Available_Server_Port(int port)
        {
            __Log_Msg(0, LogMsg.Priority.Debug, "Checking Port: " + port);
            bool isAvailable = true;

            // Evaluate current system tcp connections. This is the same information provided
            // by the netstat command line application, just in .Net strongly-typed object
            // form.  We will look through the list, and if our port we would like to use
            // in our TcpClient is occupied, we will set isAvailable to false.
            System.Net.NetworkInformation.IPGlobalProperties ipGlobalProperties = System.Net.NetworkInformation.IPGlobalProperties.GetIPGlobalProperties();
            IPEndPoint[] tcpConnInfoArray = ipGlobalProperties.GetActiveTcpListeners();

            foreach (IPEndPoint endpoint in tcpConnInfoArray)
            {
                if (endpoint.Port == port)
                {
                    isAvailable = false;
                    break;
                }
            }

            __Log_Msg(0, LogMsg.Priority.Debug, "Port " + port + " available = " + isAvailable);
            return isAvailable;
        }
        private void Background_Init_Socks(object sender, DoWorkEventArgs e)
        {
            __Log_Msg(0, LogMsg.Priority.Debug, "Starting BackgroundWorker");
            Ping_For_Client_IP();
            if (XoKey_IP == IPAddress.Any)
            {
                __Log_Msg(1, LogMsg.Priority.Error, "IP address not detected");
            }
            else
            {
                
            }

            try
            {
                if (Socks_Listener == null)
                {
                    int port = Properties.Settings.Default.Socks_Port;
                    if (Check_Available_Server_Port(port))
                    {
                        //Socks_Listener = new Xoware.SocksServerLib.SocksListener(XoKey_IP, port);
                        Socks_Listener = new Xoware.SocksServerLib.SocksListener(port);
                        Socks_Listener.Start();
                        Set_XoKey_Socks_Server();
                    }
                    else
                    {
                        __Log_Msg(1, LogMsg.Priority.Error, "Port " + port + " NOT AVAILABLE");
                    }
                }
            }
            catch (Exception ex)
            {
                __Log_Msg(1, LogMsg.Priority.Error, "Unable to start SOCKS server on Port " + Properties.Settings.Default.Socks_Port + " Addr:" + XoKey_IP.ToString());
            }
               
            
        }
   
    }
}
