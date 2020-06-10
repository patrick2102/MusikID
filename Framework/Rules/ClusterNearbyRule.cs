
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Framework.Rules
{
    class ClusterNearbyRule : IBusinessRule
    {
        // does not allow same song being played at separate times. even if its far apart. will think it still the playing the same.

        float _maxSecDist = 300f;

        public List<IBusinessRule> requiredRules = new List<IBusinessRule> { };

        public List<IBusinessRule> GetRequiredRules()
        {
            return requiredRules;
        }

        public int GetPriority()
        {
            return 0;
        }

        public IEnumerable<Result> Apply(IEnumerable<Result> input)
        {
            var dict = new Dictionary<string, List<Result>>();
            var input_list = input.OrderBy(r => r.GetStartTime()).ToList();
            for (var i = 0; i < input_list.Count(); i++)
            {

                var current = input_list[i];
                Result next;
                if (current._reference == "") continue;
                if (i+1 < input_list.Count)
                    next = input_list[i + 1];
                else
                    next = current;

                if (current._reference == next._reference)
                {
                    //combine into one result and save on next place
                    next.UpdateValues(current);
                }
                else
                {
                    // add result to dict since there is no more right beside
                    dict.TryGetValue(current._reference, out List<Result> res_list);
                    if (res_list != null)  res_list.Add(current);
                    else res_list = new List<Result>{ current };
                    dict[current._reference] =  res_list;
                }
            }

            var list = new List<Result>();
            foreach (var templst in dict.Values)
            {
                for (var i = 0; i < templst.Count(); i++)
                {
                    // join results together, if not to far apart and timeIndex difference is not too high.
                    var current = templst[i];
                    Result next;
                    if (current != templst[templst.Count() - 1])
                        next = templst[i + 1];
                    else next = null;

                    if (next == null)
                    {
                        list.Add(current);
                        continue;
                    }
                    if (next.GetStartTime().Subtract(current.GetEndTime()).TotalSeconds < 24f) //if they are very close just assume they belong together
                    { 
                        next.UpdateValues(current);
                        templst[i + 1] = next;
                    }
                    else if (current.GetHighestTimeIndex() < next.GetLowestTimeIndex() && (next.GetStartTime().Subtract(current.GetEndTime()).TotalSeconds < _maxSecDist))
                    {
                        //combine: add current to next
                        next.UpdateValues(current);
                        templst[i + 1] = next;
                    }
                    else 
                    {
                        //treat as different songs.  or at end of list, add to end result list.
                        list.Add(current);
                    }
                }
            }
            return list.OrderBy(r => r.GetStartTime());
        }

    }
}
