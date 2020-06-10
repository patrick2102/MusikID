using System;
using System.Collections.Generic;

namespace Framework
{
    public partial class Songs
    {
        public Songs()
        {
            LivestreamResults = new HashSet<LivestreamResults>();
        }

        public int Id { get; set; }
        public int DrDiskoteksnr { get; set; }
        public int Sidenummer { get; set; }
        public int Sekvensnummer { get; set; }
        public DateTime DateChanged { get; set; }
        public string Reference { get; set; }
        public long Duration { get; set; }

        public virtual ICollection<LivestreamResults> LivestreamResults { get; set; }
    }
}
