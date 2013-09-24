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

                SocksListener server = new SocksListener(6666);
                Console.WriteLine("starting");
                server.Start();
                Console.WriteLine("done");
                Console.ReadLine();
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
