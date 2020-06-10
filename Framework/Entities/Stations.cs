using System;
using System.Collections.Generic;

namespace Framework
{
    public partial class Stations
    {
        public Stations()
        {
            LivestreamResults = new HashSet<LivestreamResults>();
        }

        public string DrId { get; set; }
        public string ChannelName { get; set; }
        public string ChannelType { get; set; }
        public string StreamingUrl { get; set; }
        public bool? Running { get; set; }

        public virtual ICollection<LivestreamResults> LivestreamResults { get; set; }
    }
}
