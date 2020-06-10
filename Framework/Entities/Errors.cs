using System;
using System.Collections.Generic;

namespace Framework
{
    public partial class Errors
    {
        public long Id { get; set; }
        public long JobId { get; set; }
        public string ErrorMsg { get; set; }
        public DateTime LastUpdated { get; set; }
    }
}
