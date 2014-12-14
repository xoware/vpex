using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Management;
using System.ServiceProcess;

namespace EK_App
{
    class WinServices
    {
        // serviceName "TapiSrv"  is telephony service
        // StartService("TapiSrv", "Manual");
        public static void StartService(String serviceName, String startMode)
        {
            String status = String.Empty;
            ServiceController[] allService = ServiceController.GetServices();
            foreach (ServiceController serviceController in allService)
            {
                if (serviceController.ServiceName.Equals(serviceName))
                {
                    status = serviceController.Status.ToString();
                    System.Diagnostics.Debug.WriteLine(serviceName + " Service status : {0}", status);
                    //Check service staus.
                    if (!serviceController.Status.Equals(ServiceControllerStatus.Running))
                    {
                        bool IsStatus = StartStoppedService(serviceName, startMode);
                        if (IsStatus)
                        {
                            serviceController.Start();
                            System.Diagnostics.Debug.WriteLine("Telephony Service is : {0}", "Started");
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("Telephony Service is : {0}", "Started");
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine(serviceName + " Service already in running state");
                    }
                }
            }
        }

        public static Boolean StartStoppedService(String serviceName, String startMode)
        {
            uint _status = 1;
            String filterService = String.Format("SELECT * FROM Win32_Service WHERE Name = '{0}'", serviceName);
            ManagementObjectSearcher _managementObjectSearcher = new ManagementObjectSearcher(filterService);
            if (_managementObjectSearcher == null)
            {
                return false;
            }
            else
            {
                try
                {
                    ManagementObjectCollection _managementObjectCollection = _managementObjectSearcher.Get();
                    foreach (ManagementObject service in _managementObjectCollection)
                    {
                        //if startup type is Manual or Disabled then change it.
                        if (Convert.ToString(service.GetPropertyValue("StartMode")) == "Manual" ||
                            Convert.ToString(service.GetPropertyValue("StartMode")) == "Disabled" ||
                            Convert.ToString(service.GetPropertyValue("StartMode")) == "Automatic")
                        {
                            ManagementBaseObject _managementBaseObject = service.GetMethodParameters("ChangeStartMode");
                            _managementBaseObject["startmode"] = startMode;
                            ManagementBaseObject outParams = service.InvokeMethod("ChangeStartMode", _managementBaseObject, null);
                            _status = Convert.ToUInt16(outParams.Properties["ReturnValue"].Value);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.Write(ex.ToString());
                }
            }
            return (_status == 0);
        }

    }
}
