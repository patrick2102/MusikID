using DatabaseCommunication;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessRules
{
    class NoDuplicateRule : IBusinessRule
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
            return 3;
        }
        /*
        public IEnumerable<Result> Apply(IEnumerable<Result> input)
        {
            var list = new List<Result>();
            foreach (var res in input)
            {
                var overlapping = false;
                foreach (var other in list)
                {
                    if (other.Overlaps(res) && other != res) //check if the clusters are overlapping
                    {
                        if (chunkCollides(res._joinedResults, other._joinedResults)) //check if any of the chunks are colliding 
                        {
                            overlapping = true;
                        }
                    }
                }
                if (!overlapping) list.Add(res);
            }
            return list;
        }*/

        public IEnumerable<Result> Apply(IEnumerable<Result> input)
        {
            var groups = new List<HashSet<Result>>();

            // create groups
            foreach (var res in input)
            {
                bool overlaps = false;
                foreach (var group in groups)
                {
                    foreach (var other in group) // TODO: Maybe this should not check all other in the group, but just the "primary" one (first element?).
                    {
                        if (other.Overlaps(res)) //check if the clusters are overlapping
                        {
                            group.Add(res);
                            overlaps = true;
                            break;
                        }
                    }
                    if (overlaps) break;
                }
                if (!overlaps) groups.Add(new HashSet<Result> { res }); 
            }

            // create new list based on groups.

            var list = new List<Result>();

            foreach (var group in groups)
            {
                var longest_dur = group.OrderByDescending(g => g.GetDuration()).First();

                longest_dur._reference = longest_dur.primary_src;

                list.Add(longest_dur);
            }

            return list;
        }

        public bool chunkCollides(List<Result> l1, List<Result> l2)
        {
            bool collides = false;

            foreach (var res in l1)
            {
                foreach (var res2 in l2)
                {
                    if (res.Overlaps(res2))
                    {
                        collides = true;
                    }
                }
            }

            return true;
        }


    }
}
