using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CreateElasticIndex
{
    class Program
    {
        public static void Main(string[] args)
        {
            //CreateElasticIndexSingle index = new CreateElasticIndexSingle();
            //index.IndexSingleElement("dr", 1320646);




            int id_Min = 0;
            int id_Max = 0;
            string indexName;
            if (args.Length > 0)
            {
                indexName = args[0];
                if (args.Length > 1)
                    id_Min = int.Parse(args[1]);
                if (args.Length > 2)
                {
                    id_Max = int.Parse(args[2]);
                }
            }
            else throw new Exception("needs a index name as program arguments at least");
            if (id_Max == 0)
                new CreateElasticIndex().CreateIndex(indexName, id_Min);
            else
                new CreateElasticIndex().CreateIndex(indexName, id_Min, id_Max);
        }

    }
}
