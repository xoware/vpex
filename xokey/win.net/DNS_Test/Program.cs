using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Management;
using Xoware.NetUtil;

namespace DNS_Test
{
    class Programer
    {

    
        static void Main(string[] args)
        {
            
            Console.WriteLine("Startup len:"+ args.Length);
            if (args.Length < 1)
            {
                Console.WriteLine("<interface> <dhcp-server1,dhcpserver2>");
                return;
            }

            if (args[0].ToUpper().Contains("RMXOkey"))
                Xoware.NetUtil.DNS.Remove_XOkey_DNS();


            Xoware.NetUtil.DNS_Config dns_cfg = Xoware.NetUtil.DNS.Get_DNS_Config(args[0]);

            Console.WriteLine("Static=" + dns_cfg.Static.ToString());

            String[] servers = dns_cfg.Servers;

            if (servers == null)
            {
                Console.WriteLine("No match");
            }
            else
            {
                foreach (String s in servers)
                {
                    Console.WriteLine("DNS:" + s);
                }
            }
            if (args.Length == 2) {
                if (args[1].ToUpper().Contains("DHCP"))
                {
                    if (Xoware.NetUtil.DNS.Set_DHCP_Name_Servers(args[0]))
                    {
                        Console.WriteLine("DHCP SET");
                    } else
                        Console.WriteLine("DHCP FAILED");

                }
                else
                {
                    Console.WriteLine("SET DNS: " + args[1]);
                    Xoware.NetUtil.DNS.Set_Static_Name_Servers(args[0], args[1]);
                }
            } else
            {
               
                
            }  
            Console.WriteLine("Done");
            System.Threading.Thread.Sleep(1234);
        }
    }
}
