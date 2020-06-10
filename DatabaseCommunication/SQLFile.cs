using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseCommunication
{
    public class SQLFile
    {
        public long id { get; set; }

        public string path { get; set; }

        public DateTime date { get; set; }

        public TimeSpan file_duration { get; set; }
    }
}
