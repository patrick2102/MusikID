using BusinessRules;
using System;
using System.Linq;
using Xunit;

namespace BusinesRules.tests
{
    public class RuleParserTests
    {
        [Fact]
        public void two_valid_cmds_returns_2_rules()
        {
            var res = new RuleParser().Parse("no_duplicates=true&min_accuracy=96");

            Assert.Equal(res.Count(), 2);
        }

        [Fact]
        public void one_valid_cmds_returns_1_rules()
        {
            var res = new RuleParser().Parse("no_duplicates=true&");

            Assert.Equal(res.Count(), 1);
        }

        [Fact]
        public void one_valid_cmds_returns_1_rules1()
        {
            var res = new RuleParser().Parse("&no_duplicates=true");

            Assert.Equal(res.Count(), 1);
        }
        [Fact]
        public void one_valid_cmds_returns_1_rules2()
        {
            var res = new RuleParser().Parse("no_duplicates=true");

            Assert.Equal(res.Count(), 1);
        }
        [Fact]
        public void all_filters_returns_all()
        {
            var res = new RuleParser().Parse("all_filters=true");

            Assert.Equal(res.Count(), 1);
        }
    }
}
