using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Framework.DTO
{
    public class FileDTO
    {
        public long id { get; set; }
        public string created { get; set; }
        public bool job_finished { get; set; }
        public float percentage { get; set; }
        public string time_used { get; set; }
        public string estimated_time_of_completion { get; set; }
        public string file_duration { get; set; }
        public string file_path { get; set; }
        public string user { get; set; }
        public string file_ext { get; set; }

        public string job_type { get; set; }

        public long jobId { get; set; }

        public string last_updated { get; set; }
    }
}
