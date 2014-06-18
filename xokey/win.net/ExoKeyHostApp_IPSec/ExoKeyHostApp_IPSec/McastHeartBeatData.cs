using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace XoKeyHostApp
{
    
    [StructLayout(LayoutKind.Explicit, Size = 60, Pack = 1)]
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
        public uint Num_Addr;

        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 8)]
        [FieldOffset(12)]
        public uint []IP_Address;

        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 8)]
        [FieldOffset(44)]
        public uint []Addr_Prefix;

 
        [MarshalAs(UnmanagedType.U4)]
        [FieldOffset(76)]
        public uint Product_ID;


        [MarshalAs(UnmanagedType.U4)]
        [FieldOffset(80)]
        public uint unixtime;

        [MarshalAs(UnmanagedType.U8)]
        [FieldOffset(84)]
        public UInt64 Rand;

        [MarshalAs(UnmanagedType.U8)]
        [FieldOffset(92)]
        public UInt64 Signature0;

        [MarshalAs(UnmanagedType.U8)]
        [FieldOffset(100)]
        public UInt64 Signature1;

    }
}
