using DatabaseCommunication;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessRules
{
 /*   class RemoveOverlappingRule : IBusinessRule
    {
        public List<IBusinessRule> requiredRules = new List<IBusinessRule> { new ClusterNearbyRule() };

        public List<IBusinessRule> GetRequiredRules()
        {
            return requiredRules;
        }

        public string GetName()
        {
            return this.GetType().Name;
        }

        public int GetPriority()
        {
            return 1;
        }

        public IEnumerable<Result> Apply(IEnumerable<Result> input)
        {
            var list = new List<Result>();
            foreach (var res in input)
            {
                var overlapping = false;
                foreach (var other in list)
                {
                    if (other.Overlaps(res) && other != res)
                    {
                        overlapping = true;
                    }
                }
                if (!overlapping) list.Add(res);
            }
            return list;
        }

        
    }*/
}
