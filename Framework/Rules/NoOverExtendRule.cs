using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Framework.Rules
{
    public class NoOverExtendRule : IBusinessRule
    {

        public IEnumerable<Result> Apply(IEnumerable<Result> input)
        {
            foreach (var res in input)
            {
                //if the duration between start and end exceeds the _song_duration from database, then change end to be start plus DB_duration
                var durr = res._endTime.Subtract(res.GetStartTime());
                var real_duration = res._song_duration;

                if (durr.TotalMilliseconds > real_duration)
                {
                    var real_dur_rounded = Math.Round(real_duration/1000);
                    var new_end = res._startTime.Add(TimeSpan.FromSeconds(real_dur_rounded));
                    res._endTime = new_end;
                    var overextending_res = res._joinedResults.Where(r => r.GetEndTime().Ticks > new_end.Ticks);

                    //update all results that over extends
                    foreach (var item in overextending_res)
                    {
                        item._endTime = new_end;
                    }

                }
            }
            return input;
        }

        public List<IBusinessRule> requiredRules = new List<IBusinessRule> { };
        public List<IBusinessRule> GetRequiredRules()
        {
            return requiredRules;
        }

        public int GetPriority()
        {
            return 6;
        }
    }

}
