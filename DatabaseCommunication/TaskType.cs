using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseCommunication
{
    public enum TaskType
    {    
        Fingerprint, AudioMatch,
        RollingWindow,
        StartRadioMonitoring,
        CreateLuceneIndex,
        IndexSingle,
        CheckFiles
    }
}
