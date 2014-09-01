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
using System.Windows.Threading;
using System.Management;
using Microsoft.Win32;
using CefSharp.WinForms;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.ServiceProcess;
using System.Reflection;

using NATUPNPLib;
using NETCONLib;
using NetFwTypeLib;



namespace XoKeyHostApp
{
    public partial class ExoKeyHostAppForm : Form
    {
        

        XoKey xokey;
        private  WebView web_view;
        private bool USB_Dev_ID_Found = false;
        List<NetworkInterface> Interface_List;
        NetworkInterface Exokey_Interface = null;
        NetworkInterface Internet_Interface = null;
        Init_Dialog_Form Init_Dialog = null;
        bool First_EK_Page_Loaded = false;
        private BackgroundWorker startup_bw = new BackgroundWorker();
        private BackgroundWorker shutdown_bw = new BackgroundWorker();
        private volatile IPAddress XoKey_IP;
        private System.Windows.Forms.ContextMenu notify_contextMenu;
        private System.Windows.Forms.MenuItem notify_exit_menuItem;
        private String Log_File_Name = "";
        System.IO.StreamWriter Log_File_Stream = null;


        public ExoKeyHostAppForm(bool Enable_Debug, String Log_File)
        {
            InitializeComponent();

            try {
                Log_File_Name = Log_File;
                if (Log_File.Length > 2)
                    Log_File_Stream = new System.IO.StreamWriter(Log_File);
            }
            catch
            {
                Log_File_Stream = null;
            }

            // Subscribe to Event(s) with the WindowsInterop Class
            WindowsInterop.SecurityAlertDialogWillBeShown +=
                new GenericDelegate<Boolean, Boolean>(this.WindowsInterop_SecurityAlertDialogWillBeShown);

            WindowsInterop.ConnectToDialogWillBeShown +=
                new GenericDelegate<String, String, Boolean>(this.WindowsInterop_ConnectToDialogWillBeShown);
           // System.Drawing.Icon ico = Properties.Resources.
           // this.Icon = ico;

            ServicePointManager.ServerCertificateValidationCallback = new System.Net.Security.RemoteCertificateValidationCallback(AcceptAllCertifications);
            /*
            web_view = new WebView("", Get_Chrome_Settings());
            web_view.Dock = DockStyle.Fill;
            tabPage1.Controls.Add(web_view);
            web_view.ConsoleMessage += web_view_ConsoleMessage;
            web_view.LoadCompleted += web_view_LoadCompleted;
            web_view.LocationChanged += web_view_LocationChanged; 
             * */
            SystemEvents.PowerModeChanged += OnPowerChange;

            // Initialize menuItem1 
  
            notify_exit_menuItem = new System.Windows.Forms.MenuItem();
            notify_exit_menuItem.Index = 0;
            notify_exit_menuItem.Text = "E&xit";
            notify_exit_menuItem.Click += new System.EventHandler(this.notify_exit_Click);
            notify_contextMenu = new ContextMenu();
            notify_contextMenu.MenuItems.AddRange(
                    new System.Windows.Forms.MenuItem[] { this.notify_exit_menuItem });

            notifyIcon1.ContextMenu = notify_contextMenu;

            Properties.Settings.Default.Debug = Enable_Debug;
       //     shutdown_bw.RunWorkerCompleted += Shutdown_Worker_Complete;


        }
        private const string CLSID_FIREWALL_MANAGER = "{304CE942-6E39-40D8-943A-B913C40C9CD4}";
        private static NetFwTypeLib.INetFwMgr GetFirewallManager()
        {
            Type objectType = Type.GetTypeFromCLSID(
                  new Guid(CLSID_FIREWALL_MANAGER));
            return Activator.CreateInstance(objectType) 
                  as NetFwTypeLib.INetFwMgr;
        }
        /*
        private void Shutdown_Worker_Complete(object Sender, EventArgs e)
        {
            // Here you can safely manipulate the GUI controls
         //   this.Close();
        }
         * */
        private void notify_exit_Click(object Sender, EventArgs e)
        {
            // Close the form, which closes the application. 
            this.Close();
        }

        private DateTime RetrieveLinkerTimestamp()
        {
            string filePath = System.Reflection.Assembly.GetCallingAssembly().Location;
            const int c_PeHeaderOffset = 60;
            const int c_LinkerTimestampOffset = 8;
            byte[] b = new byte[2048];
            System.IO.Stream s = null;

            try
            {
                s = new System.IO.FileStream(filePath, System.IO.FileMode.Open, System.IO.FileAccess.Read);
                s.Read(b, 0, 2048);
            }
            finally
            {
                if (s != null)
                {
                    s.Close();
                }
            }

            int i = System.BitConverter.ToInt32(b, c_PeHeaderOffset);
            int secondsSince1970 = System.BitConverter.ToInt32(b, i + c_LinkerTimestampOffset);
            DateTime dt = new DateTime(1970, 1, 1, 0, 0, 0);
            dt = dt.AddSeconds(secondsSince1970);
            dt = dt.AddHours(TimeZone.CurrentTimeZone.GetUtcOffset(dt).Hours);
            return dt;
        }
        public bool IsProcessOpen(string name)
        {
            foreach (System.Diagnostics.Process clsProcess in System.Diagnostics.Process.GetProcesses())
            {
                if (clsProcess.ProcessName.Contains(name))
                {
                    return true;
                }
            }
            return false;
        }
        private void OnPowerChange(object s, PowerModeChangedEventArgs e)
        {
            __Log_Msg(0, LogMsg.Priority.Debug, "Power changed:" + e.Mode.ToString());
            switch (e.Mode)
            {
                case PowerModes.Resume:
                    break;
                case PowerModes.Suspend:
                    break;
            }

            if (xokey != null)
                xokey.On_Power_Change(e.Mode);
        }
        private void On_EK_State_Change(ExoKeyState Old_State, ExoKeyState New_State)
        {
            try
            {
                if (New_State == ExoKeyState.ExoKeyState_Connected)
                {
                    notifyIcon1.BalloonTipText = "ExoKey Connected to ExoNet";
                    notifyIcon1.ShowBalloonTip(1234);
                }
                else if (New_State == ExoKeyState.ExoKeyState_Disconnected)
                {
                    notifyIcon1.BalloonTipText = "ExoKey Disconnected from ExoNet";
                    notifyIcon1.ShowBalloonTip(1234);
                }
                else if (New_State == ExoKeyState.ExoKeyState_Unplugged)
                {
                    notifyIcon1.BalloonTipText = "ExoKey Unplugged or not detected";
                    notifyIcon1.ShowBalloonTip(1234);

                    // Invoke on main thread
                    this.BeginInvoke(new EventHandler(delegate
                    {
                        this.Close();
                    }));

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("State change Exception:" + ex.ToString());
            }

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
                Reload_UI_Login();
                return;
            }
            __Log_Msg(0, LogMsg.Priority.Debug, "LoadCompleted: " + url.Url);
            web_view.ExecuteScript("console.log('XOEvent:Cookie:' +document.cookie);");
            // disable right click
            web_view.ExecuteScript("window.oncontextmenu = function() { return false; };");

            if (First_EK_Page_Loaded == false)
            {
                First_EK_Page_Loaded = true;
                notifyIcon1.BalloonTipText = "ExoKey Started";
                notifyIcon1.ShowBalloonTip(1234);
            }
            
        }

        void Parse_Console_Log_Event(String Event_Msg)
        {
            const String Cookie_Marker = "XOEvent:Cookie:";

            if (Event_Msg.IndexOf(Cookie_Marker) == 0)
            {
                String Cookie_Val = Event_Msg.Substring(Cookie_Marker.Length);
                if (Cookie_Val.Length > 3)
                    xokey.Set_Session_Cookie(Cookie_Val);
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
            settings.WebSecurityDisabled = true;

            return settings;
        }



        public bool AcceptAllCertifications(object sender, System.Security.Cryptography.X509Certificates.X509Certificate certification, System.Security.Cryptography.X509Certificates.X509Chain chain, System.Net.Security.SslPolicyErrors sslPolicyErrors)
        {   // Accept all SSL certs
            Console.WriteLine("Accept SSL Cert");
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
        private void Reload_UI_Login()
        {
            if (XoKey_IP != null)
                Load_Url("https://" + XoKey_IP.ToString() + "/ui/login.html");
        }
        private void EK_Address_Change_Handler(IPAddress addr)
        {
            XoKey_IP = addr;
            __Log_Msg(0, LogMsg.Priority.Debug, "EK_Address_Change_Handler ip= "+ addr.ToString());
            Reload_UI_Login();
        }

        public static void DisableICS()
        {
            var currentShare = IcsManager.GetCurrentlySharedConnections();
            if (!currentShare.Exists)
            {
                Console.WriteLine("Internet Connection Sharing is already disabled");
                return;
            }
            Console.WriteLine("Internet Connection Sharing will be disabled:");
            Console.WriteLine(currentShare);
            IcsManager.ShareConnection(null, null);
        }
         void EnableICS(string shared, string home, bool force)
        {



            var connectionToShare = IcsManager.FindConnectionByIdOrName(shared);
            if (connectionToShare == null)
            {
                __Log_Msg(0, LogMsg.Priority.Error,  "Connection (Internet) not found:" + shared);
                return;
            }
            var homeConnection = IcsManager.FindConnectionByIdOrName(home);
            if (homeConnection == null)
            {
                __Log_Msg(0, LogMsg.Priority.Error,  "Connection (ExoKey) not found: {0}"+ home);
                return;
            }

            var currentShare = IcsManager.GetCurrentlySharedConnections();
            if (currentShare.Exists)
            {
                __Log_Msg(0, LogMsg.Priority.Info, "Internet Connection Sharing is already enabled:");
                Console.WriteLine(currentShare);
                if (!force)
                {
                    __Log_Msg(0, LogMsg.Priority.Info, "Please disable it if you want to configure sharing for other connections");
                    return;
                }
                __Log_Msg(0, LogMsg.Priority.Info, "Sharing will be disabled first.");
            }
            try
            {
                IcsManager.ShareConnection(connectionToShare, homeConnection);
            }
            catch
            {
                __Log_Msg(0, LogMsg.Priority.Critical,
                    "Internet Connection Sharing to ExoKey Failed.  Internet: " + shared + " ExoKey:" + home);
                try
                {
                    DisableICS();
                } catch
                {

                }

            }

        }
        private void Debug_Services()
        { 
            ServiceController[] allService = ServiceController.GetServices();
            foreach (ServiceController serviceController in allService)
            {
                __Log_Msg(0, LogMsg.Priority.Debug, "Service: " + serviceController.ServiceName 
                    +  " Status: " + serviceController.Status.ToString());
            }
        }
        private bool Check_ICS_Dependencies_Single()
        {
   
            string[] deps = { 
                "TapiSrv", // Telephony
                "ALG", // Application Layer Gateway
                "Netman", // Network connections
                "NlaSvc", // Network location awarenes
                "PlugPlay",
                "RasAuto",  // Remote Access Auto Connection Manage
                "RasMan",  // Remote Access Connection Manager
                "RpcSs"  // Remote Procedure Call (RPC)
             };
            bool success = true;

            foreach (String dep in deps) {
                try{
                     WinServices.StartService(dep, "Manual");
                }
                catch (Exception ex)
                {
                    success = false;
                    __Log_Msg(0, LogMsg.Priority.Warning, "Error Launching: " + dep + " Exception " + ex.ToString());
                    throw ex;
                   
                }

            }
            return success;
            
        }

        private void Check_ICS_Dependencies()
        {
            for (int retry = 5; retry > 0; retry--)
            {
                try
                {
                    Check_ICS_Dependencies_Single();
                    break;
                }
                catch (Exception ex)
                {
                    if (retry <= 1)
                    {
                        Debug_Services();
                        __Log_Msg(0, LogMsg.Priority.Warning, "Error  ICS Dependencies Exception: " + ex.ToString());
                    }
                }
            }
        }
        private void Load_Internet_Interfaces()
        {
            try
            {
                DisableICS();
            }
            catch
            {

            }

            try
            {
                Init_Dialog.Recv_Status_Text("Checking Internet Route");
                Init_Dialog.Recv_Progress_Val(20);

                // Create a UDP client, so we can figure out what interface has a route to the internet. 
                UdpClient u = new UdpClient("8.8.8.8", 53);
                IPAddress localAddr = ((IPEndPoint)u.Client.LocalEndPoint).Address; // This bound address is internet facing


                NetworkInterface Def_Intf = NetworkInterface.GetAllNetworkInterfaces().FirstOrDefault();
                Interface_List = new List<NetworkInterface>();


                Exokey_Interface = null; // init
                Internet_Interface = null;
                int i = 0;
                foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
                {

                    //  var connection = IcsManager.GetConnectionById(nic.Id);
                    //    var properties = IcsManager.GetProperties(connection);
                    //  var configuration = IcsManager.GetConfiguration(connection);
                    /*
                    var record = new
                                     {
                                         Name = nic.Name,
                                         GUID = nic.Id,
                                         MAC = nic.GetPhysicalAddress(),
                                         Description = nic.Description,
                                         SharingEnabled = configuration.SharingEnabled,
                                         NetworkAdapter = nic,
                                         Configuration = configuration, 
                                         Properties = properties,
                                     };
                     */
                    //               if (nic.OperationalStatus == OperationalStatus.Down)
                    //                   continue;
                    if (!nic.Supports(NetworkInterfaceComponent.IPv4))
                        continue;
                    if (!(nic.NetworkInterfaceType == NetworkInterfaceType.Ethernet
                            || nic.NetworkInterfaceType == NetworkInterfaceType.Wireless80211
                            || nic.NetworkInterfaceType == NetworkInterfaceType.GigabitEthernet))
                        continue;


                    IPInterfaceProperties ipProps = nic.GetIPProperties();
                    UnicastIPAddressInformationCollection uniCast = ipProps.UnicastAddresses;

                    if (uniCast == null)
                    {
                        // no address
                        continue;
                    }

                    //       Intf_comboBox.Items.Add(nic.Name + " " + nic.Description);
                    Interface_List.Add(nic);
                    __Log_Msg(0, LogMsg.Priority.Debug, "interface: " + nic.Name + "  Desc: " + nic.Description + " Status:" + nic.OperationalStatus.ToString());
                    if (nic.Description.Contains("XoWare") || nic.Description.Contains("x.o.ware"))
                    {
                        Exokey_Interface = nic;
                        __Log_Msg(0, LogMsg.Priority.Debug, "Exokey interface: " + nic.Description);
                    }
                    foreach (UnicastIPAddressInformation uni in uniCast)
                    {
                        if (uni.Address.Equals(localAddr))
                        {
                            //        Intf_comboBox.SelectedIndex = i;
                            Internet_Interface = nic;
                            __Log_Msg(0, LogMsg.Priority.Debug, "Internet interface: " + nic.Description);
                        }
                    }

                    i++;

                    Init_Dialog.Recv_Status_Text("Checking Interface " + (i + 1));
                    Init_Dialog.Recv_Progress_Val(20 + i);
                }
                Init_Dialog.Recv_Status_Text("Checking services");

                Check_ICS_Dependencies();
                Init_Dialog.Recv_Progress_Val(70);

                Init_Dialog.Recv_Status_Text("Enabling Internet Connection Sharing");
                Init_Dialog.Recv_Progress_Val(85);

                //    Intf_comboBox.Update();

                if (Exokey_Interface == null)
                {
                    Init_Dialog.Recv_Status_Text("Exokey not found");
                    __Log_Msg(0, LogMsg.Priority.Critical, "Exokey interface not found.  "
                        + " If this is the 1st time, please wait and ensure Windows has completed the driver install. "
                        + " If the problem persists after retrying, look for the ExoKey Device in the device manager.");
                    return;
                }
                else if (Internet_Interface == null)
                {
                    __Log_Msg(0, LogMsg.Priority.Critical, "Internet interface not found");
                    return;
                }
                else if (Internet_Interface.Equals(Exokey_Interface))
                {
                    __Log_Msg(0, LogMsg.Priority.Critical, "Exokey Is internet interface.  Invalid configuration.");
                    return;
                }
            }
            catch (Exception ex)
            {
                Debug_Services();
                __Log_Msg(0, LogMsg.Priority.Critical, "Exception " + ex.ToString());
            }


            try
            {
                EnableICS(Internet_Interface.Id, Exokey_Interface.Id, true);
                Init_Dialog.Recv_Status_Text("ICS complete");
                Init_Dialog.Recv_Progress_Val(90);

               // Internet_Interface
              //  IcsManager.ShareConnection(Internet_Interface as NETCONLib.INetConnection, Exokey_Interface as NETCONLib.INetConnection);
            }
            catch (Exception ex)
            {
                Debug_Services();
                __Log_Msg(0, LogMsg.Priority.Critical, "Exception "+ ex.ToString());
            }
        }


        private void startup_DoWork(object sender, DoWorkEventArgs e)
        {
   
            BackgroundWorker worker = sender as BackgroundWorker;
            __Log_Msg(0, LogMsg.Priority.Debug, "Build Date: " + RetrieveLinkerTimestamp().ToString());
            __Log_Msg(0, LogMsg.Priority.Debug, "Version: " + Assembly.GetExecutingAssembly().GetName().Version.ToString());

            __Log_Msg(0, LogMsg.Priority.Debug, "Windows Version:" + System.Environment.OSVersion.ToString());

            Load_Internet_Interfaces();

            Init_Dialog.Recv_Status_Text("Finishing Initalization.");
            Init_Dialog.Recv_Progress_Val(93);
            xokey = new XoKey(invoke => Invoke(invoke), Recv_Log_Msg);
            xokey.EK_IP_Address_Detected += EK_Address_Change_Handler; // address change event
            xokey.EK_State_Change += On_EK_State_Change;  // connection state change event
            xokey.Startup();
            Init_Dialog.Invoke_Close();
        }

        private void ExoKeyHostAppForm_Load(object sender, EventArgs e)
        {
            
            __Log_Msg(0, LogMsg.Priority.Debug, "Startup " + System.AppDomain.CurrentDomain.FriendlyName);
            __Log_Msg(0, LogMsg.Priority.Debug, "OS Name: " + OSInfo.Name);
            __Log_Msg(0, LogMsg.Priority.Debug, "OS Edition: " + OSInfo.Edition);
            __Log_Msg(0, LogMsg.Priority.Debug, "OS ServicePack: " + OSInfo.ServicePack);
            __Log_Msg(0, LogMsg.Priority.Debug, "OS Bits: " + OSInfo.Bits);

            if (Log_File_Stream != null)
            {
                __Log_Msg(0, LogMsg.Priority.Debug, "Logging To: " + Log_File_Name);
            }

            try
            {
                INetFwMgr manager = GetFirewallManager();
                Boolean FW_Enabled = manager.LocalPolicy.CurrentProfile.FirewallEnabled;

                __Log_Msg(0, LogMsg.Priority.Debug, "FW Enabled: " + FW_Enabled.ToString());

                if (!FW_Enabled)
                {
                    MessageBox.Show("Windows Firewall must be enabled for this application to work");
                    this.Close();
                    return;
                }

                // Get Reference to the current Process
                System.Diagnostics.Process thisProc = System.Diagnostics.Process.GetCurrentProcess();
                if (IsProcessOpen(System.AppDomain.CurrentDomain.FriendlyName) == false)
                {
                    //System.Windows.MessageBox.Show("Application not open!");
                    //System.Windows.Application.Current.Shutdown();
                }
                else
                {
                    // Check how many total processes have the same name as the current one
                    if (System.Diagnostics.Process.GetProcessesByName(thisProc.ProcessName).Length > 1)
                    {
                        // If ther is more than one, than it is already running.
                        MessageBox.Show("Application is already running.");
                        this.Close();
                        return;
                    }
                }
                int Major_Version = System.Environment.OSVersion.Version.Major;

                // Windows 2008 server and Windows 7 are major version 6
                if ( Major_Version < 6) 
                {
                    MessageBox.Show("Windows 7 or newer required");
                    this.Close();
                    return;
                }
                else if (Major_Version == 6)
                {
                    if (OSInfo.Edition.Contains("Starter"))
                    {
                        __Log_Msg(0, LogMsg.Priority.Error, "Windows Starter version not supported, use at your own risk");
                    }
                }


                // check_registry();
                Set_Debug(Properties.Settings.Default.Debug);

                web_view = new WebView("", Get_Chrome_Settings());
                web_view.Dock = DockStyle.Fill;
                tabPage1.Controls.Add(web_view);
                web_view.ConsoleMessage += web_view_ConsoleMessage;
                web_view.LoadCompleted += web_view_LoadCompleted;
                web_view.LocationChanged += web_view_LocationChanged; 

                Init_Dialog = new Init_Dialog_Form();
                Init_Dialog.Show();
                Init_Dialog.Set_Status_Text("Searching for USB device");
                Init_Dialog.Set_Progress_Bar(10);

                if (Search_For_ExoKey())
                {
                    Init_Dialog.Set_Status_Text("USB Device Found.  Detecting Network Intfaces.");
                    Init_Dialog.Set_Progress_Bar(20);
                    startup_bw.DoWork += new DoWorkEventHandler(startup_DoWork);
                    startup_bw.RunWorkerAsync();

                    Load_Url("file://"+ System.IO.Path.GetDirectoryName(Application.ExecutablePath)
                        +"/waiting_for_hw.html");
                }
            } catch (Exception ex)
            {
                __Log_Msg(0, LogMsg.Priority.Error, "Initalizing Loading" + ex.ToString());
            }
 
        }
        private void __Log_Msg(int code, LogMsg.Priority level, String message)
        {
            LogMsg Msg = new LogMsg(message, level, code);
            Recv_Log_Msg(Msg);
        }
        // Asyncronously pass the message to the main thread
        public void Recv_Log_Msg(LogMsg Msg)
        {
            try
            {

                Log_Msg_Handler callback = new Log_Msg_Handler(Add_Log_Entry);
                this.Invoke(callback, new object[] { Msg });
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
            Console.WriteLine("Accept WindowsInterop_SecurityAlertDialogWillBeShown");
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
            Go_button.Visible = false;
            Location_textBox.Visible = value;
            devToolsToolStripMenuItem.Visible = value;
            saveToFileToolStripMenuItem.Visible = value;
            exportLogToolStripMenuItem.Visible = value;

            if (value)
            {
                if (!tabControl1.Contains(Log_tabPage)) 
                    tabControl1.TabPages.Add(Log_tabPage);
            }
            else
            {
                if (tabControl1.Contains(Log_tabPage))
                    tabControl1.TabPages.Remove(Log_tabPage);
            }
 
//            Log_tabPage.Enabled = value;
//            Log_tabPage.Visible = value;
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

            try
            {

                if (Log_File_Stream != null)
                {
                    String Out_Line;
                    Out_Line = Msg.Time + "\t" + Msg.Code + "\t" + Msg.Level + "\t" + Msg.Message;
                    Log_File_Stream.WriteLine(Out_Line);
                    Log_File_Stream.Flush();
                }
            
                if (PopupErrors_checkBox.Checked && Msg.Level <= LogMsg.Priority.Critical)
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
            catch (Exception e)
            {
                Console.WriteLine("Ex:" + e.Message + "   " + Msg.Message);
            }

        }
        private bool Search_For_ExoKey()
        {
            Search_USB_Devices();
            while (!USB_Dev_ID_Found)
            {
             //   break; // FIXME remove this

                DialogResult result = MessageBox.Show("Please Insert ExoKey, wait for startup and click Retry, or abort to exit",
                    "ExoKey not found on USB port",
                    MessageBoxButtons.AbortRetryIgnore);

                if (result == System.Windows.Forms.DialogResult.Cancel || result == System.Windows.Forms.DialogResult.Abort)
                {
                    this.Close();
                    return false;
                }
                if (result == System.Windows.Forms.DialogResult.Ignore)
                {
                    break;             
                }
                 Search_USB_Devices();
            }
            return true;
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
                    Console.WriteLine("Name=" + property.Name + " Value = " +
                        (property.Value == null ? "null" :property.Value.ToString()));

                    if (property.Value != null)
                    {
                        if (property.Value.ToString().Contains("VID_0525&PID_A4A2")) {
                            System.Diagnostics.Debug.WriteLine("Found: VID_0525&PID_A4A2");
                            USB_Dev_ID_Found = true;  // LINUX USB Ether
                        }
                        if (property.Value.ToString().Contains("VID_29B7&PID_0101"))
                        {
                            System.Diagnostics.Debug.WriteLine("Found: VID_29B7&PID_0101");
                            USB_Dev_ID_Found = true;  // Xoware
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
        private void Navigate( String address = null)
        {
           
            if (address == null)
                address = Location_textBox.Text;

            if (String.IsNullOrEmpty(address)) return;
            if (address.Equals("about:blank")) return;
            if (!address.StartsWith("http://") &&
                !address.StartsWith("https://") && !address.StartsWith("file://"))
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
        /*
        private void webBrowser1_ProgressChanged(object sender, WebBrowserProgressChangedEventArgs e)
        {
            System.Text.StringBuilder messageBoxCS = new System.Text.StringBuilder();
   
            messageBoxCS.AppendFormat("{0} = {1} ", "CurrentProgress", e.CurrentProgress);
            messageBoxCS.AppendLine();
            messageBoxCS.AppendFormat(" {0} = {1}", "MaximumProgress", e.MaximumProgress);
            messageBoxCS.AppendLine();
            __Log_Msg(0, LogMsg.Priority.Debug, "ProgressChanged Event"+ messageBoxCS.ToString());
  
            if (e.MaximumProgress > 0)
                toolStripProgressBar1.Value = (int)Math.Round((double)(e.CurrentProgress / e.MaximumProgress) * 100);
            else
                toolStripProgressBar1.Value = 100;
        }
    */
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
            Console.WriteLine("Closing");
            try
            {
                Init_Dialog = new Init_Dialog_Form("Closing");
                Init_Dialog.Show();
                Init_Dialog.Set_Status_Text("Disabeling ExoKey Internet ");
                Init_Dialog.Set_Progress_Bar(5);
            } catch (Exception ex)
            {
                Console.WriteLine("ex" + ex.ToString());
            }

            try
            {
                Init_Dialog.Set_Status_Text("Stopping ExoKey");
                Init_Dialog.Set_Progress_Bar(10);

                if (xokey != null)
                {
                    System.Diagnostics.Debug.WriteLine("stop xokey ");
                    xokey.Stop();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Stop ex: " + ex.ToString());
            }

            try
            {
                Init_Dialog.Set_Status_Text("Disable Windows ICS with ExoKey");
                Init_Dialog.Set_Progress_Bar(20);
                Console.WriteLine("DisableICS");
                System.Diagnostics.Debug.WriteLine("DisableICS2 "); 
                DisableICS();

                Init_Dialog.Set_Status_Text("Windows ICS Disabled");
                Init_Dialog.Set_Progress_Bar(30);

                Console.WriteLine("DisableICS2 "); 
            }
            catch (Exception ex)
            {
                Console.WriteLine("Disable ICS ex: " + ex.ToString());
            }

            try
            {
                //       web_view.Dispose();
                if (xokey != null)
                {

                    Init_Dialog.Set_Status_Text("Cleaningup Configuration");
                    Init_Dialog.Set_Progress_Bar(85);
                    System.Diagnostics.Debug.WriteLine("dispose xokey ");
                    xokey.Dispose();
                    xokey = null;
                    Console.WriteLine("xokey disposed ");
                }

                Init_Dialog.Set_Status_Text("Closing");
                Init_Dialog.Set_Progress_Bar(90);
                System.Diagnostics.Debug.WriteLine("Close Dev");
                web_view.CloseDevTools();

                Init_Dialog.Set_Progress_Bar(92);
                Console.WriteLine("Close WebView");
                Console.WriteLine("stop web_view");
                web_view.Stop();
                Console.WriteLine("dispose web_view");
                web_view.Dispose();
                web_view = null;
                Console.WriteLine("dispose this object");
                //  this.Dispose();
                Init_Dialog.Invoke_Close();
                Console.WriteLine("Form closing dispose done.");

            }
            catch (Exception ex) {
                Console.WriteLine("Exception closing: " + ex.Message);
                Console.WriteLine("Exception closing: " + ex.StackTrace.ToString());
            }

            System.Threading.Thread.Sleep(123);
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void devToolsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            web_view.ShowDevTools();

        }

        private void Location_textBox_Enter(object sender, EventArgs e)
        {
            Navigate();
        }

        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            // Set the WindowState to normal if the form is minimized. 
            if (this.WindowState == FormWindowState.Minimized)
                this.WindowState = FormWindowState.Normal;

            // Activate the form. 
            this.Activate();
        }
   
    }
}
