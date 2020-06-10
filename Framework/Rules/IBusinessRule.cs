using System.Collections.Generic;

namespace Framework.Rules
{
    public interface IBusinessRule
    {
        IEnumerable<Result> Apply(IEnumerable<Result> input);
        List<IBusinessRule> GetRequiredRules();
        int GetPriority();


    }
}