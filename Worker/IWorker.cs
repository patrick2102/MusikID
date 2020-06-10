using Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Worker
{
    public interface IWorker
    {
        void Start();
        Task<bool> ExecuteAssignment(TaskQueue tsk);
        Task<bool> CleanUp(TaskQueue tsk);
    }
}
