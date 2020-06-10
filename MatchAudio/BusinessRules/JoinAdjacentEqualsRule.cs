using DatabaseCommunication;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MatchAudio
{
    class JoinAdjacentEqualsRule : IBusinessRule
    {
        // does not allow same song being played at separate times. even if its far apart. will think it still the playing the same.
        public IEnumerable<Result> Apply(IEnumerable<Result> input)
        {
            /*
            var list = new List<Result>();

            var previous = input.First();
            list.Add(previous);

            foreach (var res in input)
            {
                if (previous.Equals(res))
                {
                    previous.UpdateValues(res);
                }
                else
                {
                    list.Add(res);
                }
                previous = res;
            }
            */
            var dict = new Dictionary<string, Result>();

            foreach (var res in input)
            {
                var reference = $"{res._diskotekNr}-{res._sideNr}-{res._sequenceNr}";

                dict.TryGetValue(reference, out Result val);

                if (val == null) {
                    dict.Add(reference, res);
                } else 
                {
                    val.UpdateValues(res);
                }
            }
            return dict.Values;
        }
    }
}
