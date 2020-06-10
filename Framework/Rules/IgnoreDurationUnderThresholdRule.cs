
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Framework.Rules
{
    class IgnoreDurationUnderThresholdRule : IBusinessRule
    {
        const int DEFAULT_THRESHOLD = 16; //Should match chunk size

        float _threshold;

        public IgnoreDurationUnderThresholdRule()
        {
            _threshold = DEFAULT_THRESHOLD; //default value needed for reflection
        }

        public string GetName()
        {
            return this.GetType().Name;
        }

        public int GetPriority()
        {
            return 3;
        }

        public IgnoreDurationUnderThresholdRule(float threshold = DEFAULT_THRESHOLD)
        {
            _threshold = threshold;
        }

        public List<IBusinessRule> requiredRules = new List<IBusinessRule> { };

        public List<IBusinessRule> GetRequiredRules()
        {
            return requiredRules;
        }

        public IEnumerable<Result> Apply(IEnumerable<Result> input)
        {
            return input.Where(r => r.GetDuration() > _threshold);
        }
    }
}
