using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseCommunication
{
    public class Job
    {

        public long id;
        public JobType type;
        public string file_id;
        public DateTime start_time, last_updated;
        public string arguments, user;
        public float percentage;

        public Job(long Id, JobType Type, string File_id, DateTime Start_time, DateTime Last_updated, string Arguments, float Percentage, string User = null )
        {
            id = Id;
            type = Type;
            start_time = Start_time;
            last_updated = Last_updated;
            arguments = Arguments;
            percentage = Percentage;
            user = User;
        }
    }
}
