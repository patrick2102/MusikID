using DatabaseCommunication;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessRules
{
    class IgnoreAccuracyUnderThresholdRule : IBusinessRule
    {
        float _threshold;
        public IgnoreAccuracyUnderThresholdRule()
        {
            _threshold = 98; //default value needed for reflection
        }

        public string GetName()
        {
            return this.GetType().Name;
        }

        public int GetPriority()
        {
            return 0;
        }

        public IgnoreAccuracyUnderThresholdRule(float threshold = 98)
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
            return input.Where(r => r.GetAccuracy() > _threshold);
        }
    }
}
