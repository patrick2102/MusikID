using System;
using System.Collections.Generic;

namespace Framework
{
    public partial class Job
    {
        public long Id { get; set; }
        public string JobType { get; set; }
        public long? FileId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime LastUpdated { get; set; }
        public string Arguments { get; set; }
        public float Percentage { get; set; }
        public string User { get; set; }
        public string StatusMessage { get; set; }
    }
}
