using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace XoKeyApi
{
    public class PingResponse
    {
 
        [DataMember(Name = "client_ip")]
        public string client_ip { get; set; }
        [DataMember(Name = "ack")]
        public MsgAck ack;
    }
}
