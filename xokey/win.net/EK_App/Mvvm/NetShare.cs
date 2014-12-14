﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NETCONLib;


namespace EK_App.Mvvm
{
    public class NetShare
    {
        public INetConnection SharedConnection;

        public INetConnection HomeConnection;

        public NetShare(INetConnection sharedConnection, INetConnection homeConnection)
        {
            SharedConnection = sharedConnection;
            HomeConnection = homeConnection;
        }

        public bool Exists
        {
            get { return (SharedConnection != null) && (HomeConnection != null); }
        }

        public override string ToString()
        {
            return string.Format("{0} -> {1}", SharedConnection, HomeConnection);
        }
    }
}
