using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Management.Automation;
using System.Collections.ObjectModel;


namespace ICS_PowerShell_Test
{
    class Disable_ICS
    {
        /*

        private static string getPowerShellScript(String Interface_ID)
        {
            String script_pattern = @"# Register the HNetCfg library (once);
regsvr32 /s hnetcfg.dll; 
# Get sharing configuration;
$INTERFACE_ID = ""{0}"";
Write-Output ""INTF ID $INTERFACE_ID "";

";
            String script = string.Format(script_pattern, Interface_ID);

            script += @"
# Create a NetSharingManager object
$My_NetShare = New-Object -ComObject HNetCfg.HNetShare;

if($MY_NetShare.SharingInstalled){
	if($Con = $MY_NetShare.EnumEveryConnection | ? { $MY_NetShare.NetConnectionProps.Invoke($_).Guid -eq $INTERFACE_ID }){
		$Sharing_Config = $MY_NetShare.INetSharingConfigurationForINetConnection.Invoke($Con);
		if($Sharing_Config.SharingEnabled){
            Write-Host 'calling DisableSharing'
			$Sharing_Config.DisableSharing()
            Write-Host 'Done DisableSharing'
        }else{
			Write-Host 'not enabled'
        }
   	}else{
		Write-Host 'no connections'
   	}
}else{
	Write-Host 'not installed'
}
";
            return script;
        }
        public static string GetScriptTmpFile(String Interface_ID)
        {
            string myTempFile = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "XOKey-Disable-ICS.ps1");
            using (System.IO.StreamWriter sw = new System.IO.StreamWriter(myTempFile))
            {
                sw.WriteLine(getPowerShellScript(Interface_ID));
            }
            return myTempFile;
        }
        public static void DisableICS_Exec(String Interface_ID)
        {
            string myTempFile = GetScriptTmpFile(Interface_ID);


            Console.WriteLine("Tmp file " + myTempFile);
            System.Diagnostics.Process process = new System.Diagnostics.Process();
            System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
            startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
            startInfo.FileName = "PowerShell.exe";
            startInfo.Arguments = " -ExecutionPolicy UnRestricted -File " + myTempFile;
            startInfo.RedirectStandardOutput = true;
            startInfo.UseShellExecute = false;
            process.StartInfo = startInfo;
            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            Console.WriteLine("PS Output: " + output);
        }

        public static void DisableICS(String Interface_ID)
        {

            using (PowerShell PowerShellInstance = PowerShell.Create())
            {

                String script = getPowerShellScript(Interface_ID);


                PowerShellInstance.AddScript(script);
                // use "AddParameter" to add a single parameter to the last command/script on the pipeline.
                // PowerShellInstance.AddParameter("INTERFACE_ID", Interface_ID);

                // invoke execution on the pipeline (collecting output)
                Collection<PSObject> PSOutput = PowerShellInstance.Invoke();

                // check the other output streams (for example, the error stream)
                if (PowerShellInstance.Streams.Error.Count > 0)
                {
                    // error records were written to the error stream.
                    // do something with the items found.
                    Console.WriteLine("Error in DisableICS");

                }

                // loop through each output object item
                foreach (PSObject outputItem in PSOutput)
                {
                    // if null object was dumped to the pipeline during the script then a null
                    // object may be present here. check for null to prevent potential NRE.
                    if (outputItem != null)
                    {
                        //TODO: do something with the output item 
                        Console.WriteLine(outputItem.BaseObject.GetType().FullName);
                        Console.WriteLine(outputItem.BaseObject.ToString() + "\n");
                    }
                }
            }
        }
        */
        static void Main(string[] args)
        {

            // Create a UDP client, so we can figure out what interface has a route to the internet. 
            System.Net.Sockets.UdpClient udp_cli = new System.Net.Sockets.UdpClient("8.8.8.8", 53);
            System.Net.IPAddress localAddr = ((System.Net.IPEndPoint)udp_cli.Client.LocalEndPoint).Address; // This bound address is internet facing

            System.Net.NetworkInformation.NetworkInterface[] interfaces;

            interfaces = System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces();
            foreach (System.Net.NetworkInformation.NetworkInterface nic in interfaces)
            {

                if (!nic.Supports(System.Net.NetworkInformation.NetworkInterfaceComponent.IPv4))
                    continue;
                if (!(nic.NetworkInterfaceType == System.Net.NetworkInformation.NetworkInterfaceType.Ethernet
                        || nic.NetworkInterfaceType == System.Net.NetworkInformation.NetworkInterfaceType.Wireless80211
                        || nic.NetworkInterfaceType == System.Net.NetworkInformation.NetworkInterfaceType.GigabitEthernet))
                    continue;


                System.Net.NetworkInformation.IPInterfaceProperties ipProps = nic.GetIPProperties();
                System.Net.NetworkInformation.UnicastIPAddressInformationCollection uniCast = ipProps.UnicastAddresses;

                if (uniCast == null)
                {
                    // no address
                    continue;
                }
                Console.WriteLine("------------");

                Console.WriteLine("interface: " + nic.Name + "  Desc: " + nic.Description + " Status:" + nic.OperationalStatus.ToString());
                Console.WriteLine("interface ID: " + nic.Id);

                try
                {
        //            Xoware.NetUtil.ICS.DisableICS_Exec(nic.Id);
                    Xoware.NetUtil.ICS.DisableICS(nic.Id);
                } catch (Exception ex)
                {
                    Console.WriteLine("Exception" + ex.ToString());
                }

                foreach (System.Net.NetworkInformation.UnicastIPAddressInformation uni in uniCast)
                {
                    if (uni.Address.Equals(localAddr))
                    {
                        Console.WriteLine("  Internet interface: " + nic.Description + " " + nic.Id);
                    }
                }

            }
            Console.WriteLine("Done Disable_ICS");
           // Console.ReadKey();
        }
    }
}
