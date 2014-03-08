using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Windows.Forms;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using Xoware;


namespace XoKeyHostApp
{
    public delegate void EK_IP_Address_Detected_Handler(IPAddress ip);

    public class UdpState
    {
        public UdpClient udp_client;
        public IPEndPoint end_point;
    }

    public class XoKey : IDisposable
    {
        public event Log_Msg_Handler Log_Msg_Send_Event = null;
        public event EK_IP_Address_Detected_Handler EK_IP_Address_Detected = null;
        private readonly Object event_locker = new Object();
        private string Session_Cookie = "";
        private volatile IPAddress XoKey_IP = null; //IPAddress.Parse("192.168.255.1");
        private volatile IPAddress Client_USB_IP = IPAddress.Parse("192.168.255.2");

        System.Timers.Timer Check_State_Timer;
        IPEndPoint Server_IPEndPoint = null;
        Boolean Traffic_Routed_To_XoKey = true;
        Boolean Firewall_Opened = false;
        private volatile Boolean Disposing = false;
      //  UdpClient Mcast_UDP_Client = null;
        private BackgroundWorker startup_bw = new BackgroundWorker();

        private readonly Action<Action> gui_invoke;
        private Xoware.RoutingLib.RoutingTableRow default_route = null;
        private List<IPAddress> MCast_Listening;

        public XoKey( Action<Action> gui_invoke, Log_Msg_Handler Log_Event_Handler = null)
        {
            if (Log_Event_Handler != null)
                Log_Msg_Send_Event += Log_Event_Handler;

            Send_Log_Msg("XoKey Startup");
            this.gui_invoke = gui_invoke;
            MCast_Listening =  new List<IPAddress>();

            NetworkChange.NetworkAddressChanged += new
              NetworkAddressChangedEventHandler(AddressChangedCallback);
 
        }

        public void Startup()
        {
            startup_bw.DoWork += new DoWorkEventHandler(startup_DoWork);
            startup_bw.RunWorkerAsync();
        }
        private void startup_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;
       //   Send_Log_Msg("Worker Startup");
            Check_State_Timer = new System.Timers.Timer(1000);
            Check_State_Timer.Elapsed += new System.Timers.ElapsedEventHandler(Check_State_Timer_Expired);
            Check_State_Timer.Start();
            StartMultiCastReciever();
        }
        public void Dispose()
        {
            Disposing = true;
            Check_State_Timer.Enabled = false;
            Check_State_Timer.Dispose();
            if (Traffic_Routed_To_XoKey)
            {
                Remove_Routes();
            }
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
        public void ReceiveCallback(IAsyncResult ar)
        {
            IEnumerable<UnicastIPAddressInformation> addresses;
            UdpClient udp_client = (UdpClient)((UdpState)(ar.AsyncState)).udp_client;
            IPEndPoint end_point = (IPEndPoint)((UdpState)(ar.AsyncState)).end_point;
            const int MAX_ADDRS = 8;

    //        var args = (object[])ar.AsyncState;
       //     var session = (UdpClient)args[0];
       //     var local = (IPEndPoint)args[1];

            IPEndPoint localEp = new IPEndPoint(IPAddress.Any, 1500);
            Byte[] bytes = udp_client.EndReceive(ar, ref localEp);
            if (bytes.Length != 108){
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
                for (int i = 0 ; i < MAX_ADDRS && i < hbeat_data.Num_Addr; i++) {
                    int offset = 12 + (i * 4);
                    New_IP = IPAddress.Parse(bytes[offset] +"."+ bytes[offset+1] +"."+ bytes[offset+2] + "." + bytes[offset+3]);

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
            if (IP_Reachable && New_IP != null && !New_IP.Equals(XoKey_IP))
            {
                Send_Log_Msg(0, LogMsg.Priority.Info, "New ExoKey IP Detected" + New_IP.ToString());
                XoKey_IP = New_IP;

                Open_Firewall();

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
                if (!networkInterface.Description.Contains("XoWare"))
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
                if (!networkInterface.Description.Contains("XoWare"))
                    continue;

                IPInterfaceProperties adapterProperties = networkInterface.GetIPProperties();
                UnicastIPAddressInformationCollection unicastIPAddresses = adapterProperties.UnicastAddresses;
                IPAddress ipAddress = null;
                Send_Log_Msg("networkInterface:" + networkInterface.Description +" Name=" + networkInterface.Name);
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
        private bool Try_MCast_Bind(IPAddress Local_IP, int retries)
        {
            const int MCast_Port = 1500;
            try
            {
                IPAddress multicastaddress = IPAddress.Parse("239.255.255.255");
                UdpClient Mcast_UDP_Client = new UdpClient(AddressFamily.InterNetwork);
                Mcast_UDP_Client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                Mcast_UDP_Client.Client.Bind(new IPEndPoint(Local_IP, MCast_Port));
                Mcast_UDP_Client.JoinMulticastGroup(multicastaddress, Local_IP);
   
              

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
        private void StartMultiCastReciever()
        {
          
            try
            {
                IEnumerable<IPAddress> Local_IP_Addresses = GetLocalIpAddresses();

               foreach (IPAddress Local_IP in  Local_IP_Addresses ) {
                   bool Success = false;

                   for (int retry = 5; retry > 0  && !Success ; retry--) {
                       Success = Try_MCast_Bind(Local_IP, retry);
                   }
                   if (Success)
                   {
                       Send_Log_Msg(1, LogMsg.Priority.Debug, "Success listening to" + Local_IP.ToString());
                       MCast_Listening.Add(Local_IP);
                   }
                   else
                   {
                       if (!MCast_Listening.Contains(Local_IP))
                            Send_Log_Msg(1, LogMsg.Priority.Error, "Failed listening to" + Local_IP.ToString());
                   }
               }


            }
            catch (Exception ex)
            {
                Send_Log_Msg(1, LogMsg.Priority.Error, "MultiCastReciever ex "
                    + ex.Message.ToString());
            }

        }

        void AddressChangedCallback(object sender, EventArgs e)
        {

            NetworkInterface[] adapters = NetworkInterface.GetAllNetworkInterfaces();
            foreach (NetworkInterface n in adapters)
            {
                Send_Log_Msg(1, LogMsg.Priority.Debug,  n.Name + " : "+ n.Description + " is "+ n.OperationalStatus);

                if (n.OperationalStatus == OperationalStatus.Up && n.Description.Contains("XoWare"))
                {
                    // Needed for possible restart or plug, unplug, replug
                    StartMultiCastReciever();
                }
            }
        }

        protected virtual void Send_Log_Msg(string Log_Msg, LogMsg.Priority priority = LogMsg.Priority.Info, int code = 0)
        {
            if (Disposing)
                return;
            lock (event_locker)
            {
                if (Log_Msg_Send_Event == null)
                    return;

                LogMsg Msg = new LogMsg(Log_Msg, priority, code);
                Log_Msg_Send_Event(Msg);
            }
        }
        private void Send_Log_Msg(int code, LogMsg.Priority priority, string Log_Msg)
        {
            Send_Log_Msg(Log_Msg, priority, code);
        }

        public void Stop()
        {

            Log_Msg_Send_Event = null;

        }
        private void Ping_For_Client_IP()
        {
            WebRequest wr = WebRequest.Create("https://" + XoKey_IP.ToString() + "/api/Ping");
            wr.Method = "GET";
            WebResponse response = wr.GetResponse();
            Send_Log_Msg("Ping_For_Client_IP: status=" + ((HttpWebResponse)response).StatusDescription, LogMsg.Priority.Debug);

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
        private void Get_VPN_Status()
        {
            WebResponse response = null;
            if (Session_Cookie.Length < 3)
            {
                return;
            }
            if (XoKey_IP == null)
                return;

            HttpWebRequest wr = (HttpWebRequest)WebRequest.Create("https://" + XoKey_IP.ToString() + "/api/GetVpnStatus");
            wr.Method = "GET";
            wr.CookieContainer = new CookieContainer();
            Cookie cook = Cookie_Str_To_Cookie(Session_Cookie);
            wr.CookieContainer.Add(cook);

            try
            {
                response = wr.GetResponse();
            }
            catch
            {
                Send_Log_Msg(0, LogMsg.Priority.Error, "No connection or response from Exokey " + XoKey_IP.ToString());
                return;
            }
        //    Send_Log_Msg("GetVpnStatus: status=" + ((HttpWebResponse)response).StatusDescription, LogMsg.Priority.Debug);

            // Get the stream containing content returned by the server.
            Stream dataStream = response.GetResponseStream();
            // Open the stream using a StreamReader for easy access.
//            StreamReader reader = new StreamReader(dataStream);
            // Read the content.
//            string responseFromServer = reader.ReadToEnd();
//            Send_Log_Msg("Get_VPN_Status: responseFromServer=" + responseFromServer);

            DataContractJsonSerializer jsonSerializer = new DataContractJsonSerializer(typeof(XoKeyApi.VpnStatusResponse));
            object objResp = jsonSerializer.ReadObject(dataStream);
            XoKeyApi.VpnStatusResponse vpn_response = objResp as XoKeyApi.VpnStatusResponse;

            try {
                if (vpn_response.active_vpn != null &&  vpn_response.active_vpn.state == "Connected")
                {
                    String VPN_Server_Hostname = vpn_response.active_vpn.address[0].host;
                    IPAddress[] addresslist = Dns.GetHostAddresses(VPN_Server_Hostname);
                    IPEndPoint Server = new IPEndPoint(addresslist[0],0);
                    Set_Sever_IPEndpoint(Server);
                }
                else
                {
                    Remove_Routes();
                }

            } catch (Exception ex) {
                  Send_Log_Msg(0, LogMsg.Priority.Debug, "Ex " + ex.ToString());
            }
           
            response.Close(); // cleanup;
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
            proc = new Process();
            string NetSh_Path = Environment.GetFolderPath(Environment.SpecialFolder.SystemX86);
            proc.StartInfo.FileName = NetSh_Path + "\\netsh ";
            proc.StartInfo.Arguments = Command;
            Send_Log_Msg(proc.StartInfo.FileName + " " + Command);
            Debug.WriteLine(proc.StartInfo.FileName + " " + Command);

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
            
            Run_NetSh_Cmd("netsh advfirewall firewall delete rule Name=\"ExoKeyHost\"");

            Run_NetSh_Cmd("advfirewall firewall add rule name=\"ExoKeyHost\" dir=in action=allow program=\""
                    + System.AppDomain.CurrentDomain.FriendlyName +"\" enable=yes");

            Firewall_Opened = true;
        }
        private void Run_Route_Cmd(String Command)
        {
            System.Diagnostics.Process proc;
            proc = new Process();
            string Route_Path = Environment.GetFolderPath(Environment.SpecialFolder.SystemX86);
            proc.StartInfo.FileName = Route_Path +"\\route ";
            proc.StartInfo.Arguments = Command;
            Send_Log_Msg(proc.StartInfo.FileName + " " + Command);
            Debug.WriteLine(proc.StartInfo.FileName + " " + Command);

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
        private void NetShOutputHandler(object sendingProcess, DataReceivedEventArgs outLine)
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
        private void RouteOutputHandler(object sendingProcess, DataReceivedEventArgs outLine)
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

            if (Old_Server != null &&  default_route.GetForardNextHopIPStr().Length > 4)
                Run_Route_Cmd("DELETE " + Old_Server.Address.ToString() + " MASK 255.255.255.255 " + default_route.GetForardNextHopIPStr());

            Run_Route_Cmd("DELETE 0.0.0.0 MASK 128.0.0.0 " + XoKey_IP.ToString());
            Run_Route_Cmd("DELETE 128.0.0.0 MASK 128.0.0.0 " + XoKey_IP.ToString());
            Server_IPEndPoint = null;
            Traffic_Routed_To_XoKey = false;
        }

        private void Load_Routes()
        {
            if (Server_IPEndPoint == null || Server_IPEndPoint.Address == IPAddress.Any)
            {
                Send_Log_Msg("Load_Routes: Invalid Server IP", LogMsg.Priority.Info);
                return;
            }
            default_route = Xoware.RoutingLib.Routing.GetDefaultRoute();

      
            Run_Route_Cmd("ADD " + Server_IPEndPoint.Address.ToString() + " MASK 255.255.255.255 " + default_route.GetForardNextHopIPStr());
            Run_Route_Cmd("ADD 0.0.0.0 MASK 128.0.0.0 " + XoKey_IP.ToString());
            Run_Route_Cmd("ADD 128.0.0.0 MASK 128.0.0.0 " + XoKey_IP.ToString());

            Traffic_Routed_To_XoKey = true;
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
        /*
        private void Set_XoKey_Socks_Server()
        {
            try
            {
                Ping_For_Client_IP();

                HttpWebRequest wr = (HttpWebRequest)WebRequest.Create("https://" + XoKey_IP.ToString() + "/api/SetSocksServer?host="
                        + Client_USB_IP.ToString() + "&port=" + Properties.Settings.Default.Socks_Port);
                wr.Method = "GET";
                wr.CookieContainer = new CookieContainer();
                Cookie cook = Cookie_Str_To_Cookie(Session_Cookie);
                wr.CookieContainer.Add(cook);

                WebResponse response = wr.GetResponse();
                Send_Log_Msg(0, LogMsg.Priority.Debug, "Set_XoKey_Socks_Server: status=" + ((HttpWebResponse)response).StatusDescription);

                // Get the stream containing content returned by the server.
                Stream dataStream = response.GetResponseStream();

                DataContractJsonSerializer jsonSerializer = new DataContractJsonSerializer(typeof(XoKeyApi.RespMsg));
                object objResp = jsonSerializer.ReadObject(dataStream);
                XoKeyApi.RespMsg resp_msg = objResp as XoKeyApi.RespMsg;

                if (resp_msg.ack.status != 0)
                {
                    Send_Log_Msg(1, LogMsg.Priority.Error, "Set_XoKey_Socks_Server: Error setting SOCKS proxy");
                }
                else
                {
                    Send_Log_Msg(1, LogMsg.Priority.Debug, "Set_XoKey_Socks_Server: Set SOCKS proxy OK");
                }


                response.Close(); // cleanup;
            }
            catch (Exception ex)
            {
                Send_Log_Msg(1, LogMsg.Priority.Error, "Set_XoKey_Socks_Server: Exception setting SOCKS proxy " + ex.Message.ToString());
                throw;
            }
        }
     */
        private void Check_State_Timer_Expired(object source, System.Timers.ElapsedEventArgs e)
        {


            try
            {
                if (Disposing)
                    return;
                Check_State_Timer.Enabled = false;  // avoid timer overrun
                // Send_Log_Msg("Check_State_Timer_Expired", LogMsg.Priority.Debug);

                Get_VPN_Status();

            }
            catch (Exception ex)
            {
                Send_Log_Msg(1, LogMsg.Priority.Error, "Check_State_Timer_Expired: Exception " + ex.Message.ToString() 
                    + " : " + ex.StackTrace);

            }
            finally
            {
                if (!Disposing)
                    Check_State_Timer.Enabled = true;
            }
        }
      
    }
}
