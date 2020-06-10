using DatabaseCommunication;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using RadioChannel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Worker
{
    public class RadioWorker
    {
        public static void Main(string[] args)
        {
            //string IniPath = CDR.DB_Helper.FingerprintIniFile;
            //CDR.Ini.IniFile ini = new CDR.Ini.IniFile(IniPath);

            //string luceneIndexPath = @"C:\LuceneInUse";

            //string subFingerMap = ini.IniReadValue("Program", "SubFingerMap", "SubFingerLookup");

            //var subFingerLookupPath = Path.Combine(luceneIndexPath, subFingerMap);

            //var _indexSubFingerLookup = new IndexSearcher(IndexReader.Open(FSDirectory.Open(new System.IO.DirectoryInfo(subFingerLookupPath)), true));

            while (true)
            {
                new SQLCommunication().GetRadioTask(out RadioAssignment rass);

                if (rass != null)
                {
                    ExecuteRadioAssignment(rass);

                    CleanUp(rass);
                }
                else // if no new assignments where found then sleep before trying again.  Done in order to avoid too many queries.
                {
                    Thread.Sleep(200);
                }
            }
        }
        private static void ExecuteRadioAssignment(RadioAssignment ass)
        {
            new RadioMonitorWorker(ass).Start();
        }

        private static void CleanUp(RadioAssignment rass)
        {
            new SQLCommunication().DeleteRadioTask(rass.ID);
        }

    }
}
