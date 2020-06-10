using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Framework.DTO
{
    public class FileResult
    {
        public string file_path { get; set; }

        public bool found { get; set; }

        public string reference { get; set; }
    }
}
