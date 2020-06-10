using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseCommunication
{
    public class Assignment
    {
        public int ID { get; set; }
        public TaskType Type { get; set; }
        public string Arguments { get; set; }
        public int JobID { get; set; }
        public int FileID { get; set; }
    }
}
