using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Framework.Rules
{
    public class RuleApplier
    {
        Dictionary<IBusinessRule, bool> applied_rules;

        IEnumerable<IBusinessRule> _rules;

        public RuleApplier (IEnumerable<IBusinessRule> rules)
        {
            _rules = rules;
            applied_rules = new Dictionary<IBusinessRule, bool>();
        }

        public IEnumerable<Result> ApplyRules(IEnumerable<Result> lst) {

            _rules = _rules.OrderBy(r => r.GetPriority());

            foreach (var rule in _rules)
            {
                applied_rules.TryGetValue(rule, out bool contains);

             //   if (contains) continue;

               // lst = applyRequiredRules(rule.GetRequiredRules(), lst);
                
                lst = rule.Apply(lst);
                applied_rules.Add(rule, true);
            }
            return lst;
        }

        private IEnumerable<Result> applyRequiredRules(List<IBusinessRule> required_Rules, IEnumerable<Result> lst)
        {
            foreach (var rule in required_Rules)
            {
                applied_rules.TryGetValue(rule, out bool contains);

                if (!contains)
                {
                    lst = rule.Apply(lst);
                    applied_rules.Add(rule, true);
                }
            }
            return lst;
        }

        


    }
}
