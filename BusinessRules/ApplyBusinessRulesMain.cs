
using DatabaseCommunication;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;

namespace BusinessRules
{
    public class ApplyBusinessRulesMain
    {

        readonly static private string sharedPathForRadioChannels = @"\\musa01\download\ITU\MUR\RadioChannels\";

        static void Main(string[] args)
        {
            var arg = $"{RuleParser.Rules.ALL_FILTERS.ToString()}=true";

            var rules = new RuleParser().Parse(arg);

            new SQLCommunication().GetOnDemandResults(437, out List<Result> results);

            var lst = new RuleApplier(rules).ApplyRules(results);

            foreach (var song in lst)
            {
                Console.WriteLine(song);
            }
        }

        public ApplyBusinessRulesMain()
        {
            
        }

        public IEnumerable<Result> Apply(string arg, IEnumerable<Result> results)
        {
            var rules = new RuleParser().Parse(arg);

            var results_after_rules = new RuleApplier(rules).ApplyRules(results);

            return results_after_rules;
        }

        public IEnumerable<Result> ApplyTest(IEnumerable<IBusinessRule> rules)
        {
            var format = "yyyy-MM-dd HH:mm:ss";
            var provider = new CultureInfo("fr-FR");
            DateTime start = DateTime.ParseExact("2015-04-16 12:15:00", format, provider);

            DateTime end = DateTime.ParseExact("2021-04-16 12:45:00", format, provider);

            new SQLCommunication().GetOnDemandResults(244, out List<Result> lst);

            var ra = new RuleApplier(rules);

            var lst_after_rules = ra.ApplyRules(lst);

            foreach (var song in lst_after_rules)
            {
                Console.WriteLine(song);
            }

            Console.WriteLine(lst_after_rules);

            return lst_after_rules;
        }

    }
}
