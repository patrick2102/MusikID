using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Framework.DTO
{
    public class OnDemandResultDTO
    {
        public FileDTO file { get; set; }
        public ResultDTO[] results { get; set; }
    }
}
