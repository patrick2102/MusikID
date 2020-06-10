using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Framework
{
    class Program
    {
        static void Main(string[] args)
        {
            //find and apply rules to a specific ID for testing purpose
            var file_id = 3363;
            var repo = new DrRepository(new drfingerprintsContext());
            var data = repo.GetODFileResultsByID(file_id, true);
            Console.WriteLine(data); 
        }
    }
}
