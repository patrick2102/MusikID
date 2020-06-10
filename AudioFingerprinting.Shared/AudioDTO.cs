using System;

namespace AudioFingerprinting.Shared
{
    public class AudioDTO
    {
        public string Name { get; set; }

        public string Extension { get; set; }

        public byte[] Audio { get; set; }

        public int DiskotekNr { get; set; }

        public int SideNr { get; set; }

        public int SequenceNr { get; set; }
    }
}
