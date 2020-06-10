using System;
using System.Collections.Generic;

namespace Framework
{
    public partial class FingerTaskQueue
    {
        public long Id { get; set; }
        public bool Started { get; set; }
        public DateTime LastUpdated { get; set; }
        public string TaskType { get; set; }
        public string Arguments { get; set; }
        public long JobId { get; set; }
        public string Machine { get; set; }
    }
}
