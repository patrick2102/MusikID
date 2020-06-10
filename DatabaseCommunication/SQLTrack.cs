using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace DatabaseCommunication
{
    public class SQLTrack
    {
        public long id { get; set; }

        public int dr_diskoteksnr { get; set; }

        public int sidenummer { get; set; }

        public int sekvensnummer { get; set; }

        public DateTime date_changed { get; set; }

        public string reference { get; set; }

        public long duration { get; set; }
        
    }
}
