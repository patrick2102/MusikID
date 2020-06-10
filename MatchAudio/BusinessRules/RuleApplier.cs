using DatabaseCommunication;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MatchAudio
{
    class RuleApplier
    {
        List<IBusinessRule> _rules;

        public RuleApplier (List<IBusinessRule> rules)
        {
            _rules = rules;
        }

        public IEnumerable<Result> Apply(IEnumerable<Result> lst) {
            foreach (var rule in _rules)
            {
                lst = rule.Apply(lst);
            }
            return lst;
        }
    }
}
