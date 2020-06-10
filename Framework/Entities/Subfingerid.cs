using System;
using System.Collections.Generic;

namespace Framework
{
    public partial class Subfingerid
    {
        public int Id { get; set; }
        public DateTime DateChanged { get; set; }
        public DateTime DateAdded { get; set; }
        public byte[] Signature { get; set; }
    }
}
