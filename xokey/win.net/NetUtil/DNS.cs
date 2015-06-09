using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Management;

namespace Xoware.NetUtil
{

    public class DNS_Config
    {
        public bool Static; // static if true.  DHCP if false
        public String[] Servers; // Servers in their search order

        public DNS_Config()
        {
            Static = false;
            Servers = null;
        }
    }

    public class DNS
    {
        // nic string can be a name or  interface index
        public static DNS_Config Get_DNS_Config(string nic)
        {
            DNS_Config cfg = new DNS_Config();

            var proc = new System.Diagnostics.Process 
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "netsh.exe",
                    Arguments = "interface ipv4 show dnsservers name=\"" + nic + "\"" ,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };

            proc.Start();
            Console.WriteLine("Get_DNS_Info");

            while (!proc.StandardOutput.EndOfStream)
            {
                string line = proc.StandardOutput.ReadLine();
                // do something with line
                Console.WriteLine(line);
                if (line.Contains("DHCP:"))
                    cfg.Static = false;
                else if (line.Contains("Statically"))
                    cfg.Static = true;
            }

            cfg.Servers = Get_Name_Severs(nic);
                 
            return cfg;
        }

        public static void Remove_XOkey_DNS()
        {

            using (var networkConfigMng = new System.Management.ManagementClass("Win32_NetworkAdapterConfiguration"))
            {
                using (var networkConfigs = networkConfigMng.GetInstances())
                {
                    foreach (var managementObject in networkConfigs.Cast<ManagementObject>().Where(objMO => (bool)objMO["IPEnabled"]))
                    {
                        UInt32 Interface_IDX = (UInt32) managementObject["InterfaceIndex"];
                        Console.WriteLine("Desc: " + managementObject["Description"] + "  ID=" + Interface_IDX.ToString());
                         
                        String[] servers = (String[])managementObject["DNSServerSearchOrder"];

                        if (servers == null)
                            continue;

                        foreach (String s in servers)
                        {
                            // If not EK ignore
                            if (s != "192.168.137.2")
                                continue;

                            Set_DHCP_Name_Servers(Interface_IDX.ToString());

                        }

                    }
                }
            }
        }

        private static String[] Get_Name_Severs(string nic)
        {

            using (var networkConfigMng = new System.Management.ManagementClass("Win32_NetworkAdapterConfiguration"))
            {
                using (var networkConfigs = networkConfigMng.GetInstances())
                {
                    //                   foreach (var managementObject in networkConfigs.Cast<ManagementObject>().Where(objMO => (bool)objMO["IPEnabled"] && objMO["Caption"].Equals(nic)))
                    foreach (var managementObject in networkConfigs.Cast<ManagementObject>().Where(objMO => (bool)objMO["IPEnabled"] && objMO["InterfaceIndex"].ToString().Contains(nic)))
                    //                    foreach (var managementObject in networkConfigs.Cast<ManagementObject>().Where(objMO => (bool)objMO["IPEnabled"] )) 
                    {
                        Console.WriteLine("Desc: " + managementObject["Description"]);

                        String[] servers = (String[])managementObject["DNSServerSearchOrder"];
                        

                    
                        return servers;

                    }
                }
            }
            return null;
        }
        public static bool Set_DHCP_Name_Servers(string nic)
        {
            bool Sucess = true;

            var proc = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "netsh.exe",
                    Arguments = "interface ipv4 set dns name=\"" + nic + "\" source=dhcp",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };

            proc.Start();
            Console.WriteLine("Get_DNS_Info");

            while (!proc.StandardOutput.EndOfStream)
            {
                string line = proc.StandardOutput.ReadLine();
                // do something with line
                Console.WriteLine(line);
                if (line.Contains("not found"))
                    Sucess = false;

            }
            return Sucess;
        }

        public static void Set_Static_Name_Servers(UInt32 ifIndex, String dnsServers)
        {
            using (var networkConfigMng = new System.Management.ManagementClass("Win32_NetworkAdapterConfiguration"))
            {
                using (var networkConfigs = networkConfigMng.GetInstances())
                {
                    //                   foreach (var managementObject in networkConfigs.Cast<ManagementObject>().Where(objMO => (bool)objMO["IPEnabled"] && objMO["Caption"].Equals(nic)))
                    foreach (var managementObject in networkConfigs.Cast<ManagementObject>().Where(objMO => (bool)objMO["IPEnabled"] && (UInt32)objMO["InterfaceIndex"] == ifIndex))
                    //                    foreach (var managementObject in networkConfigs.Cast<ManagementObject>().Where(objMO => (bool)objMO["IPEnabled"] )) 
                    {
                        Console.WriteLine("Desc: " + managementObject["Description"]);


                        if (dnsServers.Length > 4)
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
        }

        public static void Set_Static_Name_Servers(string nic, string dnsServers)
        {
            using (var networkConfigMng = new System.Management.ManagementClass("Win32_NetworkAdapterConfiguration"))
            {
                using (var networkConfigs = networkConfigMng.GetInstances())
                {
                    //                   foreach (var managementObject in networkConfigs.Cast<ManagementObject>().Where(objMO => (bool)objMO["IPEnabled"] && objMO["Caption"].Equals(nic)))
                    foreach (var managementObject in networkConfigs.Cast<ManagementObject>().Where(objMO => (bool)objMO["IPEnabled"] && objMO["Description"].ToString().Contains(nic)))
                    //                    foreach (var managementObject in networkConfigs.Cast<ManagementObject>().Where(objMO => (bool)objMO["IPEnabled"] )) 
                    {
                        Console.WriteLine("Desc: " + managementObject["Description"]);


                        if (dnsServers.Length > 4)
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
        }

    }
}
