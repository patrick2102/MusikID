using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Framework.DTO
{
    public class LivestreamResultsDTO
    {
        public string channelName { get; set; }
        public string startTime { get; set; }
        public string endTime { get; set; }
        public List<ResultDTO> results { get; set; }

    }
}
