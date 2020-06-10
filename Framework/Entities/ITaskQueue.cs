using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Framework.Entities
{
    public interface ITaskQueue
    {
        long Id { get; set; }
        bool Started { get; set; }
        DateTime LastUpdated { get; set; }
        long JobId { get; set; }
        string Machine { get; set; }
    }
}
