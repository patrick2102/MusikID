using DatabaseCommunication;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessRules
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

            foreach (var res in input)
            {
                var reference = $"{res._diskotekNr}-{res._sideNr}-{res._sequenceNr}";

                dict.TryGetValue(reference, out List<Result> val);

                if (val == null) {
                    dict.Add(reference, new List<Result> { res });
                } else 
                {
                foreach (var song in val)
                {
                    if (res.GetStartTime().Subtract(song.GetEndTime()).TotalSeconds < _maxSecDist)
                    {
                        song.UpdateValues(res);
                        break;
                    }
                    else
                    {
                        continue;
                    }
                }
                }
            }

            var list = new List<Result>();
            foreach (var templst in dict.Values)
            {
                foreach (var song in templst)
                {

                    list.Add(song);
                }
            }
            return list.OrderBy(r => r.GetStartTime());
        }
        
    }
}
