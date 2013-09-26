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

namespace XoKeyHostApp
{
    public class XoKey : IDisposable
    {
        public event Log_Msg_Handler Log_Msg_Send_Event = null;
        private readonly Object event_locker = new Object();
        private string Session_Cookie = "";
        private IPAddress XoKey_IP = IPAddress.Parse("192.168.255.1");
        private IPAddress Client_USB_IP = IPAddress.Parse("192.168.255.2");
        BackgroundWorker bw = null;
        Xoware.SocksServerLib.SocksListener Socks_Listener = null;
        System.Timers.Timer Check_State_Timer;
        IPEndPoint Server_IPEndPoint = null;
        Boolean Traffic_Routed_To_XoKey = false;
        Boolean Disposing = false;

        public XoKey(Log_Msg_Handler Log_Event_Handler = null)
        {
            if (Log_Event_Handler != null)
                Log_Msg_Send_Event += Log_Event_Handler;

            Send_Log_Msg("XoKey Startup");

            Check_State_Timer = new System.Timers.Timer(1000);
            Check_State_Timer.Elapsed += new System.Timers.ElapsedEventHandler(Check_State_Timer_Expired);
            Check_State_Timer.Start();
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
        private void RouteOutputHandler(object sendingProcess, DataReceivedEventArgs outLine)
        {
            // Collect the command output. 
      //      if (!String.IsNullOrEmpty(outLine.Data))
            {
               // numOutputLines++;

                // Add the text to the collected output.
             //   Output.Append(Environment.NewLine +  "[" + numOutputLines.ToString() + "] - " + outLine.Data);
                Send_Log_Msg("Route Output:" + outLine.Data);
            }
        }
        private void Remove_Routes()
        {

            Run_Route_Cmd("DELETE " + Server_IPEndPoint.Address.ToString() + " 0.0.0.0");
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
            Run_Route_Cmd("ADD " + Server_IPEndPoint.Address.ToString() + " 0.0.0.0");
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
            Send_Log_Msg("Starting BackgroundWorker", LogMsg.Priority.Debug);
            Ping_For_Client_IP();
            if (XoKey_IP == IPAddress.Any)
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
            if (bw == null)
            {
                bw = new BackgroundWorker();
                bw.DoWork += Background_Init_Socks;
                bw.RunWorkerAsync();
            }
        }

        private void Set_XoKey_Socks_Server()
        {
            try
            {
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
