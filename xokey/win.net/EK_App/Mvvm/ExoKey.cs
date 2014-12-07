using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using System.Net;
using System.Runtime.InteropServices;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Management;
using System.Windows;

namespace EK_App.Mvvm
{

    public enum ExoKeyState : int
    {
        ExoKeyState_Init = 0,
        ExoKeyState_Disconnected,
        ExoKeyState_USBDeviceDetected,
        ExoKeyState_Connected,
        ExoKeyState_Unplugged,
    }

    public enum ExoKeyLoginState : int
    {
        ExoKeyLoginState_Init = 0,
        ExoKeyLoginState_LoadingUi,
        ExoKeyLoginState_Loggedout,
        ExoKeyLoginState_Loggedin,
    }

    public delegate void EK_IP_Address_Detected_Handler(IPAddress ip);
    public delegate void EK_State_Change_Handler(ExoKeyState Old_State, ExoKeyState New_State);

    public class UdpState
    {
        public UdpClient udp_client;
        public IPEndPoint end_point;
    }

    public enum FacilityEnum : int
    {
        Kernel = 0,
        User = 1,
        Mail = 2,
        System = 3,
        Security = 4,
        Internally = 5,
        Printer = 6,
        News = 7,
        UUCP = 8,
        cron = 9,
        Security2 = 10,
        Ftp = 11,
        Ntp = 12,
        Audit = 13,
        Alert = 14,
        Clock2 = 15,
        local0 = 16,
        local1 = 17,
        local2 = 18,
        local3 = 19,
        local4 = 20,
        local5 = 21,
        local6 = 22,
        local7 = 23,
    }

    public enum SeverityEnum : int
    {
        Emergency = 0,
        Alert = 1,
        Critical = 2,
        Error = 3,
        Warning = 4,
        Notice = 5,
        Info = 6,
        Debug = 7,
    }

    public struct PriStruct
    {

        public FacilityEnum Facility;

        public SeverityEnum Severity;

        public PriStruct(string strPri)
        {

            int intPri = Convert.ToInt32(strPri);

            int intFacility = intPri >> 3;

            int intSeverity = intPri & 0x7;

            this.Facility = (FacilityEnum)Enum.Parse(typeof(FacilityEnum), intFacility.ToString());
            this.Severity = (SeverityEnum)Enum.Parse(typeof(SeverityEnum),
               intSeverity.ToString());
        }
        public override string ToString()
        {
            //EXPORT VALUES TO A VALID PRI STRUCTURE
            return string.Format("{0}.{1}", this.Facility, this.Severity);
        }
    }


    public class ExoKey : IDisposable
    {
        const int MCast_Port = 1500;
        const int SysLog_MCast_Port = 514;
        IPAddress ExoKey_Multicast_Address = IPAddress.Parse("239.255.255.255");
        IPAddress ExoKey_SysLogMcast_Address = IPAddress.Parse("239.255.255.254");
        private String Last_SysLog_Msg = "";  // to avoid repeats
        public event Log_Msg_Handler Log_Msg_Send_Event = null;
        public event EK_IP_Address_Detected_Handler EK_IP_Address_Detected = null;
        public event EK_State_Change_Handler EK_State_Change = null;
        private readonly Object event_locker = new Object();
        private string Session_Cookie = "";
        private volatile IPAddress XoKey_IP = null; //IPAddress.Parse("192.168.255.1");
        private volatile IPAddress Client_USB_IP = null;
        private volatile ExoKeyState EK_State = ExoKeyState.ExoKeyState_Disconnected;
        ExoKeyLoginState Login_State = ExoKeyLoginState.ExoKeyLoginState_Init;
  //      System.Timers.Timer Check_State_Timer;
        IPEndPoint Server_IPEndPoint = null;
        Boolean Traffic_Routed_To_XoKey = false;
        Boolean Firewall_Opened = false;
        private volatile Boolean Disposing = false;
        //  UdpClient Mcast_UDP_Client = null;
        private BackgroundWorker startup_bw = new BackgroundWorker();

        private Xoware.RoutingLib.RoutingTableRow default_route = null;
        private List<IPAddress> MCast_Listening;
        public int No_EK_Status_Error_Count = 0;
        public int EK_Intf_Down_Count = 0;
        private int Ping_Error_Count = 0;
 //       bool Check_State_Timer_Running = false;
        NetworkInterface Exokey_Interface = null;
        NetworkInterface Internet_Interface = null;
        bool Keep_Running = true;
        bool Dependency_Services_Running = false;
        bool USB_Dev_ID_Found = false;
        bool ExoKey_Driver_Found = false;
        bool ICS_Configured = false;
        bool Network_Interfaces_OK = false;
        bool Has_Internet_Access = false;
        DateTime Last_Vpn_Status = DateTime.Now;
        System.Threading.Thread State_Machine_Thread = null;
        public EK_App.ViewModels.BrowserTabViewModel Browser = null;

        public ExoKey(Log_Msg_Handler Log_Event_Handler = null, EK_App.ViewModels.BrowserTabViewModel wb = null)
        {
            if (Log_Event_Handler != null)
                Log_Msg_Send_Event += Log_Event_Handler;

            if (wb != null)
            {
                Browser = wb;
           
            }

            Send_Log_Msg("XoKey Startup");
            MCast_Listening = new List<IPAddress>();

            NetworkChange.NetworkAddressChanged += new
              NetworkAddressChangedEventHandler(AddressChangedCallback);

            State_Machine_Thread = new System.Threading.Thread(State_Machine_Thread_Main);
            State_Machine_Thread.Start();


            /*
            App.Current.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(
           () =>
           {
               var notify = new NotificationWindow();
               notify.Show("Starting");
           }));*/
        }
        private void Check_Internet_Access()
        {
            string[] Hosts = new string[] { "8.8.8.8",
                "208.67.222.222",
                "8.8.4.4",
                "www.xoware.com",
                "ns2.vpex.org"};

            for (int i = 0; i < Hosts.Count(); i++)
            {

                try
                {

                    Ping pingSender = new Ping();
                    PingOptions options = new PingOptions();

                    // Use the default Ttl value which is 128, 
                    // but change the fragmentation behavior.
                    options.DontFragment = true;

                    // Create a buffer of of data to be transmitted. 
                    string data = "Hello";
                    byte[] buffer = Encoding.ASCII.GetBytes(data);
                    int timeout = 3210;
                    PingReply reply = pingSender.Send(Hosts[i], timeout, buffer, options);
                    if (reply.Status == IPStatus.Success)
                    {
                        Has_Internet_Access = true;
                        Console.WriteLine("Address: {0}", reply.Address.ToString());
                        Console.WriteLine("RoundTrip time: {0}", reply.RoundtripTime);
                        Console.WriteLine("Time to live: {0}", reply.Options.Ttl);
                        Console.WriteLine("Don't fragment: {0}", reply.Options.DontFragment);
                        Console.WriteLine("Buffer size: {0}", reply.Buffer.Length);
                        Ping_Error_Count = 0;
                        return;

                    }
                    else
                    {
                        Has_Internet_Access = false;
                    }

                }
                catch (Exception ex)
                {


                    Has_Internet_Access = false;
                    Console.WriteLine("Ping Ex: {0}    Error_Count {1}", ex.Message, Ping_Error_Count);
                    Ping_Error_Count++;
                    if (Ping_Error_Count > 40) 
                        InvokeExecuteJavaScript("if (document.location.href.indexOf('custom://') < 0) document.location.href='custom://cefsharp/home';");
                }
            }
        }

        // parse something like // "\\KARL-PC\root\cimv2:Win32_PnPEntity.DeviceID="USB\\VID_29B7&PID_0101\\123"
        private void Check_Dev_Desc(String id_str)
        {
            string Device_ID_Str = "DeviceID=";
            int pos = id_str.LastIndexOf(Device_ID_Str);

            if (pos < 0)
                return;

            pos += Device_ID_Str.Length;
            String ID_Value = id_str.Substring(pos);
            ID_Value = ID_Value.Replace("\"", "");
            
            ManagementObjectCollection collection;
            using (var searcher = new ManagementObjectSearcher(@"Select * From Win32_PnPSignedDriver Where DeviceID = '" + ID_Value + "'")) // Win32_USBDevice Win32_USBHub
                collection = searcher.Get();

            foreach (var entity in collection)
            {
                Console.WriteLine("entity=" + entity.ToString());
            }
            try
            {
                string ComputerName = "localhost";
                ManagementScope Scope;
                Scope = new ManagementScope(String.Format("\\\\{0}\\root\\CIMV2", ComputerName), null);

                Scope.Connect();
                ObjectQuery Query = new ObjectQuery("SELECT * FROM Win32_PnPSignedDriver Where DeviceID = '" + ID_Value + "'");
                ManagementObjectSearcher Searcher = new ManagementObjectSearcher(Scope, Query);

                foreach (ManagementObject WmiObject in Searcher.Get())
                {
                    Console.WriteLine("{0,-35} {1,-40}", "ClassGuid", WmiObject["ClassGuid"]);// String
                    Console.WriteLine("{0,-35} {1,-40}", "DeviceClass", WmiObject["DeviceClass"]);// String
                    Console.WriteLine("{0,-35} {1,-40}", "DeviceID", WmiObject["DeviceID"]);// String
                    Console.WriteLine("{0,-35} {1,-40}", "DeviceName", WmiObject["DeviceName"]);// String
                    Console.WriteLine("{0,-35} {1,-40}", "Manufacturer", WmiObject["Manufacturer"]);// String
                    Console.WriteLine("{0,-35} {1,-40}", "Name", WmiObject["Name"]);// String
                    Console.WriteLine("{0,-35} {1,-40}", "Status", WmiObject["Status"]);// String
                    Console.WriteLine("{0,-35} {1,-40}", "DriverName", WmiObject["DriverName"]);// String
                    Console.WriteLine("{0,-35} {1,-40}", "DriverVersion", WmiObject["DriverVersion"]);// String
                    Console.WriteLine("{0,-35} {1,-40}", "FriendlyName", WmiObject["FriendlyName"]);// String
                    Console.WriteLine("{0,-35} {1,-40}", "Started", WmiObject["Started"]);// String
                    if (WmiObject["DeviceName"] != null && WmiObject["DriverVersion"] != null
                       && WmiObject["DeviceName"].ToString().Length > 4 && WmiObject["DriverVersion"].ToString().Length > 3)
                       ExoKey_Driver_Found = true;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(String.Format("Exception {0} Trace {1}", e.Message, e.StackTrace));
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
                    Console.WriteLine("Name=" + property.Name + " Value = " +
                        (property.Value == null ? "null" : property.Value.ToString()));

                    if (property.Value != null)
                    {
                        if (property.Value.ToString().Contains("VID_0525&PID_A4A2"))
                        {
                            System.Diagnostics.Debug.WriteLine("Found: VID_0525&PID_A4A2");
                            USB_Dev_ID_Found = true;  // LINUX USB Ether
                        }
                        if (property.Value.ToString().Contains("VID_29B7&PID_0101"))
                        {
                            System.Diagnostics.Debug.WriteLine("Found: VID_29B7&PID_0101");
                            USB_Dev_ID_Found = true;  // Xoware


                            PropertyDataCollection system_properties = device.SystemProperties;
                            foreach (PropertyData sys_prop in system_properties)
                            {
                                Console.WriteLine("sys_prop=" + sys_prop.Name + " Value = " +
                                 (property.Value == null ? "null" : sys_prop.Value.ToString()));
                            }

                            QualifierDataCollection qualifiers = device.Qualifiers;
                            foreach (QualifierData qdata in qualifiers)
                            {
                                Console.WriteLine("qdata=" + qdata.Name + " Value = " +
                                (property.Value == null ? "null" : qdata.Value.ToString()));
                            }
                            Check_Dev_Desc(property.Value.ToString());
                        }
                    }

                }

            }
            collection.Dispose();


            //   return devices;
        }
        private void Url_Changed(String url)
        {
            if (url.Contains("/ek/login"))
            {
                Login_State = ExoKeyLoginState.ExoKeyLoginState_Loggedout;
                SetStatusMsg("Please login.");
            }
            else if (url.Contains("/ek/vpex"))
            {
                Login_State = ExoKeyLoginState.ExoKeyLoginState_Loggedin;
                SetStatusMsg(" ");
            }
            else if (url.Contains("custom://cefsharp"))
            {
                Login_State = ExoKeyLoginState.ExoKeyLoginState_Init;
            }
            else if (Login_State == ExoKeyLoginState.ExoKeyLoginState_Init && ICS_Configured)
            {
                Send_Log_Msg(0, LogMsg.Priority.Debug, "Retry load UI");
                InvokeExecuteJavaScript("setInterval(function(){ "
                               + " document.location.href='https://192.168.137.2/'; }, 2000);");
            }
        }
        private void Process_Browswer_Console_Msg(String msg)
        {
            try
            {
                // if state change to connected
                if (msg.Contains("VPNStatus=Connected=") && EK_State != ExoKeyState.ExoKeyState_Connected)
                {
                    String IP_Addr_Str = msg.Substring(msg.LastIndexOf("=") + 1);
                    //             IPHostEntry hostEntry = Dns.GetHostEntry(IP_Addr_Str); // fixme check if host address

                    IPEndPoint Server = new IPEndPoint(IPAddress.Parse(IP_Addr_Str), 0);
                    Set_Sever_IPEndpoint(Server);
                    Set_EK_State(ExoKeyState.ExoKeyState_Connected);
                    SetStatusMsg("Connected to ExoNet at " + IP_Addr_Str);

                    Set_EK_State(ExoKeyState.ExoKeyState_Connected);
                }

                if (msg.Contains("VPNStatus=Stopped"))
                {
                    if (Traffic_Routed_To_XoKey)
                        Remove_Routes();

                    Set_EK_State(ExoKeyState.ExoKeyState_Disconnected);
                    SetStatusMsg("Not connected to ExoNet");
                }
                Last_Vpn_Status = DateTime.Now;
            }
            catch (Exception ex)
            {
                Send_Log_Msg(1, LogMsg.Priority.Debug, "Process_Browswer_Console_Msg ex " + ex.Message);
            }
        }
        private void Load_Error(String url, String Error_Text, CefSharp.CefErrorCode code)
        {
            Send_Log_Msg(1, LogMsg.Priority.Debug, "Failed to load  " + url 
                + "  Msg=" + Error_Text + "  Code=" + code);
            Restart_Detection();
        }
        private void InvokeExecuteJavaScript(String s)
        {
            if (Browser != null && Keep_Running)
                Browser.InvokeExecuteJavaScript(s);
        }
        private void SetStatusMsg(String s)
        {
            if (Browser != null && Keep_Running)
                Browser.OutputMessage = s;
        }

        private void State_Machine_Thread_Main()
        {
            Open_Firewall();

            while (Keep_Running && Browser == null)
            {
                System.Threading.Thread.Sleep(50);
            }

            for (int i = 0; Keep_Running &&  i < 5; i++)
            {
                System.Threading.Thread.Sleep(100);
            }


            if (!Keep_Running)
                 return;

            Browser.Url_Changed_Event += Url_Changed;
            Browser.Load_Error_Event += Load_Error;
            Browser.Console_Message_Event += Process_Browswer_Console_Msg;
            SetStatusMsg("Starting...");

            while(Keep_Running)
            {
                if (!USB_Dev_ID_Found || !ExoKey_Driver_Found)
                {
//                    InvokeExecuteJavaScript("$('#status_usb_hw_detected').attr('class', 'label label-primary');"
//                        + "$('#status_usb_hw_detected').text('Checking');");
                    Search_USB_Devices();
                }

                if (!USB_Dev_ID_Found)
                {
                    InvokeExecuteJavaScript("$('#status_usb_hw_detected').attr('class', 'label label-warning');"
                        + "$('#status_usb_hw_detected').text('Not Found');");

                    InvokeExecuteJavaScript("$('#status_usb_hw_msg').html('"
                       + "<div class=\\'alert alert-danger\\' role=\\'alert\\'>"
                       + "ExoKey USB Device not found.  "
                       + " Please try to reinsert the ExoKey or plugging in a different USB port. "
                       + " After you plug the ExoKey in it should show up here in 10 to 15 seconds. "
                       + "If the problem persists after retrying, try a different USB cable."
                       + "</div>');");
                }

                if (!ExoKey_Driver_Found) {
                    InvokeExecuteJavaScript("$('#status_driver').attr('class', 'label label-warning');"
                        + "$('#status_driver').text('Not Found');");
                    InvokeExecuteJavaScript("$('#status_driver_msg').html('"
                       + "<div class=\\'alert alert-danger\\' role=\\'alert\\'>"
                       + "ExoKey network driver not found.  "
                       + " Please try rebooting your PC, or reinstalling the driver or plugging in a different USB port. "
                       + " If the problem persists after retrying, look for the ExoKey Device in the device manager as a Network Adapter."
                       + "</div>');");
                }



                if (Login_State == ExoKeyLoginState.ExoKeyLoginState_Init && USB_Dev_ID_Found)
                {
                    InvokeExecuteJavaScript("$('#status_usb_hw_detected').attr('class', 'label label-success');"
                        + "$('#status_usb_hw_detected').text('OK');");

                    InvokeExecuteJavaScript("$('#status_usb_hw_msg').html('');");
                }

                if (Login_State == ExoKeyLoginState.ExoKeyLoginState_Init && ExoKey_Driver_Found)
                {
                    InvokeExecuteJavaScript("$('#status_driver').attr('class', 'label label-success');"
                        + "$('#status_driver').text('OK');");
                    InvokeExecuteJavaScript("$('#status_driver_msg').html('');");
                }

                if (!USB_Dev_ID_Found || !ExoKey_Driver_Found)
                {
                    goto next_loop;
                }

                Check_Internet_Access();

                if (!Has_Internet_Access)
                {
                    if (System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
                    {
                        if (Login_State == ExoKeyLoginState.ExoKeyLoginState_Init)
                              InvokeExecuteJavaScript("$('#status_net_available').attr('class', 'label label-success');"
                                 + "$('#status_net_available').text('OK');");
                    }
                    else
                    {
                        if (Login_State == ExoKeyLoginState.ExoKeyLoginState_Init)
                           InvokeExecuteJavaScript("$('#status_net_available').attr('class', 'label label-danger');"
                             + "$('#status_net_available').text('Failed');");
                    }

                 //   Check_Internet_Access();

                    if (Has_Internet_Access)
                    {
                        // state changed

                        InvokeExecuteJavaScript("$('#status_internet_connectity').attr('class', 'label label-success');"
                          + "$('#status_internet_connectity').text('OK');");

                        // set EK NS
                        SetNameservers("ExoKey", "8.8.8.8,8.8.4.4");
                    }
                }

                if (!Has_Internet_Access && Login_State == ExoKeyLoginState.ExoKeyLoginState_Init)
                {

                    InvokeExecuteJavaScript("$('#status_internet_connectity').attr('class', 'label label-danger');"
                         + "$('#status_internet_connectity').text('Failed');");
                }


                if (!Dependency_Services_Running )
                {
                    if (Login_State == ExoKeyLoginState.ExoKeyLoginState_Init)
                        InvokeExecuteJavaScript("$('#status_windows_service_deps').attr('class', 'label label-primary');"
                        + "$('#status_windows_service_deps').text('Checking');");
                    Check_ICS_Dependencies();
                }

                if (Dependency_Services_Running && Login_State == ExoKeyLoginState.ExoKeyLoginState_Init)
                {
                    InvokeExecuteJavaScript("$('#status_windows_service_deps').attr('class', 'label label-success');"
                    + "$('#status_windows_service_deps').text('OK');");
                }
                if (!Keep_Running)
                    break;


                if (!Network_Interfaces_OK)
                    Configure_Internet_Interfaces();

                if (Network_Interfaces_OK && Login_State == ExoKeyLoginState.ExoKeyLoginState_Init)
                {
                 
                    InvokeExecuteJavaScript("$('#status_network_interfasces').attr('class', 'label label-success');"
                        + "$('#status_network_interfacees_msg').text('');"
                        + "$('#status_network_interfasces').text('OK');");

                }

                if (Network_Interfaces_OK && !ICS_Configured
                    && System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable() && Has_Internet_Access)
                {
                    Enable_ICS();

                    // If state change occured
                    if (ICS_Configured)
                    {
                        Check_Intf_Status();

                        if (Login_State != ExoKeyLoginState.ExoKeyLoginState_LoadingUi)
                        {
                            Login_State = ExoKeyLoginState.ExoKeyLoginState_LoadingUi;
                            InvokeExecuteJavaScript("$('#status_windows_ics').attr('class', 'label label-success');"
                              + "$('#status_windows_ics').text('OK');");
                            InvokeExecuteJavaScript("setInterval(function(){ "
                               + " document.location.href='https://192.168.137.2/'; }, 2000);");
                        }

                    }
                    else
                    {
                        InvokeExecuteJavaScript("$('#status_windows_ics').attr('class', 'label label-danger');"
                                                 + "$('#status_windows_ics').text('Failed');");
                    }
                }
                if (!Keep_Running)
                    break;

                if (!Has_Internet_Access || !System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
                {
                    InvokeExecuteJavaScript("$('#status_windows_ics').attr('class', 'label label-warning');"
                    + "$('#status_windows_ics').text('Waiting for Internet Access');");
                } 
                else if (ICS_Configured && Login_State == ExoKeyLoginState.ExoKeyLoginState_Init)
                {
                    if (Client_USB_IP != null)
                    {
                        try
                        {
                            Send_MCast_Announce_Msg(Client_USB_IP);
                        }
                        catch
                        {
                            // Ingnore error OK. 
                        }
                    }
                    if (Login_State != ExoKeyLoginState.ExoKeyLoginState_LoadingUi)
                    {
                        Login_State = ExoKeyLoginState.ExoKeyLoginState_LoadingUi;
                        InvokeExecuteJavaScript("setInterval(function() { "
                               + " document.location.href='https://192.168.137.2/'; }, 2500);");
                    }
                }
                else if (Login_State == ExoKeyLoginState.ExoKeyLoginState_Init)
                {
                    InvokeExecuteJavaScript("$('#status_windows_ics').attr('class', 'label label-danger');"
                    + "$('#status_windows_ics').text('Failed');");
                }

                if (Login_State == ExoKeyLoginState.ExoKeyLoginState_Loggedin)
                    Get_VPN_Status();

            next_loop:
                System.Threading.Thread.Sleep(900);

//                if (Browser != null)
//                    InvokeExecuteJavaScript("console.log('starting ExoKey')");
            }
        }
        private void Set_EK_State(ExoKeyState New_State)
        {
            if (New_State != EK_State)
            {
                if (EK_State_Change != null)
                    EK_State_Change(EK_State, New_State);

                EK_State = New_State;
            }
        }
        /*
        public void Startup()
        {
            startup_bw.DoWork += new DoWorkEventHandler(startup_DoWork);
            startup_bw.RunWorkerAsync();
        }
         */
        public void DisplayDnsAddresses()
        {
            NetworkInterface[] adapters = NetworkInterface.GetAllNetworkInterfaces();
            foreach (NetworkInterface adapter in adapters)
            {

                IPInterfaceProperties adapterProperties = adapter.GetIPProperties();
                IPAddressCollection dnsServers = adapterProperties.DnsAddresses;
                if (dnsServers.Count > 0)
                {
                    Console.WriteLine(adapter.Description);
                    foreach (IPAddress dns in dnsServers)
                    {
                        Console.WriteLine("  DNS Servers ............................. : {0}",
                            dns.ToString());
                        Send_Log_Msg(0, LogMsg.Priority.Debug, "  DNS Servers  : " + dns.ToString()
                            + " Adapter: " + adapter.Description);
                    }
                    Console.WriteLine();
                }
            }
        }
        /*
        private void startup_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;
            //   Send_Log_Msg("Worker Startup");
            try
            {
                Check_State_Timer = new System.Timers.Timer(1000);
                Check_State_Timer.Elapsed += new System.Timers.ElapsedEventHandler(Check_State_Timer_Expired);
                Check_State_Timer.Start();
                StartMultiCastReciever();
                DisplayDnsAddresses();
                SetNameservers("ExoKey", "8.8.8.8,8.8.4.4");
            }
            catch (Exception ex)
            {
                Send_Log_Msg(0, LogMsg.Priority.Debug, "startup_DoWork Execption:" + ex.ToString() + "  " + ex.StackTrace.ToString());
            }
        }
         */
        public void Stop()
        {
            Stop_VPN();
            Keep_Running = false;
            Remove_Routes();

            try
            {
                DisableICS();
            }
            catch
            {

            }
            Browser = null;
            /*
            if (Check_State_Timer != null)
            {
                Check_State_Timer.Enabled = false;
                Check_State_Timer.Stop();
                Check_State_Timer.Dispose();
                Check_State_Timer = null;
            }

            Stop_VPN();
            Disposing = true;

            if (Check_State_Timer_Running)
            {
                Console.WriteLine("Check_State_Timer_Running");
                for (int i = 0; i < 10 && Check_State_Timer_Running; i++)
                {
                    System.Threading.Thread.Sleep(500);
                }
            }*/

            Log_Msg_Send_Event = null;
            Console.WriteLine("XoKey stop done.");
        }
        public void Dispose()
        {
            Disposing = true;


            if (Traffic_Routed_To_XoKey)
            {
                Remove_Routes();
            }
            System.Diagnostics.Debug.WriteLine("XoKey dispose done.");
        }
        private McastHeartBeatData ByteArr2McastHeartBeatData(byte[] bytes)
        {
            GCHandle handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            McastHeartBeatData stuff = (McastHeartBeatData)Marshal.PtrToStructure(
                handle.AddrOfPinnedObject(), typeof(McastHeartBeatData));
            handle.Free();
            return stuff;
        }


        /*
              protected void OnClientRecv(object sender, SocketAsyncEventArgs ea)
              {
                  try
                  {
                      McastHeartBeatData hbeat_data;
                      var info = ea.ReceiveMessageFromPacketInfo;
                      Debug.WriteLine("Data recieved from client " + ea.RemoteEndPoint.ToString());

                      hbeat_data = ByteArr2McastHeartBeatData(ea.Buffer);
                      ea.Dispose();
                      SetupClientRecv(); // recv next packet
                  }
                  catch (Exception ex)
                  {
                      // Note, this happens when we were disposed of. when TCP controlling connection was closed
                      Debug.WriteLine("OnClientRecv Ex:" + ex.Message);
                  }
              }
               */
        public void SysLog_ReceiveCallback(IAsyncResult ar)
        {
   //         IEnumerable<UnicastIPAddressInformation> addresses;
            UdpClient udp_client = (UdpClient)((UdpState)(ar.AsyncState)).udp_client;
            IPEndPoint end_point = (IPEndPoint)((UdpState)(ar.AsyncState)).end_point;
            PriStruct Pri;
            System.Text.RegularExpressions.Regex mRegex;
            IPEndPoint localEp = new IPEndPoint(IPAddress.Any, 1500);
            Byte[] bytes = udp_client.EndReceive(ar, ref localEp);
            String Parsed_Msg;

            try
            {
                if (bytes.Length < 3)
                {
                    // Invalid packet
                    goto queue_next;
                }
                string msg = System.Text.ASCIIEncoding.ASCII.GetString(bytes, 0, bytes.Length);

                lock (Last_SysLog_Msg)
                {
                    // avoid duplicate messages
                    if (msg == Last_SysLog_Msg)
                        goto queue_next;
                    Last_SysLog_Msg = msg;
                }

                mRegex = new System.Text.RegularExpressions.Regex("<(?<PRI>([0-9]{1,3}))>(?<Message>.*)",
                    System.Text.RegularExpressions.RegexOptions.Compiled);
                System.Text.RegularExpressions.Match tmpMatch = mRegex.Match(msg);
                Pri = new PriStruct(tmpMatch.Groups["PRI"].Value);
                Parsed_Msg = tmpMatch.Groups["Message"].Value;

                Send_Log_Msg(0, (LogMsg.Priority)Pri.Severity, "EK: " + Parsed_Msg);

            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Ex Syslog Recv:" + ex.ToString());
            }

        queue_next:
            udp_client.BeginReceive(SysLog_ReceiveCallback, ar.AsyncState); // recieve next packet
        }
        public void ReceiveCallback(IAsyncResult ar)
        {
            IEnumerable<UnicastIPAddressInformation> addresses;
            UdpClient udp_client = (UdpClient)((UdpState)(ar.AsyncState)).udp_client;
            IPEndPoint end_point = (IPEndPoint)((UdpState)(ar.AsyncState)).end_point;
            const int MAX_ADDRS = 8;

            IPEndPoint localEp = new IPEndPoint(IPAddress.Any, 1500);
            Byte[] bytes = udp_client.EndReceive(ar, ref localEp);
            if (bytes.Length != 108)
            {
                // Invalid packet
                goto queue_next;
            }

            McastHeartBeatData hbeat_data = ByteArr2McastHeartBeatData(bytes);

            if (hbeat_data.Magic != 0xDEADBEEF)
            {
                // Invalid packet
                goto queue_next;
            }

            if (hbeat_data.Product_ID != 2)
            {
                goto queue_next;
            }

            addresses = GetEKUnicastAddresses();
            IPAddress New_IP = null;
            bool IP_Reachable = false;
            foreach (UnicastIPAddressInformation unicastIPAddress in addresses)
            {
                for (int i = 0; i < MAX_ADDRS && i < hbeat_data.Num_Addr; i++)
                {
                    int offset = 12 + (i * 4);
                    New_IP = IPAddress.Parse(bytes[offset] + "." + bytes[offset + 1] + "." + bytes[offset + 2] + "." + bytes[offset + 3]);

                    byte[] maskBytes = unicastIPAddress.IPv4Mask.GetAddressBytes();
                    byte[] myIPBytes = unicastIPAddress.Address.GetAddressBytes();
                    byte[] ekIPbytes = New_IP.GetAddressBytes();

                    IP_Reachable = true;
                    for (int b = 0; b < myIPBytes.Length; b++)
                    {
                        // Check if IP matches our range by the netmask
                        if ((myIPBytes[b] & maskBytes[b]) != (ekIPbytes[b] & maskBytes[b]))
                        {
                            IP_Reachable = false;  // This IP is un reachable
                            break;
                        }

                    }
                    if (IP_Reachable)
                        break; // We found a reachable one, get out of here
                }
                if (IP_Reachable)
                    break; // We found a reachable one, get out of here
            }


            //  Console.WriteLine("Received: {0}", bytes);
            if (IP_Reachable && New_IP != null && !New_IP.Equals(XoKey_IP) && Has_Internet_Access && ICS_Configured)
            {
                Send_Log_Msg(0, LogMsg.Priority.Info, "New ExoKey IP Detected: " + New_IP.ToString());
                XoKey_IP = New_IP;

                if (Browser != null && New_IP.ToString() != "192.168.255.1")
                {

                    Login_State = ExoKeyLoginState.ExoKeyLoginState_LoadingUi;
                    InvokeExecuteJavaScript("setInterval(function(){ "
                     + " document.location.href='https://" + New_IP.ToString() + "/'; }, 2000);");
                }


                if (EK_IP_Address_Detected != null)
                {
                    EK_IP_Address_Detected(XoKey_IP);
                    //Ping_For_Client_IP();
                }
            }
        queue_next:
            udp_client.BeginReceive(ReceiveCallback, ar.AsyncState); // recieve next packet
        }

        private IEnumerable<UnicastIPAddressInformation> GetEKUnicastAddresses()
        {
            //  IEnumerable<IPAddress> addresses = new IEnumerable<IPAddress>();
            List<UnicastIPAddressInformation> addresses = new List<UnicastIPAddressInformation>();

            // join multicast group on all available network interfaces
            System.Net.NetworkInformation.NetworkInterface[] networkInterfaces =
                    System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces();


            foreach (NetworkInterface networkInterface in networkInterfaces)
            {
                if ((!networkInterface.Supports(NetworkInterfaceComponent.IPv4)) ||
                     (networkInterface.OperationalStatus != OperationalStatus.Up))
                {
                    continue;
                }

                // Only get IP addres of USB ethernet  (Exo Key IP)
                if (!networkInterface.Description.Contains("XoWare") && !networkInterface.Description.Contains("x.o.ware"))
                    continue;

                IPInterfaceProperties adapterProperties = networkInterface.GetIPProperties();
                UnicastIPAddressInformationCollection unicastIPAddresses = adapterProperties.UnicastAddresses;
                IPAddress ipAddress = null;
                foreach (UnicastIPAddressInformation unicastIPAddress in unicastIPAddresses)
                {
                    ipAddress = unicastIPAddress.Address;

                    if (ipAddress == null)
                        continue;

                    // Only get IPv4
                    if (unicastIPAddress.Address.AddressFamily != AddressFamily.InterNetwork)
                        continue;

                    if (ipAddress.Equals(IPAddress.Parse("127.0.0.1")))
                        continue;


                    addresses.Add(unicastIPAddress);
                }

            }
            return addresses;
        }
        private IEnumerable<IPAddress> GetLocalIpAddresses()
        {
            //  IEnumerable<IPAddress> addresses = new IEnumerable<IPAddress>();
            List<IPAddress> addresses = new List<IPAddress>();

            // join multicast group on all available network interfaces
            System.Net.NetworkInformation.NetworkInterface[] networkInterfaces =
                    System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces();


            foreach (NetworkInterface networkInterface in networkInterfaces)
            {
                if ((!networkInterface.Supports(NetworkInterfaceComponent.IPv4)) ||
                     (networkInterface.OperationalStatus != OperationalStatus.Up))
                {
                    continue;
                }

                // Only get IP addres of USB ethernet  (Exo Key IP)
                if (!networkInterface.Description.Contains("x.o.ware"))
                    continue;

                IPInterfaceProperties adapterProperties = networkInterface.GetIPProperties();
                UnicastIPAddressInformationCollection unicastIPAddresses = adapterProperties.UnicastAddresses;
                IPAddress ipAddress = null;
                Send_Log_Msg("networkInterface:" + networkInterface.Description + " Name=" + networkInterface.Name);
                foreach (UnicastIPAddressInformation unicastIPAddress in unicastIPAddresses)
                {
                    ipAddress = unicastIPAddress.Address;

                    if (ipAddress == null)
                        continue;

                    // Only get IPv4
                    if (unicastIPAddress.Address.AddressFamily != AddressFamily.InterNetwork)
                        continue;

                    if (ipAddress.Equals(IPAddress.Parse("127.0.0.1")))
                        continue;


                    addresses.Add(ipAddress);
                }

            }
            return addresses;
        }
        byte[] getAnnounceDataBytes(McastHeartBeatData announce_data)
        {
            int size = Marshal.SizeOf(announce_data);
            byte[] arr = new byte[size];
            IntPtr ptr = Marshal.AllocHGlobal(size);

            Marshal.StructureToPtr(announce_data, ptr, true);
            Marshal.Copy(ptr, arr, 0, size);
            Marshal.FreeHGlobal(ptr);

            return arr;
        }
        private void Send_MCast_Announce_Msg(IPAddress Local_IP)
        {
            if (Local_IP.ToString().Contains("169.254."))
                return; // Don't do empty IP, DCHP ranges


            McastHeartBeatData announce_data = new McastHeartBeatData();
            UdpClient Mcast_UDP_Client = new UdpClient(AddressFamily.InterNetwork);

            IPEndPoint Remote_EP = new IPEndPoint(ExoKey_Multicast_Address, MCast_Port);
            Mcast_UDP_Client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            Mcast_UDP_Client.Client.Bind(new IPEndPoint(Local_IP, MCast_Port));
            Mcast_UDP_Client.JoinMulticastGroup(ExoKey_Multicast_Address, Local_IP);
            Mcast_UDP_Client.Ttl = 1;


            UdpState state = new UdpState();
            state.udp_client = Mcast_UDP_Client;
            state.end_point = new IPEndPoint(Local_IP, MCast_Port);

            announce_data.Version = 1;
            announce_data.Magic = 0xDEADBEEF;
            announce_data.Num_Addr = 1;
            announce_data.IP_Address = new uint[8];
            announce_data.IP_Address[0] = BitConverter.ToUInt32(Local_IP.GetAddressBytes(), 0);
            announce_data.Addr_Prefix = new uint[8];
            announce_data.Addr_Prefix[0] = 24;
            announce_data.Product_ID = 3;
            announce_data.unixtime = (uint)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
            byte[] mcast_bytes = getAnnounceDataBytes(announce_data);
            Mcast_UDP_Client.Send(mcast_bytes,
                Marshal.SizeOf(announce_data), Remote_EP);

        }
        private bool Try_MCast_Bind(IPAddress Local_IP, int retries)
        {

            try
            {
                Client_USB_IP = Local_IP;
                Send_MCast_Announce_Msg(Local_IP);
                UdpClient Mcast_UDP_Client = new UdpClient(AddressFamily.InterNetwork);

                IPEndPoint Remote_EP = new IPEndPoint(ExoKey_Multicast_Address, MCast_Port);
                Mcast_UDP_Client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                Mcast_UDP_Client.Client.Bind(new IPEndPoint(Local_IP, MCast_Port));
                Mcast_UDP_Client.JoinMulticastGroup(ExoKey_Multicast_Address, Local_IP);
                Mcast_UDP_Client.Ttl = 1;


                UdpState state = new UdpState();
                state.udp_client = Mcast_UDP_Client;
                state.end_point = new IPEndPoint(Local_IP, MCast_Port);


                Mcast_UDP_Client.BeginReceive(new AsyncCallback(ReceiveCallback), state);
                return true;
            }
            catch (Exception ex)
            {
                Send_Log_Msg(1, LogMsg.Priority.Debug, "MultiCastReciever ex " + Local_IP.ToString() + " "
                    + ex.Message.ToString() + " Retries Remaining:" + retries);
                System.Threading.Thread.Sleep(123);
                return false;
            }
        }
        private bool Try_SysLog_MCast_Bind(IPAddress Local_IP, int retries)
        {

            try
            {
                Client_USB_IP = Local_IP;
                Send_MCast_Announce_Msg(Local_IP);
                UdpClient Mcast_UDP_Client = new UdpClient(AddressFamily.InterNetwork);

                IPEndPoint Remote_EP = new IPEndPoint(ExoKey_SysLogMcast_Address, SysLog_MCast_Port);
                Mcast_UDP_Client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                Mcast_UDP_Client.Client.Bind(new IPEndPoint(Local_IP, SysLog_MCast_Port));
                Mcast_UDP_Client.JoinMulticastGroup(ExoKey_SysLogMcast_Address, Local_IP);


                UdpState state = new UdpState();
                state.udp_client = Mcast_UDP_Client;
                state.end_point = new IPEndPoint(Local_IP, MCast_Port);


                Mcast_UDP_Client.BeginReceive(new AsyncCallback(SysLog_ReceiveCallback), state);
                Send_Log_Msg(1, LogMsg.Priority.Debug, "Syslog MultiCastReciever bound " + Local_IP.ToString());
                return true;
            }
            catch
            {
                //                Send_Log_Msg(1, LogMsg.Priority.Debug, "Syslog MultiCastReciever ex " + Local_IP.ToString() + " "
                //                    + ex.Message.ToString() + " Retries Remaining:" + retries);
                return false;
            }

        }
        private void StartMultiCastReciever()
        {

            try
            {
                IEnumerable<IPAddress> Local_IP_Addresses = GetLocalIpAddresses();

                foreach (IPAddress Local_IP in Local_IP_Addresses)
                {
                    bool Success = false;

                    for (int retry = 5; retry > 0 && !Success; retry--)
                    {
                        Success = Try_MCast_Bind(Local_IP, retry);

                        Try_SysLog_MCast_Bind(Local_IP, retry);
                    }
                    if (Success)
                    {
                        Send_Log_Msg(1, LogMsg.Priority.Debug, "Success listening to" + Local_IP.ToString());
                        MCast_Listening.Add(Local_IP);
                    }
                    else
                    {
                        if (!MCast_Listening.Contains(Local_IP))
                            Send_Log_Msg(1, LogMsg.Priority.Debug, "Failed listening to" + Local_IP.ToString());
                    }
                }


            }
            catch (Exception ex)
            {
                Send_Log_Msg(1, LogMsg.Priority.Error, "MultiCastReciever ex "
                    + ex.Message.ToString());
            }

        }
        void Restart_Detection()
        {
            Set_EK_State(ExoKeyState.ExoKeyState_Disconnected);

            Login_State = ExoKeyLoginState.ExoKeyLoginState_Init;
            Stop_VPN();
            Remove_Routes();
            DisableICS();

            Has_Internet_Access = false;
            Network_Interfaces_OK = false;
            Internet_Interface = null;
            Server_IPEndPoint = null;
            USB_Dev_ID_Found = false;
            ExoKey_Driver_Found = false;
            InvokeExecuteJavaScript("if (document.location.href.indexOf('custom://') < 0) document.location.href='custom://cefsharp/home';");

        }

        void Check_Intf_Status()
        {
            bool EK_Is_Up = false;
            NetworkInterface EK_Interface = null;
            bool Interenet_Interface_Found = false;

            NetworkInterface[] adapters = NetworkInterface.GetAllNetworkInterfaces();
            foreach (NetworkInterface n in adapters)
            {
                Send_Log_Msg(1, LogMsg.Priority.Debug, n.Name + " : " + n.Description + " is " + n.OperationalStatus);

                if (n.Description.Contains("XoWare") || n.Description.Contains("x.o.ware")){
                    EK_Interface = n;
                    
                    if (n.OperationalStatus == OperationalStatus.Up)  
                    {
                        EK_Is_Up = true;
                        EK_Intf_Down_Count = 0;
                        // Needed for possible restart or plug, unplug, replug
                        StartMultiCastReciever();
                    }
                }  else if (n.OperationalStatus == OperationalStatus.Down && EK_State == ExoKeyState.ExoKeyState_Connected
                    && (n.Description.Contains("XoWare") || (n.Description.Contains("x.o.ware"))))
                {
                    Send_Log_Msg(1, LogMsg.Priority.Error, "ExoKey Down");
                }
                if (Internet_Interface != null && Internet_Interface.Id == n.Id)
                {
                    Interenet_Interface_Found = true;
                    if (n.OperationalStatus == OperationalStatus.Down)
                    {
                        // load custom://cefsharp/home
                        Send_Log_Msg(1, LogMsg.Priority.Debug, "Internet interface down");
                      //  Restart_Detection();              
                    }
                }
            }

            // if internet interface disappeared. eg  Wifi disabled
            if (Internet_Interface != null && !Interenet_Interface_Found)
                Restart_Detection();

            // EK must have been removed
            if (Exokey_Interface != null && EK_Interface == null)
            {
                Exokey_Interface = null;
                Send_Log_Msg(1, LogMsg.Priority.Debug, "ExoKey Interface down");
                Restart_Detection();
                Set_EK_State(ExoKeyState.ExoKeyState_Unplugged);
            }

            if (!EK_Is_Up || EK_Interface == null)
            {
                EK_Intf_Down_Count++;
                if (EK_Intf_Down_Count > 25)
                    Set_EK_State(ExoKeyState.ExoKeyState_Unplugged);
            }
        }
        void AddressChangedCallback(object sender, EventArgs e)
        {
            Check_Intf_Status();
        }

        protected virtual void Send_Log_Msg(string Log_Msg, LogMsg.Priority priority = LogMsg.Priority.Info, int code = 0)
        {
            if (Disposing)
                return;
            /*
            lock (event_locker)
            {
                if (Log_Msg_Send_Event == null)
                    return;

                LogMsg Msg = new LogMsg(Log_Msg, priority, code);
                Log_Msg_Send_Event(Msg);
            }
            */

            if (System.Threading.Monitor.TryEnter(event_locker, new TimeSpan(0, 0, 1)))
            {
                try
                {
                    if (Log_Msg_Send_Event == null)
                        return;

                    LogMsg Msg = new LogMsg(Log_Msg, priority, code);
                    Log_Msg_Send_Event(Msg);
                }
                finally
                {
                    System.Threading.Monitor.Exit(event_locker);
                }
            }
        }
        private void Send_Log_Msg(int code, LogMsg.Priority priority, string Log_Msg)
        {
            Console.WriteLine("ExoKey: " + Log_Msg);
            App.Log("EK APP:" + Log_Msg);
        }

        public void Stop_VPN()
        {
            if (Browser != null)
                InvokeExecuteJavaScript("jQuery.getJSON('/api/StopVpn');");

            /*
            WebResponse response = null;
            System.Diagnostics.Debug.WriteLine("Stop_VPN");
            if (Session_Cookie.Length < 3)
            {
                return;
            }
            if (XoKey_IP == null)
                return;

            HttpWebRequest wr = (HttpWebRequest)WebRequest.Create("https://" + XoKey_IP.ToString() + "/api/StopVpn");
            wr.Method = "GET";
            wr.CookieContainer = new CookieContainer();
            Cookie cook = Cookie_Str_To_Cookie(Session_Cookie);
            wr.CookieContainer.Add(cook);

            try
            {
                wr.Timeout = 2000; //Set 2 sec timeout
                response = wr.GetResponse();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("stop ex " + ex.ToString());
                Send_Log_Msg(0, LogMsg.Priority.Warning, "No connection or response from Exokey " + XoKey_IP.ToString());
                return;
            }
            //    Send_Log_Msg("GetVpnStatus: status=" + ((HttpWebResponse)response).StatusDescription, LogMsg.Priority.Debug);

            try
            {
                // Get the stream containing content returned by the server.
                System.IO.Stream dataStream = response.GetResponseStream();


                DataContractJsonSerializer jsonSerializer = new DataContractJsonSerializer(typeof(XoKeyApi.VpnStatusResponse));
                object objResp = jsonSerializer.ReadObject(dataStream);
                XoKeyApi.StopVpnResponse vpn_response = objResp as XoKeyApi.StopVpnResponse;

                Send_Log_Msg("Stop Response: " + vpn_response.ack.msg);
            }
            catch
            {

            }
             **/
        }

        private void Get_VPN_Status()
        {

            if (Browser == null || Login_State != ExoKeyLoginState.ExoKeyLoginState_Loggedin)
                return;

            int diff = DateTime.Compare(DateTime.Now , Last_Vpn_Status);

            if (diff < 3)
                return;


            InvokeExecuteJavaScript(@"jQuery.getJSON('/api/GetVpnStatus',  function( response ) {
             if (!response || response == undefined)
                return;

            if (response.active_vpn == null) {
                console.log('VPNStatus=Stopped');
                return;
            }

            if (response.active_vpn.state == 'Connected'  && response.active_vpn.address)
            {
                if(response.active_vpn.address[0].ip != undefined)
                    console.log('VPNStatus=Connected='+response.active_vpn.address[0].ip);
                else
                    console.log('VPNStatus=Connected='+response.active_vpn.address[0].host);
            } else
                console.log('VPNStatus=Stopped');
}).fail( function(jqxhr, textStatus, error) {
  var err = textStatus + ' : ' + error;
  console.log('ERROR:' + err);
  if (jqxhr && jqxhr.status)
    console.log('status code' +  jqxhr.status);
  else
    console.log('ExoKeyStatus=NotReachable');

  
});");

            /*
            WebResponse response = null;
            if (Session_Cookie.Length < 3)
            {
                return;
            }
            if (XoKey_IP == null)
                return;

            HttpWebRequest wr = (HttpWebRequest)WebRequest.Create("https://" + XoKey_IP.ToString() + "/api/GetVpnStatus");
            wr.Method = "GET";
            wr.Timeout = 6000;
            wr.CookieContainer = new CookieContainer();
            Cookie cook = Cookie_Str_To_Cookie(Session_Cookie);
            wr.CookieContainer.Add(cook);

            try
            {
                response = wr.GetResponse();

            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception Get_VPN_Status: " + ex.Message);
                Console.WriteLine("Exception Get_VPN_Status: " + ex.StackTrace.ToString());
                No_EK_Status_Error_Count++;
                Send_Log_Msg(0, LogMsg.Priority.Warning, "No connection or response from Exokey " + XoKey_IP.ToString() + " Err count=" + No_EK_Status_Error_Count);
                if (No_EK_Status_Error_Count > 10)
                    Set_EK_State(ExoKeyState.ExoKeyState_Unplugged);
                return;
            }
            //    Send_Log_Msg("GetVpnStatus: status=" + ((HttpWebResponse)response).StatusDescription, LogMsg.Priority.Debug);

            // Get the stream containing content returned by the server.
            System.IO.Stream dataStream = response.GetResponseStream();


            DataContractJsonSerializer jsonSerializer = new DataContractJsonSerializer(typeof(XoKeyApi.VpnStatusResponse));
            object objResp = jsonSerializer.ReadObject(dataStream);
            XoKeyApi.VpnStatusResponse vpn_response = objResp as XoKeyApi.VpnStatusResponse;

            try
            {
                if (vpn_response != null && vpn_response.active_vpn != null && vpn_response.active_vpn.state == "Connected")
                {
                    String VPN_Server_Hostname = vpn_response.active_vpn.address[0].host;
                    IPAddress[] addresslist;
                    try
                    {
                        if (vpn_response.active_vpn.address[0].ip.Length > 4)
                        {
                            addresslist = new IPAddress[1];

                            addresslist[0] = IPAddress.Parse(vpn_response.active_vpn.address[0].ip);

                        }
                        else
                        {
                            try
                            {
                                addresslist = Dns.GetHostAddresses(VPN_Server_Hostname);
                            }
                            catch (Exception ex)
                            {
                                Send_Log_Msg(0, LogMsg.Priority.Warning, "Can't resolve DNS address:" + VPN_Server_Hostname
                                      + " : " + ex.GetType().ToString());
                                throw ex;
                            }
                        }
                        No_EK_Status_Error_Count = 0; //OK
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine(" Response Ex " + ex.ToString());

                        throw ex;
                    }
                    IPEndPoint Server = new IPEndPoint(addresslist[0], 0);
                    Set_Sever_IPEndpoint(Server);
                    Set_EK_State(ExoKeyState.ExoKeyState_Connected);

                }
                else
                {
                    Remove_Routes();
                    Set_EK_State(ExoKeyState.ExoKeyState_Disconnected);
                    No_EK_Status_Error_Count = 0; //OK
                }

            }
            catch (Exception ex)
            {
                Send_Log_Msg(0, LogMsg.Priority.Debug, "Ex " + ex.ToString() + ex.StackTrace.ToString());
                Send_Log_Msg(0, LogMsg.Priority.Debug, dataStream.ToString());
            }

            response.Close(); // cleanup;
             */
        }

        /// <summary>
        /// Set's the DNS Server of the local machine
        /// </summary>
        /// <param name="nic">NIC address</param>
        /// <param name="dnsServers">Comma seperated list of DNS server addresses</param>
        /// <remarks>Requires a reference to the System.Management namespace</remarks>
        public void SetNameservers(string nic, string dnsServers)
        {
            using (var networkConfigMng = new System.Management.ManagementClass("Win32_NetworkAdapterConfiguration"))
            {
                using (var networkConfigs = networkConfigMng.GetInstances())
                {
                    //                   foreach (var managementObject in networkConfigs.Cast<ManagementObject>().Where(objMO => (bool)objMO["IPEnabled"] && objMO["Caption"].Equals(nic)))
                    foreach (var managementObject in networkConfigs.Cast<ManagementObject>().Where(objMO => (bool)objMO["IPEnabled"] && objMO["Description"].ToString().Contains(nic)))
                    //                    foreach (var managementObject in networkConfigs.Cast<ManagementObject>().Where(objMO => (bool)objMO["IPEnabled"] )) 
                    {
                        using (var newDNS = managementObject.GetMethodParameters("SetDNSServerSearchOrder"))
                        {
                            newDNS["DNSServerSearchOrder"] = dnsServers.Split(',');
                            managementObject.InvokeMethod("SetDNSServerSearchOrder", newDNS, null);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Set's a new IP Address and it's Submask of the local machine
        /// </summary>
        /// <param name="ip_address">The IP Address</param>
        /// <param name="subnet_mask">The Submask IP Address</param>
        /// <remarks>Requires a reference to the System.Management namespace</remarks>
        public void setIP(string nic, string ip_address, string subnet_mask)
        {
            ManagementClass objMC = new ManagementClass("Win32_NetworkAdapterConfiguration");
            ManagementObjectCollection objMOC = objMC.GetInstances();

            foreach (ManagementObject objMO in objMOC)
            {
                if ((bool)objMO["IPEnabled"])
                {
                    try
                    {

                        if (!objMO["Description"].ToString().Contains(nic))
                            continue;

                        ManagementBaseObject setIP;
                        ManagementBaseObject newIP =
                            objMO.GetMethodParameters("EnableStatic");

                        newIP["IPAddress"] = new string[] { ip_address };
                        newIP["SubnetMask"] = new string[] { subnet_mask };

                        setIP = objMO.InvokeMethod("EnableStatic", newIP, null);
                    }
                    catch (Exception)
                    {
                        throw;
                    }


                }
            }
        }
        /// <summary>
        /// Set's a new Gateway address of the local machine
        /// </summary>
        /// <param name="gateway">The Gateway IP Address</param>
        /// <remarks>Requires a reference to the System.Management namespace</remarks>
        public void setGateway(string nic, string gateway)
        {
            ManagementClass objMC = new ManagementClass("Win32_NetworkAdapterConfiguration");
            ManagementObjectCollection objMOC = objMC.GetInstances();

            foreach (ManagementObject objMO in objMOC)
            {
                if ((bool)objMO["IPEnabled"])
                {
                    try
                    {
                        ManagementBaseObject setGateway;
                        ManagementBaseObject newGateway =
                            objMO.GetMethodParameters("SetGateways");

                        newGateway["DefaultIPGateway"] = new string[] { gateway };
                        newGateway["GatewayCostMetric"] = new int[] { 1 };

                        setGateway = objMO.InvokeMethod("SetGateways", newGateway, null);
                    }
                    catch (Exception)
                    {
                        throw;
                    }
                }
            }
        }

        private Cookie Cookie_Str_To_Cookie(String Cook_Str)
        {

            string[] vals = Cook_Str.Split('=');

            if (vals.Length != 2)
            {
                Send_Log_Msg(0, LogMsg.Priority.Error, "Parsing cookie string" + Cook_Str);
                return null;

            }
            Cookie cook = new Cookie(vals[0], vals[1]);
            cook.Domain = XoKey_IP.ToString();
            cook.Path = "/";
            return cook;
        }
        private bool Check_Available_Server_Port(int port)
        {
            Send_Log_Msg(0, LogMsg.Priority.Debug, "Checking Port: " + port);
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

            Send_Log_Msg(0, LogMsg.Priority.Debug, "Port " + port + " available = " + isAvailable);
            return isAvailable;
        }
        private void Run_NetSh_Cmd(String Command)
        {
            System.Diagnostics.Process proc;
            proc = new System.Diagnostics.Process();
            string NetSh_Path = Environment.GetFolderPath(Environment.SpecialFolder.SystemX86);
            proc.StartInfo.FileName = NetSh_Path + "\\netsh ";
            proc.StartInfo.Arguments = Command;
            Send_Log_Msg(proc.StartInfo.FileName + " " + Command);
            System.Diagnostics.Debug.WriteLine(proc.StartInfo.FileName + " " + Command);

            // Set UseShellExecute to false for redirection.
            proc.StartInfo.UseShellExecute = false;

            // Redirect the standard output of the sort command.   
            // This stream is read asynchronously using an event handler.
            proc.StartInfo.RedirectStandardOutput = true;
            proc.StartInfo.CreateNoWindow = true;
            //Output = new StringBuilder("");

            // Set our event handler to asynchronously read the sort output.
            proc.OutputDataReceived += new System.Diagnostics.DataReceivedEventHandler(NetShOutputHandler);

            // Redirect standard input as well.  This stream 
            // is used synchronously.
            proc.StartInfo.RedirectStandardInput = true;

            // Start the process.
            proc.Start();


            // Start the asynchronous read of the sort output stream.
            proc.BeginOutputReadLine();
            proc.WaitForExit(3210);
        }
        private void Open_Firewall()
        {
            if (Firewall_Opened == true)
                return;

            Run_NetSh_Cmd("advfirewall firewall delete rule Name=\"EK_App\" dir=in");
            //   Run_NetSh_Cmd("netsh advfirewall firewall delete rule name=\"ExoKey Port 1500 inbound\"  dir=in action=allow protocol=UDP localport=1500");
            //     Run_NetSh_Cmd("netsh advfirewall firewall delete rule name=\"ExoKey Port 1500 outbound\"  dir=out action=allow protocol=UDP localport=1500");

            //      Run_NetSh_Cmd("netsh advfirewall firewall add rule name=\"ExoKey Port 1500 inbound\" dir=in action=allow protocol=UDP localport=1500");
            //     Run_NetSh_Cmd("netsh advfirewall firewall add rule name=\"ExoKey Port 1500 outbound\" dir=out action=allow protocol=UDP localport=1500");
            Run_NetSh_Cmd("advfirewall firewall add rule name=\"EK_App\" dir=in action=allow program=\""
                    + System.Reflection.Assembly.GetExecutingAssembly().Location + "\" enable=yes");

            Firewall_Opened = true;

            try
            {

                Run_Route_Cmd("DELETE 0.0.0.0 MASK 128.0.0.0 192.168.255.1");
                Run_Route_Cmd("DELETE 128.0.0.0 MASK 128.0.0.0 192.168.255.1");
                Run_Route_Cmd("DELETE 0.0.0.0 MASK 128.0.0.0 192.168.137.2");
                Run_Route_Cmd("DELETE 128.0.0.0 MASK 128.0.0.0 192.168.137.2");

            }
            catch
            {
                // ignore
            }

        }
        private void Run_Route_Cmd(String Command)
        {
            System.Diagnostics.Process proc;
            proc = new System.Diagnostics.Process();
            string Route_Path = Environment.GetFolderPath(Environment.SpecialFolder.SystemX86);
            proc.StartInfo.FileName = Route_Path + "\\route ";
            proc.StartInfo.Arguments = Command;
            Send_Log_Msg(proc.StartInfo.FileName + " " + Command);
            System.Diagnostics.Debug.WriteLine(proc.StartInfo.FileName + " " + Command);

            // Set UseShellExecute to false for redirection.
            proc.StartInfo.UseShellExecute = false;

            // Redirect the standard output of the sort command.   
            // This stream is read asynchronously using an event handler.
            proc.StartInfo.RedirectStandardOutput = true;
            proc.StartInfo.CreateNoWindow = true;
            //Output = new StringBuilder("");

            // Set our event handler to asynchronously read the sort output.
            proc.OutputDataReceived += new System.Diagnostics.DataReceivedEventHandler(RouteOutputHandler);

            // Redirect standard input as well.  This stream 
            // is used synchronously.
            proc.StartInfo.RedirectStandardInput = true;

            // Start the process.
            proc.Start();


            // Start the asynchronous read of the sort output stream.
            proc.BeginOutputReadLine();
            proc.WaitForExit(3210);
        }
        private void NetShOutputHandler(object sendingProcess, System.Diagnostics.DataReceivedEventArgs outLine)
        {
            // Collect the command output.

            if (!String.IsNullOrEmpty(outLine.Data))
            {
                // numOutputLines++;

                // Add the text to the collected output.
                //   Output.Append(Environment.NewLine +  "[" + numOutputLines.ToString() + "] - " + outLine.Data);
                Send_Log_Msg("NetSh Output:" + outLine.Data);
            }
        }
        private void RouteOutputHandler(object sendingProcess, System.Diagnostics.DataReceivedEventArgs outLine)
        {
            // Collect the command output.

            if (!String.IsNullOrEmpty(outLine.Data))
            {
                // numOutputLines++;

                // Add the text to the collected output.
                //   Output.Append(Environment.NewLine +  "[" + numOutputLines.ToString() + "] - " + outLine.Data);
                Send_Log_Msg("Route Output:" + outLine.Data);
            }
        }

        private void Remove_Routes(IPEndPoint Old_Server = null)
        {
            if (Old_Server == null)
            {
                Old_Server = Server_IPEndPoint;
            }
            if (Traffic_Routed_To_XoKey == false)
                return;

            try
            {
                if (Old_Server != null && default_route.GetForardNextHopIPStr().Length > 4)
                    Run_Route_Cmd("DELETE " + Old_Server.Address.ToString() + " MASK 255.255.255.255 " + default_route.GetForardNextHopIPStr());
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Remove_Routes EK ex " + ex.ToString());
            }
            try
            {
                if (XoKey_IP != null && XoKey_IP.ToString().Length > 3)
                {
                    //System.Windows.Application.Current.MainWindow,
                  MessageBox.Show(
                      "Your network traffic is no longer encrypted by the ExoKey",
                       "Disconnected from ExoNet", MessageBoxButton.OK, MessageBoxImage.Warning,
                       MessageBoxResult.OK,  MessageBoxOptions.ServiceNotification );

                    Run_Route_Cmd("DELETE 0.0.0.0 MASK 128.0.0.0 " + XoKey_IP.ToString());
                    Run_Route_Cmd("DELETE 128.0.0.0 MASK 128.0.0.0 " + XoKey_IP.ToString());
                }

                Run_Route_Cmd("DELETE 0.0.0.0 MASK 128.0.0.0 192.168.255.1");
                Run_Route_Cmd("DELETE 128.0.0.0 MASK 128.0.0.0 192.168.255.1");
                Run_Route_Cmd("DELETE 0.0.0.0 MASK 128.0.0.0 192.168.137.2");
                Run_Route_Cmd("DELETE 128.0.0.0 MASK 128.0.0.0 192.168.137.2");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Remove_Routes def ex " + ex.ToString());
            }

           
            Traffic_Routed_To_XoKey = false;
            if (Server_IPEndPoint != null)
            {
                App.Current.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(
                () =>
                {
                    //           var notify = new NotificationWindow();
                    //            notify.Show("Disconnected");

                    var toast = new Mantin.Controls.Wpf.Notification.ToastPopUp(
                     "ExoKey",
                     "Disconnected",
                     null,
                     Mantin.Controls.Wpf.Notification.NotificationType.Information);
                     toast.Show();
                }));
            }

            Server_IPEndPoint = null;
        }

        private void Load_Routes()
        {
            if (Server_IPEndPoint == null || Server_IPEndPoint.Address == IPAddress.Any)
            {
                Send_Log_Msg("Load_Routes: Invalid Server IP", LogMsg.Priority.Info);
                return;
            }
            default_route = Xoware.RoutingLib.Routing.GetDefaultRoute();

            if (XoKey_IP == null)
                XoKey_IP = IPAddress.Parse("192.168.137.2");

            Run_Route_Cmd("ADD " + Server_IPEndPoint.Address.ToString() + " MASK 255.255.255.255 "
                + default_route.GetForardNextHopIPStr() + " METRIC 2" );
            Run_Route_Cmd("ADD 0.0.0.0 MASK 128.0.0.0 " + XoKey_IP.ToString());
            Run_Route_Cmd("ADD 128.0.0.0 MASK 128.0.0.0 " + XoKey_IP.ToString() + " METRIC 800");
            Traffic_Routed_To_XoKey = true;



            App.Current.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(
           () =>
           {
               var toast = new Mantin.Controls.Wpf.Notification.ToastPopUp(
  "ExoKey",
  "Connected to: " + Server_IPEndPoint.Address.ToString(),
  null,
  Mantin.Controls.Wpf.Notification.NotificationType.Information);
               toast.Show();
           }));

        }
        private void Set_Sever_IPEndpoint(IPEndPoint Server)
        {
            if (Server == null)
                return;
            if (Server_IPEndPoint != null && (Server_IPEndPoint.Equals(Server) || Server_IPEndPoint.Address.Equals(Server.Address)))
                return;  // do nothing, as already set

            Server_IPEndPoint = Server;
            Load_Routes();
        }

        public void Set_Session_Cookie(String Cookie)
        {
            if (Cookie == Session_Cookie)
                return;

            Session_Cookie = Cookie;



        }
       
        public void On_Power_Change(Microsoft.Win32.PowerModes Power_Mode)
        {
            switch (Power_Mode)
            {
                case Microsoft.Win32.PowerModes.Resume:

                    break;
                case Microsoft.Win32.PowerModes.Suspend:
                    Stop_VPN();
                    break;
            }
        }

        // Disable ICS on any network iterfaces which may no longer be present in the system
        public void Disable_ICS_WMI()
        {
            ManagementScope scope = new ManagementScope("\\\\.\\ROOT\\Microsoft\\HomeNet");

            //create object query
            ObjectQuery query = new ObjectQuery("SELECT * FROM HNet_ConnectionProperties ");

            //create object searcher
            ManagementObjectSearcher searcher =
                                    new ManagementObjectSearcher(scope, query);

            //get a collection of WMI objects
            ManagementObjectCollection queryCollection = searcher.Get();

      
            //enumerate the collection.
            foreach (ManagementObject m in queryCollection)
            {
                // access properties of the WMI object
                Console.WriteLine("Connection : {0}", m["Connection"]);

                try
                {
                    PropertyDataCollection properties = m.Properties;
                    foreach(PropertyData  prop in properties)
                    {
                       Console.WriteLine("name = {0}   ,  value = {1}", prop.Name, prop.Value);
                       if (prop.Name == "IsIcsPrivate" && ((Boolean) prop.Value ) == true)
                       {
                           prop.Value = false;
                           m.Put();
                       }
                    }
          
                    
                } catch (Exception e)
                {
                    Console.WriteLine("ex " + e.Message);
                    continue;
                }
            }
        }

        public void DisableICS()
        {
            IcsManager.DisableAllShares();
            var currentShare = IcsManager.GetCurrentlySharedConnections();
            ICS_Configured = false;
            if (!currentShare.Exists)
            {
                Console.WriteLine("Internet Connection Sharing is already disabled");
                Disable_ICS_WMI();
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
                Send_Log_Msg(0, LogMsg.Priority.Error, "Connection (Internet) not found:" + shared);
                return;
            }
            var homeConnection = IcsManager.FindConnectionByIdOrName(home);
            if (homeConnection == null)
            {
                Send_Log_Msg(0, LogMsg.Priority.Error, "Connection (ExoKey) not found: {0}" + home);
                return;
            }

            var currentShare = IcsManager.GetCurrentlySharedConnections();
            if (currentShare.Exists)
            {
                Send_Log_Msg(0, LogMsg.Priority.Info, "Internet Connection Sharing is already enabled:");
                Console.WriteLine(currentShare);
                if (!force)
                {
                    Send_Log_Msg(0, LogMsg.Priority.Info, "Please disable it if you want to configure sharing for other connections");
                    return;
                }
                Send_Log_Msg(0, LogMsg.Priority.Info, "Sharing will be disabled first.");
            }
            try
            {
                IcsManager.ShareConnection(connectionToShare, homeConnection);
                ICS_Configured = true;
            }
            catch (Exception e)
            {
                Console.WriteLine(String.Format("Exception {0} Trace {1}", e.Message, e.StackTrace));
               
                Send_Log_Msg(0, LogMsg.Priority.Critical,
                    "Internet Connection Sharing to ExoKey Failed. Please restart the app or your computer.  "
                    + " If the problem persists contact support. Internet: " + shared + " ExoKey:" + home);
                Send_Log_Msg(0, LogMsg.Priority.Critical, String.Format("Exception {0} Trace {1}", e.Message, e.StackTrace));
                try
                {
                    Debug_Services();
                    DisableICS();
                }
                catch
                {

                }

            }

        }
        private void Debug_Services()
        {
            System.ServiceProcess.ServiceController[] allService = System.ServiceProcess.ServiceController.GetServices();
            foreach (System.ServiceProcess.ServiceController serviceController in allService)
            {
                Send_Log_Msg(0, LogMsg.Priority.Debug, "Service: " + serviceController.ServiceName
                    + " Status: " + serviceController.Status.ToString());
            }
        }
        private bool Check_ICS_Dependencies_Single()
        {

            string[] deps = { 
                "TapiSrv", // Telephony
                "ALG", // Application Layer Gateway
                "dot3svc", // Wired AutoConfig Service
                "Netman", // Network connections
                "NlaSvc", // Network location awarenes
                "PlugPlay",
                "RasAuto",  // Remote Access Auto Connection Manage
                "RasMan",  // Remote Access Connection Manager
                "RpcSs"  // Remote Procedure Call (RPC)
             };
            bool success = true;

            foreach (String dep in deps)
            {
                try
                {
                    WinServices.StartService(dep, "Manual");
                }
                catch (Exception ex)
                {
                    success = false;
                    Send_Log_Msg(0, LogMsg.Priority.Warning, "Error Launching: " + dep + " Exception " + ex.ToString());
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
                    Dependency_Services_Running = Check_ICS_Dependencies_Single();
                    break;
                }
                catch (Exception ex)
                {
                    Dependency_Services_Running = false;
                    if (retry <= 1)
                    {
                        Debug_Services();
                        Send_Log_Msg(0, LogMsg.Priority.Warning, "Error  ICS Dependencies Exception: " + ex.ToString());
                    }
                }
            }
        }

        private void Configure_Internet_Interfaces()
        {
            try
            {
                List<NetworkInterface> Interface_List = null;
                Network_Interfaces_OK = false; // INIT

                if (!System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
                    return;
                
             // Create a UDP client, so we can figure out what interface has a route to the internet. 
                UdpClient udp_cli = new UdpClient("8.8.8.8", 53);
                IPAddress localAddr = ((IPEndPoint)udp_cli.Client.LocalEndPoint).Address; // This bound address is internet facing


                NetworkInterface Def_Intf = NetworkInterface.GetAllNetworkInterfaces().FirstOrDefault();
                Interface_List = new List<NetworkInterface>();


                Exokey_Interface = null; // init
                Internet_Interface = null;
                int i = 0;
                NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();
                foreach (NetworkInterface nic in interfaces)
                {

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
                    Send_Log_Msg(0, LogMsg.Priority.Debug, "interface: " + nic.Name + "  Desc: " + nic.Description + " Status:" + nic.OperationalStatus.ToString());
                    if (nic.Description.Contains("XoWare") || nic.Description.Contains("x.o.ware"))
                    {
                        Exokey_Interface = nic;
                        Send_Log_Msg(0, LogMsg.Priority.Debug, "Exokey interface: " + nic.Description + " " + nic.Id);
                    }
                    foreach (UnicastIPAddressInformation uni in uniCast)
                    {
                        if (uni.Address.Equals(localAddr))
                        {
                            //        Intf_comboBox.SelectedIndex = i;
                            Internet_Interface = nic;
                            Send_Log_Msg(0, LogMsg.Priority.Debug, "Internet interface: " + nic.Description + " " + nic.Id);
                        }
                    }

                    i++;

                }
                Send_Log_Msg(0, LogMsg.Priority.Debug, "Checking services");


                if (Exokey_Interface == null)
                {
                    Send_Log_Msg(0, LogMsg.Priority.Critical, "ExoKey interface not found.  "
                        + " If this is the 1st time, please wait and ensure Windows has completed the driver install. "
                        + " If the problem persists after retrying, look for the ExoKey Device in the device manager.");

                    InvokeExecuteJavaScript("$('#status_network_interfacees_msg').html('"
                        + "<div class=\\'alert alert-danger\\' role=\\'alert\\'>"
                        + "ExoKey interface not found.  "
                        + " If this is the 1st time, please wait and ensure Windows has completed the driver install. Try rebooting your PC. "
                        + " If the problem persists after retrying, look for the ExoKey Device in the device manager as a Network Adapter."
                        + "</div>');");

                /*    InvokeExecuteJavaScript(@"$('#status_network_interfacees_msg').html('
<div class=""alert alert-danger"" role=""alert"">
 Exokey interface not found.  
 If this is the 1st time, please wait and ensure Windows has completed the driver install. 
 If the problem persists after retrying, look for the ExoKey Device in the device manager.
</div>
');");*/
                    return;
                }
                else if (Internet_Interface == null)
                {
                    Send_Log_Msg(0, LogMsg.Priority.Critical, "Internet interface not found.  Please check that you have internet connectivity");
                    InvokeExecuteJavaScript("$('#status_network_interfacees_msg').html('"
+ "<div class=\\'alert alert-danger\\' role=\\'alert\\'>"
+ "Internet interface not found.  Please check that you have internet connectivity"
+ "</div>');");
                    return;
                }
                else if (Internet_Interface.Equals(Exokey_Interface))
                {
                    Send_Log_Msg(0, LogMsg.Priority.Critical, "Exokey Is internet interface.  Invalid configuration.");
                    Run_Route_Cmd("DELETE 0.0.0.0 MASK 128.0.0.0 192.168.255.1");
                    Run_Route_Cmd("DELETE 128.0.0.0 MASK 128.0.0.0 192.168.255.1");
                    Run_Route_Cmd("DELETE 0.0.0.0 MASK 128.0.0.0 192.168.137.2");
                    Run_Route_Cmd("DELETE 128.0.0.0 MASK 128.0.0.0 192.168.137.2");
                    Run_Route_Cmd("DELETE 0.0.0.0 MASK 128.0.0.0");
                    Run_Route_Cmd("DELETE 128.0.0.0 MASK 128.0.0.0");

                    InvokeExecuteJavaScript(@"$('#status_network_interfacees_msg').text('
<div class=""alert alert-danger"" role=""alert"">
Invalid Configuration.
</div>
');");
                    return;
                }
                Network_Interfaces_OK = true;
            }
            catch (Exception ex)
            {
                Debug_Services();
                Send_Log_Msg(0, LogMsg.Priority.Critical, "Exception " + ex.ToString());
            }

        }

        private void Enable_ICS()
        {


            ICS_Configured = false;
            try
            {
                DisableICS();
            }
            catch
            {

            }
            try
            {
               // setIP("ExoKey", "192.168.255.2", "255.255.255.252");
            }
            catch {

            }


            try
            {
                EnableICS(Internet_Interface.Id, Exokey_Interface.Id, true);

                
                // Internet_Interface
                //  IcsManager.ShareConnection(Internet_Interface as NETCONLib.INetConnection, Exokey_Interface as NETCONLib.INetConnection);
            }
            catch (Exception ex)
            {
                Debug_Services();
                Send_Log_Msg(0, LogMsg.Priority.Critical, "Exception " + ex.ToString());
            }
        }
        /*
        private void Check_State_Timer_Expired(object source, System.Timers.ElapsedEventArgs e)
        {


            try
            {
                if (Disposing)
                    return;
                if (Check_State_Timer_Running)
                    return;

                Check_State_Timer_Running = true;

                Check_State_Timer.Enabled = false;  // avoid timer overrun
                // Send_Log_Msg("Check_State_Timer_Expired", LogMsg.Priority.Debug);

                if (Client_USB_IP != null && XoKey_IP == null)
                {
                    Send_MCast_Announce_Msg(Client_USB_IP);
                }

                Get_VPN_Status();

                if (EK_Intf_Down_Count > 0)
                {
                    Check_Intf_Status();
                }

            }
            catch (Exception ex)
            {
                Send_Log_Msg(1, LogMsg.Priority.Error, "Check_State_Timer_Expired: Exception " + ex.Message.ToString()
                    + " : " + ex.StackTrace);

            }
            finally
            {
                if (!Disposing && Check_State_Timer != null)
                    Check_State_Timer.Enabled = true; // reenable timer

                Check_State_Timer_Running = false;
            }
        }
         */

    }
}
