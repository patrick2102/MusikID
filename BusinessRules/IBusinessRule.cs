using System.Collections.Generic;
using DatabaseCommunication;

namespace BusinessRules
{
    public interface IBusinessRule
    {
        IEnumerable<Result> Apply(IEnumerable<Result> input);
        List<IBusinessRule> GetRequiredRules();
        int GetPriority();


    }
}