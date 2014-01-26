using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace XoKeyApi
{
    public class VpnKey
    {
        [DataMember(Name = "path")]
        public string path { get; set; }

        [DataMember(Name = "id")]
        public int id { get; set; }

        [DataMember(Name = "name")]
        public string name { get; set; }

        [DataMember(Name = "last_use")]
        public string last_use { get; set; }

        [DataMember(Name = "state")]
        public string state { get; set; }

        [DataMember(Name = "address")]
        public HostPortAddress[] address { get; set; }
    }
}
