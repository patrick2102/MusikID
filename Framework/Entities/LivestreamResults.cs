using System;
using System.Collections.Generic;

namespace Framework
{
    public partial class LivestreamResults
    {
        public long Id { get; set; }
        public int SongId { get; set; }
        public string ChannelId { get; set; }
        public DateTime PlayDate { get; set; }
        public DateTime LastUpdated { get; set; }
        public TimeSpan Offset { get; set; }
        public int Duration { get; set; }
        public float Accuracy { get; set; }
        public float? SongOffset { get; set; }

        public virtual Stations Channel { get; set; }
        public virtual Songs Song { get; set; }
    }
}
