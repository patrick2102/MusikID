using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseCommunication
{
    public class Station
    {
        public string DR_ID { get; set; }
        public string channel_name { get; set; }
        public string channel_type { get; set; }
        public string streaming_url { get; set; }
        public bool running { get; set; }
}
}
