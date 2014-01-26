using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using System.ComponentModel;
using System.Net;

namespace Xoware.RoutingLib
{
    [StructLayout(LayoutKind.Sequential)]
    public struct MIB_IPFORWARDROW
    {
        [MarshalAs(UnmanagedType.U4)]
        public int dwForwardDest; // IP addr of destination
        [MarshalAs(UnmanagedType.U4)]
        public int dwForwardMask; // subnetwork mask of destination
        [MarshalAs(UnmanagedType.U4)]
        public int dwForwardPolicy; // conditions for multi-path route
        [MarshalAs(UnmanagedType.U4)]
        public int dwForwardNextHop; // IP address of next hop
        [MarshalAs(UnmanagedType.U4)]
        public int dwForwardIfIndex; // index of interface
        [MarshalAs(UnmanagedType.U4)]
        public int dwForwardType; // route type
        [MarshalAs(UnmanagedType.U4)]
        public int dwForwardProto; // protocol that generated route
        [MarshalAs(UnmanagedType.U4)]
        public int dwForwardAge; // age of route
        [MarshalAs(UnmanagedType.U4)]
        public int dwForwardNextHopAS; // autonomous system number
        [MarshalAs(UnmanagedType.U4)]
        public int dwForwardMetric1; // protocol-specific metric
        [MarshalAs(UnmanagedType.U4)]
        public int dwForwardMetric2; // protocol-specific metric
        [MarshalAs(UnmanagedType.U4)]
        public int dwForwardMetric3; // protocol-specific metric
        [MarshalAs(UnmanagedType.U4)]
        public int dwForwardMetric4; // protocol-specific metric
        [MarshalAs(UnmanagedType.U4)]
        public int dwForwardMetric5; // protocol-specific metric            
    }
    public class RoutingTableRow
    {
        public MIB_IPFORWARDROW row;

        public string GetForardDestIPStr()
        {
            return Routing.IPToString(IPAddress.NetworkToHostOrder(row.dwForwardDest));
        }
        public string GetForardMaskIPStr()
        {
            return Routing.IPToString(IPAddress.NetworkToHostOrder(row.dwForwardMask));
        }
        public string GetForardNextHopIPStr()
        {
            return Routing.IPToString(IPAddress.NetworkToHostOrder(row.dwForwardNextHop));
        }
    }
    public class Routing
    {
           

        [DllImport("IpHlpApi.dll")]
        [return: MarshalAs(UnmanagedType.U4)]
        static extern int GetIpForwardTable(
                IntPtr pIpForwardTable,
                [MarshalAs(UnmanagedType.U4)]
    ref int pdwSize,
                bool bOrder);

        const int ERROR_INSUFFICIENT_BUFFER = 122;


        public static MIB_IPFORWARDROW[]  GetRouteingTable()
        {
            // The number of bytes needed.
            int bytesNeeded = 0;
            // The result from the API call.
            int result = GetIpForwardTable(IntPtr.Zero, ref bytesNeeded, false);

            // Call the function, expecting an insufficient buffer.
            if (result != ERROR_INSUFFICIENT_BUFFER)
            {
                // Throw an exception.
                throw new Win32Exception(result);
            }

            // Allocate the memory, do it in a try/finally block, to ensure
            // that it is released.
            IntPtr buffer = IntPtr.Zero;

            try
            {
                // Allocate the memory.
                buffer = Marshal.AllocCoTaskMem(bytesNeeded);

                // Make the call again.  If it did not succeed, then
                // raise an error.
                result = GetIpForwardTable(buffer, ref bytesNeeded, false);

                // If the result is not 0 (no error), then throw an exception.
                if (result != 0)
                {
                    // Throw an exception.
                    throw new Win32Exception(result);
                }

                // Now we have the buffer, we have to marshal it.  We can read
                // the first 4 bytes to get the length of the buffer.
                int entries = Marshal.ReadInt32(buffer);

                // Increment the memory pointer by the size of the int.
                IntPtr currentBuffer = new IntPtr(buffer.ToInt64() + Marshal.SizeOf(new int()));

                // Allocate an array of entries.
                MIB_IPFORWARDROW[] table = new MIB_IPFORWARDROW[entries];

                // Cycle through the entries.
                for (int index = 0; index < entries; index++)
                {
                    // Call PtrToStructure, getting the structure information.
                    table[index] = (MIB_IPFORWARDROW)Marshal.PtrToStructure(new
                    IntPtr(currentBuffer.ToInt64() + (index *
                            Marshal.SizeOf(typeof(MIB_IPFORWARDROW)))), typeof(MIB_IPFORWARDROW));
                }
                return table;
            }
            finally
            {
                // Release the memory.
                Marshal.FreeCoTaskMem(buffer);
            }

        }

        public static RoutingTableRow GetDefaultRoute()
        {
            RoutingTableRow def_route = new RoutingTableRow();
            string ip = "";
            string mask = "";
            MIB_IPFORWARDROW[] table = Routing.GetRouteingTable();

            for (int i = 0; i < table.Length; i++)
            {
                ip = IPToString(IPAddress.NetworkToHostOrder(table[i].dwForwardDest));
                mask = IPToString(IPAddress.NetworkToHostOrder(table[i].dwForwardMask));

                Console.WriteLine("Destination: {0}  Interface index: {1}  Metric: {2} Mask:{3}",
                    ip, table[i].dwForwardIfIndex, table[i].dwForwardMetric1, mask);

                if (   IPAddress.NetworkToHostOrder(table[i].dwForwardDest) == 0
                    && IPAddress.NetworkToHostOrder(table[i].dwForwardMask) == 0)
                {
                    Console.WriteLine("This is the default route!");
                    def_route.row = table[i];
                    return def_route;
                }
            }
            return null;

        }

        public static string IPToString(int ipaddr)
        {
            return String.Format("{0}.{1}.{2}.{3}",
                (ipaddr >> 24) & 0xFF, (ipaddr >> 16) & 0xFF,
                (ipaddr >> 8) & 0xFF, ipaddr & 0xFF);
        }
       
    }
}

