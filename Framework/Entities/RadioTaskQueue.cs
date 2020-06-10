using Framework.Entities;
using System;
using System.Collections.Generic;

namespace Framework
{
    public partial class RadioTaskQueue : ITaskQueue
    {
        public long Id { get; set; }
        public bool Started { get; set; }
        public DateTime LastUpdated { get; set; }
        public string ChannelId { get; set; }
        public string ChunkPath { get; set; }
        public long JobId { get; set; }
        public string Machine { get; set; }
    }
}
