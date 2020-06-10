using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Framework.DTO
{
    public class TrackDTO
    {
        public int Id { get; set; }
        public int DrDiskoteksnr { get; set; }
        public int Sidenummer { get; set; }
        public int Sekvensnummer { get; set; }
        public DateTime DateChanged { get; set; }
        public string Reference { get; set; }
        public long Duration { get; set; }
    }
}
