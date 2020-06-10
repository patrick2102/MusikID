using DatabaseCommunication;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessRules
{
    /*
     * This rule assumes that the JoinAdjacentEqualsRule has been run before this. Not sure if it will work if that is not the case. 
     */
    class ExpandStartAndEndRule : IBusinessRule
    {
        public List<IBusinessRule> requiredRules = new List<IBusinessRule> { new ClusterNearbyRule(), new NoDuplicateRule() };

        public List<IBusinessRule> GetRequiredRules()
        {
            return requiredRules;
        }

        public int GetPriority()
        {
            return 5;
        }

        public string GetName()
        {
            return this.GetType().Name;
        }

        public IEnumerable<Result> Apply(IEnumerable<Result> input)
        {
            if (input.Count() == 0) return input;

            if (input.Count() > 1)
            {
                var arr = input.ToArray();

                var size = input.Count();

                var arr_stretched = StretchResults(arr, size - 2, arr[size - 1]);

                return arr_stretched.ToList();
            }
            else // if only one song is found, then 
            {
                var res = input.First();

                if (res.GetDuration() > 60 && res._joinedResults.Min(r => r._timeIndex)/1000 < 20) 
                {
                    var extra_sec_in_start = Math.Min(res._joinedResults.Min(r => r._timeIndex) / 1000, res.GetStartTime().TimeOfDay.TotalSeconds);

                    res._startTime = res._startTime.AddSeconds(-extra_sec_in_start);

                    var extra_sec_in_end = res._song_duration - res._joinedResults.Max(r => r._timeIndex);

                    res._endTime = res._endTime.AddSeconds(extra_sec_in_end);
                }
                return input;
            }
        }

        public Result[] StretchResults(Result[] arr, int i, Result prev)
        {
            var curr = arr[i];

            if (prev.GetDuration() > 60)
            {
               
                // fix previous start time.

                var prev_wanted_seconds = Math.Min(prev._joinedResults.Min(r => r._timeIndex)/1000, prev.GetStartTime().TimeOfDay.TotalSeconds);

                var seconds_between_curr_and_prev = prev.GetStartTime().Subtract(curr.GetEndTime()).TotalSeconds;

                var prev_extra_secs_in_start = Math.Min(seconds_between_curr_and_prev, prev_wanted_seconds);

                var prev_first = prev._joinedResults.OrderBy(r => r._startTime).First();

                if (prev._joinedResults.Min(r => r._timeIndex)/1000 < 20)
                {
                    prev_first._startTime = prev_first._startTime.AddSeconds(-prev_extra_secs_in_start);
                }

                // fix current end time.

                var seconds_to_spare = seconds_between_curr_and_prev - prev_extra_secs_in_start;

                var curr_wanted_seconds = (curr._song_duration - curr._joinedResults.Max(r => r._timeIndex)) / 1000;

                var curr_extra_secs_in_end = Math.Min(curr_wanted_seconds, seconds_to_spare);

                var curr_last = curr._joinedResults.OrderBy(r => r._endTime).First();

                curr_last._endTime = curr_last._endTime.AddSeconds(curr_extra_secs_in_end);
            }

            if (i == 0) return arr;
            else return StretchResults(arr, i - 1, curr);
        }

    }
}
