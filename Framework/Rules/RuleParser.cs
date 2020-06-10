using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Framework.Rules
{
    public class RuleParser
    {

        public enum Rules {NO_DUPLICATES, MIN_ACCURACY, ALL_FILTERS, CLUSTER_NEARBY, REMOVE_UNKNOWN };


        public IEnumerable<IBusinessRule> Parse(string arguments)
        {
            var list = new List<IBusinessRule>();

            var splits = arguments.Split('&');


            foreach (var cmd in splits)
            {
                var rules = ConvertFromStringToRules(cmd);
                
                foreach (var rule in rules)
                {
                    if (rule.GetType() != typeof(DefaultRule))
                    {
                        list.Add(rule);
                    }
                }
            }

            return list;
        }


        public IEnumerable<IBusinessRule> ConvertFromStringToRules(string str)
        {
            str = str.Trim();
            if (str == "") return new List<IBusinessRule> { new DefaultRule() };

            string rule = "";
            string cond = "";

            if (str.Contains("="))
            {
                var split_on_eq = str.Split('=');

                rule = split_on_eq[0].ToUpper();

                cond = split_on_eq[1].ToLower();
            }
            else
            {
                rule = str;
            }


            Enum.TryParse<Rules>(rule, out Rules ruleEnum);
            

            switch (ruleEnum)
            {
                case Rules.ALL_FILTERS:
                    /*var type = typeof(IBusinessRule);
                    var types = AppDomain.CurrentDomain.GetAssemblies()
                        .SelectMany(s => s.GetTypes())
                        .Where(p => type.IsAssignableFrom(p));

                    return types.Where(t => !t.IsInterface).Select(t => Activator.CreateInstance(t) as IBusinessRule);
                    */
                    return new List<IBusinessRule>() {
                        new ClusterNearbyRule(),
                        new DRInfoRule(),
                        new ExpandStartAndEndRule(),
                        new IgnoreAccuracyUnderThresholdRule(),
                        new IgnoreDurationUnderThresholdRule(),
                        new IgnoreUnknownRule(),
                        new NoDuplicateRule(),
                        new NoOverExtendRule()

                    };
                case Rules.NO_DUPLICATES:
                    bool val = false;
                    try
                    {
                        val = bool.Parse(cond);
                    }
                    catch (Exception e)
                    {
                        //new SQLCommunication().InsertError("couldn't parse bool value for NO_DUPLICATES", -1);
                        Console.WriteLine(e.ToString());
                        return new List<IBusinessRule> { new DefaultRule() };
                    }
                    if (val) return new List<IBusinessRule> { new NoDuplicateRule() };
                    break;
               
                case Rules.REMOVE_UNKNOWN:
                    bool val4 = false;
                    try
                    {
                        val = bool.Parse(cond);
                    }
                    catch (Exception e)
                    {
                        //new SQLCommunication().InsertError("couldn't parse bool value for REMOVE_UNKNOWN", -1);
                        return new List<IBusinessRule> { new DefaultRule() };
                    }
                    if (val) return new List<IBusinessRule> { new IgnoreUnknownRule() };
                    break;
                case Rules.CLUSTER_NEARBY:
                    bool val2 = false;
                    try
                    {
                        val = bool.Parse(cond);
                    }
                    catch (Exception e)
                    {
                     //   new SQLCommunication().InsertError("couldn't parse bool value for CLUSTER_NEARBY", -1);
                        return new List<IBusinessRule> { new DefaultRule() };
                    }
                    if (val) return new List<IBusinessRule> { new ClusterNearbyRule() };
                    break;
                case Rules.MIN_ACCURACY:
                    float m_val = 0f;
                    try
                    {
                        m_val = float.Parse(cond);
                    }
                    catch (Exception e)
                    {
                    //    new SQLCommunication().InsertError("couldn't parse float value for MIN_ACCURACY", -1);
                        return new List<IBusinessRule> { new DefaultRule() };
                    }
                    //HACK always ignore acc with default
                    return new List<IBusinessRule> { new IgnoreAccuracyUnderThresholdRule() };
                default:
                    break;
            }
            return new List<IBusinessRule> { new DefaultRule() };
        }
    }
}
