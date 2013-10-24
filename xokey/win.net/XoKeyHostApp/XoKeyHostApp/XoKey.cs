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

        Xoware.SocksServerLib.SocksListener Socks_Listener = null;
        System.Timers.Timer Check_State_Timer;
        IPEndPoint Server_IPEndPoint = null;
        Boolean Traffic_Routed_To_XoKey = false;
        private volatile Boolean Disposing = false;
      //  UdpClient Mcast_UDP_Client = null;
        private BackgroundWorker startup_bw = new BackgroundWorker();
        private BackgroundWorker socks_bw = new BackgroundWorker();
        private readonly Action<Action> gui_invoke;
        private Xoware.RoutingLib.RoutingTableRow default_route = null;

        public XoKey( Action<Action> gui_invoke, Log_Msg_Handler Log_Event_Handler = null)
        {
            if (Log_Event_Handler != null)
                Log_Msg_Send_Event += Log_Event_Handler;

            Send_Log_Msg("XoKey Startup");
            this.gui_invoke = gui_invoke;

 
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
            UdpClient udp_client = (UdpClient)((UdpState)(ar.AsyncState)).udp_client;
            IPEndPoint end_point = (IPEndPoint)((UdpState)(ar.AsyncState)).end_point;

    //        var args = (object[])ar.AsyncState;
       //     var session = (UdpClient)args[0];
       //     var local = (IPEndPoint)args[1];

            IPEndPoint localEp = new IPEndPoint(IPAddress.Any, 1500);
            Byte[] bytes = udp_client.EndReceive(ar, ref localEp);
            if (bytes.Length != 16) {
                // Invalid packet
                goto queue_next;
            }

            McastHeartBeatData hbeat_data = ByteArr2McastHeartBeatData(bytes);

            if (hbeat_data.Magic != 0xDEADBEEF)
            {
                // Invalid packet
                goto queue_next;
            }

            IPAddress New_IP = IPAddress.Parse(bytes[8] +"."+ bytes[9] +"."+ bytes[10] + "." + bytes[11]);
          //  Console.WriteLine("Received: {0}", bytes);
            if (!New_IP.Equals(XoKey_IP))
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

                //FIXME   Only get IP addres of USB ethernet  (Exo Key IP)


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
        private void StartMultiCastReciever()
        {
            const int MCast_Port = 1500;
            try
            {

                IEnumerable<IPAddress> Local_IP_Addresses = GetLocalIpAddresses();
                IPAddress multicastaddress = IPAddress.Parse("239.255.255.255");

               foreach (IPAddress Local_IP in  Local_IP_Addresses ) {

                    UdpClient  Mcast_UDP_Client = new UdpClient(AddressFamily.InterNetwork);
                    Mcast_UDP_Client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                    Mcast_UDP_Client.Client.Bind(new IPEndPoint(Local_IP, MCast_Port));
                    Mcast_UDP_Client.JoinMulticastGroup(multicastaddress, Local_IP);


                    UdpState state = new UdpState();
                    state.udp_client = Mcast_UDP_Client;
                    state.end_point = new IPEndPoint(Local_IP, MCast_Port);

                    Mcast_UDP_Client.BeginReceive(new AsyncCallback(ReceiveCallback), state);
               }


            }
            catch (Exception ex)
            {
                Send_Log_Msg(1, LogMsg.Priority.Error, "MultiCastReciever ex "
                    + ex.Message.ToString());
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
            if (Session_Cookie.Length < 3)
            {
                return;
            }


            HttpWebRequest wr = (HttpWebRequest)WebRequest.Create("https://" + XoKey_IP.ToString() + "/api/GetVpnStatus");
            wr.Method = "GET";
            wr.CookieContainer = new CookieContainer();
            Cookie cook = Cookie_Str_To_Cookie(Session_Cookie);
            wr.CookieContainer.Add(cook);

            WebResponse response = wr.GetResponse();
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
            Run_NetSh_Cmd("advfirewall firewall add rule name=\"ExoKeyHost\" dir=in action=allow program=\""
                    + System.AppDomain.CurrentDomain.FriendlyName +"\" enable=yes");
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

            if (Old_Server != null &&  default_route.GetForardNextHopIPStr().Length > 4)
                Run_Route_Cmd("DELETE " + Old_Server.Address.ToString() + " MASK 255.255.255.255 " + default_route.GetForardNextHopIPStr());

            Run_Route_Cmd("DELETE 0.0.0.0 MASK 128.0.0.0 " + XoKey_IP.ToString());
            Run_Route_Cmd("DELETE 128.0.0.0 MASK 128.0.0.0 " + XoKey_IP.ToString());

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
        private void Background_Init_Socks(object sender, DoWorkEventArgs e)
        {
            Send_Log_Msg("Starting Background_Init_Socks", LogMsg.Priority.Debug);
           
            if (XoKey_IP == null &&  XoKey_IP == IPAddress.Any)
            {
                Send_Log_Msg(1, LogMsg.Priority.Error, "IP address not detected");
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
                        Send_Log_Msg(1, LogMsg.Priority.Error, "Port " + port + " NOT AVAILABLE");
                    }
                }
            }
            catch (Exception ex)
            {
                Send_Log_Msg(1, LogMsg.Priority.Error, "Unable to start SOCKS server on Port "
                    + Properties.Settings.Default.Socks_Port + " Addr:" + XoKey_IP.ToString()
                    + "   "+ ex.Message.ToString());
            }


        }
        public void Set_Session_Cookie(String Cookie)
        {
            if (Cookie == Session_Cookie)
                return;

            Session_Cookie = Cookie;

 
            socks_bw.DoWork += Background_Init_Socks;
            socks_bw.RunWorkerAsync();

        }

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
        private void Check_Socks_Clients()
        {
            if (Socks_Listener == null)
                return;

            
            int Num_Clients = Socks_Listener.GetClientCount();
         //   Send_Log_Msg("Check_Socks_Clients Num_Clients=" + Num_Clients, LogMsg.Priority.Debug);

            if (Num_Clients == 0 && Server_IPEndPoint != null)
            {
                // No current connection be we were connected before.

                Remove_Routes(Server_IPEndPoint);

                Server_IPEndPoint = null;
            }

            for (int i = 0; i < Num_Clients; i++) {
                Xoware.SocksServerLib.Client client = Socks_Listener.GetClientAt(i);
                Xoware.SocksServerLib.SocksClient sc = (Xoware.SocksServerLib.SocksClient)client;
                IPEndPoint ServerEP = sc.GetSeverRemoteEndpoint();
                if (ServerEP == null)
                    continue; // No socks endpoint
                if (Server_IPEndPoint == null || !Server_IPEndPoint.Equals(ServerEP)
                    && !Server_IPEndPoint.Address.Equals(ServerEP.Address))
                {
                    Send_Log_Msg("client i=" + i + " " + client.ToString()
                        + " Server: " + sc.GetSeverRemoteEndpoint().ToString()
                        + " Client " + sc.GetClientRemoteEndpoint().ToString()
                        , LogMsg.Priority.Debug);
                    Set_Sever_IPEndpoint(ServerEP);
                }

                
          
            }

           

        }
        private void Check_State_Timer_Expired(object source, System.Timers.ElapsedEventArgs e)
        {


            try
            {
                Check_State_Timer.Enabled = false;  // avoid timer overrun
                // Send_Log_Msg("Check_State_Timer_Expired", LogMsg.Priority.Debug);

                Get_VPN_Status();

                Check_Socks_Clients();
            }
            catch (Exception ex)
            {
                Send_Log_Msg(1, LogMsg.Priority.Error, "Check_State_Timer_Expired: Exception " + ex.Message.ToString() 
                    + " : " + ex.StackTrace);

            }
            finally
            {
                Check_State_Timer.Enabled = true;
            }
        }
      
    }
}
