using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace XoKeyApi
{
    public class MsgAck
    {
        [DataMember(Name = "msg")]
        public string msg { get; set; }
        [DataMember(Name = "status")]
        public int status { get; set; }
    }
}
