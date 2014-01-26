using System;

namespace XoKeyHostApp
{
    internal delegate T2 GenericDelegate<T1, T2>(T1 param);
    internal delegate T3 GenericDelegate<T1, T2, T3>(ref T1 param1, ref T2 param2);
}
