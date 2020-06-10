using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Framework.DTO
{
    public class ResultDTO
    {
        public string start_time { get; set; }
        public string end_time { get; set; }
        
        public int duration { get; set; } 
        public string reference { get; set; }
        public string title { get; set; }
        public string artists { get; set; }
        public float accuracy { get; set; }
        public ResultDTO(DateTime StartTime, DateTime EndTime, string Reference, string Title, string Artists, float Accuracy) 
        { 
            start_time = StartTime.ToString("HH:mm:ss"); 
            end_time = EndTime.ToString("HH:mm:ss"); 
            duration = (int)EndTime.Subtract(StartTime).TotalSeconds; 
            reference = Reference; title = Title; artists = Artists;
            accuracy = Accuracy; 
        }
    }
}
