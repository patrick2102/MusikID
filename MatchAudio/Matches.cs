using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MatchAudio
{
    class Matches
    {
        private string reference;
        private int startTime;
        private int endTime;
        private int similarity;

        public Matches(string reference, int startTime, int endTime, int similarity)
        {
            this.Reference = reference;
            this.StartTime = startTime;
            this.EndTime = endTime;
            this.Similarity = similarity;
        }

        public string Reference { get => reference; set => reference = value; }
        public int StartTime { get => startTime; set => startTime = value; }
        public int EndTime { get => endTime; set => endTime = value; }
        public int Similarity { get => similarity; set => similarity = value; }

    }
}