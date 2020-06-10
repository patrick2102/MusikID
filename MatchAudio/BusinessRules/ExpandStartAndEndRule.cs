using DatabaseCommunication;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MatchAudio
{
    /*
     * This rule assumes that the JoinAdjacentEqualsRule has been run before this. Not sure if it will work if that is not the case. 
     */
    class ExpandStartAndEndRule : IBusinessRule
    {
        public IEnumerable<Result> Apply(IEnumerable<Result> input)
        {
            foreach (var res in input)
            {
                if (res.GetDuration() < 60)
                {
                    var extra_sec_in_end = res._song_duration - res._timeIndexes.Max();
                    var fewer_sec_in_start = Math.Min(res._timeIndexes.Min(), res._startTime.TimeOfDay.TotalSeconds);

                    res._startTime.AddSeconds(-fewer_sec_in_start);
                    res._endTime.AddSeconds(extra_sec_in_end);
                }
            }
            return input;
        }
    }
}
