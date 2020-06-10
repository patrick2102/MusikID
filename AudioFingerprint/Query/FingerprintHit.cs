using AudioFingerprint.Audio;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AudioFingerprint.Query
{
    public class FingerprintHit
    {
        public FingerprintSignature Fingerprint;
        public int TimeIndex;
        public int BER;
        public int IndexNumberInMatchList;
        public int SubFingerCountHitInFingerprint;
        public SearchStrategy SearchStrategy;
        public int SearchIteration;


        public FingerprintHit(FingerprintSignature fingerprint, int timeIndex, int BER, int indexNumberInMatchList, int subFingerCountHitInFingerprint, SearchStrategy searchStrategy, int searchIteration)
        {
            this.Fingerprint = fingerprint;
            this.TimeIndex = timeIndex;
            this.BER = BER;
            this.IndexNumberInMatchList = indexNumberInMatchList;
            this.SubFingerCountHitInFingerprint = subFingerCountHitInFingerprint;
            this.SearchStrategy = searchStrategy;
            this.SearchIteration = searchIteration;
        }
    }
}
