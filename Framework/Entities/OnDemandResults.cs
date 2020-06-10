using System;
using System.Collections.Generic;

namespace Framework
{
    public partial class OnDemandResults
    {
        public long Id { get; set; }
        public int SongId { get; set; }
        public DateTime LastUpdated { get; set; }
        public TimeSpan Offset { get; set; }
        public int Duration { get; set; }
        public long FileId { get; set; }
        public float Accuracy { get; set; }
        public float? SongOffset { get; set; }

        public virtual Files File { get; set; }
    }
}
