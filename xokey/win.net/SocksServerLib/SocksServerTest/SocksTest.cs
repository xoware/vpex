using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xoware.SocksServerLib;

namespace SocksTest
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("create listener");

            try
            {
                ConsoleKeyInfo key;
                SocksListener server = new SocksListener(6666);
                Console.WriteLine("starting");
                server.Start();

                do
                {
                    System.Threading.Thread.Sleep(10);
                    Console.WriteLine("Status (Press Key to sample or Q to quit):");
                    int Num_Clients = server.GetClientCount();
                    Console.WriteLine("Check_Socks_Clients Num_Clients=" + Num_Clients);

                    for (int i = 0; i < Num_Clients; i++)
                    {
                        Xoware.SocksServerLib.Client client = server.GetClientAt(i);
                        Console.WriteLine("client i=" + i + " " + client.ToString());
                        Xoware.SocksServerLib.SocksClient sc = (Xoware.SocksServerLib.SocksClient) client;
                        if (sc != null)
                        {
                            Console.WriteLine("client i=" + i + " Server: " + sc.GetSeverRemoteEndpoint().ToString()
                                + " Client " + sc.GetClientRemoteEndpoint().ToString());

                        }
                    }

                    key = Console.ReadKey();
                } while (key.KeyChar != 'q' && key.KeyChar != 'Q');
            //    Console.ReadLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Unhandeled Exception");
                Console.WriteLine(ex.ToString());
                Console.WriteLine(ex.StackTrace.ToString());
                Console.ReadLine();
            }
        }
    }
}
