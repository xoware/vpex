using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;

namespace STUN_Ping_Test
{
    class Program
    {
        public static LumiSoft.Net.STUN.Message.STUN_Message Test_STUN_Binding()
        {
            var ServerTupleList = new List<Tuple<string, int>>
            {
                new Tuple<string, int>("ns1.vpex.org", 3478),
                new Tuple<string, int>("ns2.vpex.org", 3478),
                new Tuple<string, int>("stun.sipgate.net", 3478), 
                new Tuple<string, int>("stun.stunprotocol.org", 3478), 
                new Tuple<string, int>("stun.l.google.com", 19302),
                new Tuple<string, int>("stun1.l.google.com", 19302),
                new Tuple<string, int>("stun2.l.google.com", 19302),
                new Tuple<string, int>("stun3.l.google.com", 19302),
                new Tuple<string, int>("stun4.l.google.com", 19302),

            };

            foreach ( Tuple<string, int>server in ServerTupleList) {

                try
                {
                    Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                    socket.Bind(new IPEndPoint(IPAddress.Any, 0));

                    LumiSoft.Net.STUN.Message.STUN_Message msg = LumiSoft.Net.STUN.Client.STUN_Client.Do_Binding_Request(server.Item1, server.Item2, socket);
                    socket.Close();
                    if (msg == null)
                    {
                        Console.WriteLine("STUN blocked: " + server.Item1);
                    }
                    else
                    {
                        Console.WriteLine("Success: " + server.Item1);
                    }
                }
                catch
                {
                    Console.WriteLine("Error: " + server.Item1);
                }
                
            }
            return null;
        }

        static void Main(string[] args)
        {


            Test_STUN_Binding();

            // Create new socket for STUN client.
            Socket socket = new Socket(AddressFamily.InterNetwork,SocketType.Dgram,ProtocolType.Udp);
            socket.Bind(new IPEndPoint(IPAddress.Any,0));

            LumiSoft.Net.STUN.Message.STUN_Message msg = LumiSoft.Net.STUN.Client.STUN_Client.Do_Binding_Request("karl.hiramoto.org", 3478, socket);

            if (msg == null)
            {
                Console.WriteLine("STUN blocked: ");
            }
            else
            {
                Console.WriteLine("Success: ");
            }

            /* 
            // Query STUN server
            LumiSoft.Net.STUN.Client.STUN_Result result = LumiSoft.Net.STUN.Client.STUN_Client.Query("ns1.vpex.org", 3478, socket);
            if (result.NetType != LumiSoft.Net.STUN.Client.STUN_NetType.UdpBlocked)
            {
                 // UDP blocked or !!!! bad STUN server
                Console.WriteLine("STUN blocked: ");
             }
             else{
                 IPEndPoint publicEP = result.PublicEndPoint;
                 Console.WriteLine("Success: ");
                 // Do your stuff
             }
             */
        }
    }
}
