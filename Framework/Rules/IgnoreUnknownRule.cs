
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Framework.Rules
{
    class IgnoreUnknownRule : IBusinessRule
    {
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
            return input.Where(r => r._diskotekNr != -1);
        }
    }
}
