using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Framework.DTO
{
    public class CheckFilesResult
    {
        public int file_count { get; set; }
        public int file_completed_count { get; set; }

        public List<FileResult> file_results { get; set; }
    }
}
