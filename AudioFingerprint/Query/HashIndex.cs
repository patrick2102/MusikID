using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AudioFingerprint.Query
{
    public class HashIndex
    {
        public int FingerIndex;
        public int Index;
        public uint Hash;
        public bool Variant;

        public HashIndex(int fingerIndex, int index, uint hash)
        {
            this.FingerIndex = fingerIndex;
            this.Index = index;
            this.Hash = hash;
            this.Variant = false;
        }

        public HashIndex(int fingerIndex, int index, uint hash, bool variant)
        {
            this.FingerIndex = fingerIndex;
            this.Index = index;
            this.Hash = hash;
            this.Variant = variant;
        }

        public override bool Equals(object obj)
        {
            return ((HashIndex)obj).Hash == Hash;
        }

        public override int GetHashCode()
        {
            return unchecked((int)Hash);
        }
    }
}
