using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace XoKeyApi
{
    public class HostPortAddress
    {
        [DataMember(Name = "host")]
        public string host { get; set; }
        [DataMember(Name = "port")]
        public int port { get; set; }
    }
}
