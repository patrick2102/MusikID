using DatabaseCommunication;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessRules
{
    /*
     * This rule assumes that the JoinAdjacentEqualsRule has been run before this. Not sure how it will work else. 
     */
    class DefaultRule : IBusinessRule
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
            return input;
        }
    }
}
