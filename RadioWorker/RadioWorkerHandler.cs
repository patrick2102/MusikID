//using Framework;
//using Lucene.Net.Index;
//using Lucene.Net.Search;
//using Lucene.Net.Store;
//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;
//using System.Text;
//using System.Threading;
//using System.Threading.Tasks;

//namespace RadioWorker
//{
//    public class RadioWorkerHandler
//    {
//        public RadioWorkerHandler(RadioTaskQueue rass)
//        {
//            ExecuteRadioAssignment(rass);

//            CleanUp(rass);
//        }
//        private static void ExecuteRadioAssignment(RadioTaskQueue rass)
//        {
//            new RadioWorker(rass).Start();
//        }

//        private static void CleanUp(RadioTaskQueue rass)
//        {
//            new SQLCommunication().DeleteRadioTask(rass.ID);
//        }

//    }
//}
