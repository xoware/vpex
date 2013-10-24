using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace XoKeyApi
{
    public class VpnStatusResponse
    {

        [DataMember(Name = "active_vpn")]
        public VpnKey active_vpn { get; set; }


        [DataMember(Name = "ack")]
        public MsgAck ack;


    }
}
