using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace XoKeyHostApp
{
    [StructLayout(LayoutKind.Explicit, Size = 56, Pack = 1)]
    public struct McastHeartBeatData
    {
        [MarshalAs(UnmanagedType.U4)]
        [FieldOffset(0)]
        public uint Version;

        [MarshalAs(UnmanagedType.U4)]
        [FieldOffset(4)]
        public uint Magic;

        [MarshalAs(UnmanagedType.U4)]
        [FieldOffset(8)]
        public uint IP_Address;

        [MarshalAs(UnmanagedType.U4)]
        [FieldOffset(12)]
        public uint Addr_Prefix;
    }
}
