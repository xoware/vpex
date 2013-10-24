using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace XoKeyApi
{
    public class RespMsg
    {
        [DataMember(Name = "ack")]
        public MsgAck ack;
    }
}
