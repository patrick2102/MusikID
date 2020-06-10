#region License
// Copyright (c) 2015-2017 Stichting Centrale Discotheek Rotterdam.
// 
// website: https://www.muziekweb.nl
// e-mail:  info@muziekweb.nl
//
// This code is under MIT licence, you can find the complete file here: 
// LICENSE.MIT
#endregion
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AcoustID;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Store;
using MySql.Data.MySqlClient;
using System.IO;
using System.Reflection;
using AudioFingerprint.Audio;
using Directory = System.IO.Directory;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using DatabaseCommunication;

namespace CreateInversedFingerprintIndex
{
    public class InversedWorker
    {
        private HttpClient client;
        private string[] radioIDs;

        private void CreateAndInstanzialiseClient()
        {
            client = new HttpClient();
            client.BaseAddress = new Uri("http://ITUMUR02:8080");
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(
              new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public void InitializeInversedID(bool isPriority)
        {
            new SQLCommunication().InsertJob(JobType.CreateLuceneIndex.ToString(),DateTime.Now,0,-1,$"machine : {Environment.MachineName}", out int jobID); 
            try
            {
                CreateAndInstanzialiseClient();
            } catch (Exception e)
            {
                using (System.IO.StreamWriter file = new System.IO.StreamWriter(@"UpdateLuceneFailureFuck.txt"))
                {
                    file.WriteLine(e.ToString());
                }
                new SQLCommunication().InsertError(e.ToString(), jobID);
                return;
            }
            string IniPath = CDR.DB_Helper.FingerprintIniFile;
            CDR.Ini.IniFile ini = new CDR.Ini.IniFile(IniPath);

            //string luceneIndexPath = @"\\musa01\Download\ITU\MUR\Lucene\LuceneCopy";
            string luceneIndexPath = @"C:\LuceneInUse";
            string acoustIDFingerMap = ini.IniReadValue("Program", "AcoustIDFingerMap", "AcoustIDFingerMap");
            string subFingerMap = ini.IniReadValue("Program", "SubFingerMap", "SubFingerLookup");

            BassLifetimeManager.bass_EMail = ini.IniReadValue("BASS", "bass_EMail", "");
            BassLifetimeManager.bass_RegistrationKey = ini.IniReadValue("BASS", "bass_RegistrationKey", "");


            AudioFingerprint.Math.SimilarityUtility.InitSimilarityUtility();

           // CreateAcoustIDFingerLookupIndex(Path.Combine(luceneIndexPath, acoustIDFingerMap));
            CreateSubFingerLookupIndex(Path.Combine(luceneIndexPath, subFingerMap),jobID, isPriority);

            new SQLCommunication().UpdateJob(jobID, 100);
        }

        #region Create SubFinger Lucene Database functions

        public void ClearFolder(string lucenePath)
        {
            System.IO.DirectoryInfo di = new DirectoryInfo(lucenePath);

            foreach (FileInfo file in di.GetFiles())
            {
                file.Delete();
            }
            foreach (DirectoryInfo dir in di.GetDirectories())
            {
                dir.Delete(true);
            }
        }

public bool CreateSubFingerLookupIndex(string luceneIndexPath, int jobID, bool isPriority)
        {
            DateTime startTime = DateTime.Now;
            DateTime endTime = startTime;
            Console.WriteLine("Creating SubFingerLookup index.");

            if (!isPriority)
                ClearFolder(luceneIndexPath);

            int minID;
            int maxID;
            if (!Exec_MySQL_MinAndMax_IDS(out minID, out maxID) && minID >= 1)
            {
                return false;
            }

            if (!System.IO.Directory.Exists(luceneIndexPath))
            {
                System.IO.Directory.CreateDirectory(luceneIndexPath);
            }


            Lucene.Net.Store.Directory directory = FSDirectory.Open(new System.IO.DirectoryInfo(luceneIndexPath));
            IndexWriter iw = null;
            int fingerCount = 0;
            try
            {
                //HACK
                iw = new IndexWriter(directory, new StandardAnalyzer(Lucene.Net.Util.Version.LUCENE_30), false, IndexWriter.MaxFieldLength.UNLIMITED);
                iw.UseCompoundFile = false;
                iw.SetSimilarity(new CDR.Indexer.DefaultSimilarityExtended());
                iw.MergeFactor = 10; // default = 10
                iw.SetRAMBufferSizeMB(512 * 3);                                   // use memory to do a flush
                iw.SetMaxBufferedDocs(IndexWriter.DISABLE_AUTO_FLUSH);            // only use memory as trigger to do a flush
                iw.SetMaxBufferedDeleteTerms(IndexWriter.DISABLE_AUTO_FLUSH);     // only use memory as trigger to do a flush

                Document doc = new Document();
                doc.Add(new Field("FINGERID", "", Field.Store.YES, Field.Index.NOT_ANALYZED));
                doc.Add(new Field("SUBFINGER", "", Field.Store.YES, Field.Index.ANALYZED));

                Field fFingerID = doc.GetField("FINGERID");
                fFingerID.OmitNorms = true;
                fFingerID.OmitTermFreqAndPositions = true;

                Field fSubFinger = doc.GetField("SUBFINGER");
                fSubFinger.OmitNorms = true;
                fSubFinger.OmitTermFreqAndPositions = true;

                StringBuilder sb = new StringBuilder(256 * 1024);

                int start = minID;
                int count = 5000;
                while (start <= maxID)
                {
                    DataTable dt;
                    //HACK
                    //if (Exec_MySQL_LOADSUBFINGERIDS(start, (start + count - 1), out dt))
                    if (Exec_MySQL_LOADSUBFINGERIDS_NEW(start, (start + count - 1), out dt))
                    {
                        foreach (DataRow row in dt.Rows)
                        {
                            fingerCount++;
                            if ((fingerCount % 100) == 0 || fingerCount <= 1)
                            {
                                Console.Write("\rIndexing subfingerprint #" + fingerCount.ToString());
                                float percentage = (((float)fingerCount) / maxID) * 100;
                                new SQLCommunication().UpdateJob(jobID, percentage);
                            }

                            FingerprintSignature fingerprint = new FingerprintSignature((string)row["REFERENCE"], Convert.ToInt64(row["ID"]), (byte[])row["SIGNATURE"], Convert.ToInt64(row["DURATION"]));

                            sb.Clear();
                            for (int i = 0; i < fingerprint.SubFingerprintCount; i++)
                            {
                                uint subFingerValue = fingerprint.SubFingerprint(i);
                                int bits = AudioFingerprint.Math.SimilarityUtility.HammingDistance(subFingerValue, 0);
                                if (bits < 10 || bits > 22) // 5 27
                                {
                                    continue;
                                }
                                sb.Append(subFingerValue.ToString());
                                sb.Append(' ');
                            }
                            fFingerID.SetValue(row["ID"].ToString());
                            fSubFinger.SetValue(sb.ToString());

                            iw.AddDocument(doc);
                        } //foreach
                        Console.Write("\rIndexing subfingerprint #" + fingerCount.ToString());

                        start += count;
                    } //if
                    else
                    {
                        if (!RetryDatabaseError())
                        {
                            return false;
                        }
                    }
                } // while all fingerprints
            }
            finally
            {
                Console.WriteLine();
                Console.WriteLine("Finalizing");
                if (iw != null)
                {
                    // Optimizes the index
                    Console.WriteLine("Commiting Start: " + DateTime.Now);
                    iw.Commit();
                    Console.WriteLine("Optimizing Start: " + DateTime.Now);
                 //   iw.Optimize(1, true);
                    Console.WriteLine("Disposing Start: " + DateTime.Now);
                    iw.Dispose();
                    iw = null;
                    GC.WaitForPendingFinalizers();
                }
                try
                {
                    try
                    {
                        if(isPriority == true)
                        {
                       new ServerRadioHandler();
                        }
                    }
                    catch
                    {
                        Console.WriteLine("Something went wrong in the moving process...");
                    }
                }
                catch
                {
                    Console.WriteLine("Directory not found");
                }
            }


            endTime = DateTime.Now;
            TimeSpan ts = (endTime - startTime);
            Console.WriteLine(String.Format("Elapsed index time {0:00}:{1:00}:{2:00}.{3:000}", ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds));
            Console.WriteLine();

            return true;
        }

        /// <summary>
        /// Create SubFinger fingerprint database where all fingerprint are stored. (just like the database)
        /// </summary>
        //public bool CreateSubFingerDatabase(string luceneDBPath)
        //{
        //    DateTime startTime = DateTime.Now;
        //    DateTime endTime = startTime;
        //    Console.WriteLine("Creating SubFingerprint Database.");

        //    int minID;
        //    int maxID;
        //    if (!Exec_MySQL_MinAndMax_IDS(out minID, out maxID) && minID >= 1)
        //    {
        //        return false;
        //    }

        //    if (!System.IO.Directory.Exists(luceneDBPath))
        //    {
        //        System.IO.Directory.CreateDirectory(luceneDBPath);
        //    }


        //    Lucene.Net.Store.Directory directory = FSDirectory.Open(new System.IO.DirectoryInfo(luceneDBPath));
        //    IndexWriter iw = null;
        //    try
        //    {
        //        PerFieldAnalyzerWrapper analyzerWrapper = new PerFieldAnalyzerWrapper(new StandardAnalyzer(Lucene.Net.Util.Version.LUCENE_30));
        //        analyzerWrapper.AddAnalyzer("FINGERID", new KeywordAnalyzer());
        //        analyzerWrapper.AddAnalyzer("REFERENCE", new KeywordAnalyzer());
        //        analyzerWrapper.AddAnalyzer("DURATION", new KeywordAnalyzer());
        //        analyzerWrapper.AddAnalyzer("AUDIO_FORMAT", new KeywordAnalyzer());
        //        analyzerWrapper.AddAnalyzer("FINGERPRINT", new KeywordAnalyzer());

        //        //HACK
        //        iw = new ThreadedIndexWriter(directory, analyzerWrapper, false, IndexWriter.MaxFieldLength.UNLIMITED);
        //        iw.UseCompoundFile = false;
        //        iw.SetSimilarity(new CDR.Indexer.DefaultSimilarityExtended());
        //        iw.MergeFactor = 10000; // default = 10
        //        iw.SetRAMBufferSizeMB(2000);                                      // use memory to do a flush
        //        iw.SetMaxBufferedDocs(IndexWriter.DISABLE_AUTO_FLUSH);            // only use memory as trigger to do a flush
        //        iw.SetMaxBufferedDeleteTerms(IndexWriter.DISABLE_AUTO_FLUSH);     // only use memory as trigger to do a flush

        //        int fingerCount = 0;
        //        int start = minID;
        //        int count = 5000;
        //        while (start <= maxID)
        //        {
        //            DataTable dt;
        //            if (Exec_MySQL_LOADSUBFINGERIDS(start, (start + count - 1), out dt))
        //            {
        //                foreach (DataRow row in dt.Rows)
        //                {
        //                    fingerCount++;
        //                    if ((fingerCount % 100) == 0 || fingerCount <= 1)
        //                    {
        //                        Console.Write("\rIndexing subfingerprint #" + fingerCount.ToString());
        //                    }

        //                    Document doc = new Document();
        //                    doc.Add(new Field("FINGERID", row["ID"].ToString(), Field.Store.YES, Field.Index.ANALYZED));
        //                    doc.Add(new Field("REFERENCE", row["REFERENCE"].ToString(), Field.Store.YES, Field.Index.ANALYZED));
        //                    doc.Add(new Field("DURATION", row["DURATION"].ToString(), Field.Store.YES, Field.Index.NO));
        //                    doc.Add(new Field("AUDIO_FORMAT", row["AUDIO_FORMAT"].ToString(), Field.Store.YES, Field.Index.NO));
        //                    doc.Add(new Field("FINGERPRINT", (byte[])row["SIGNATURE"], Field.Store.YES));

        //                    iw.AddDocument(doc);
        //                } //foreach
        //                Console.Write("\rIndexing subfingerprint #" + fingerCount.ToString());
        //                start += count;
        //            } //if
        //            else
        //            {
        //                if (!RetryDatabaseError())
        //                {
        //                    return false;
        //                }
        //            }
        //        } // while alle fingerprints
        //    }
        //    finally
        //    {
        //        Console.WriteLine();
        //        Console.WriteLine("Optimizing.");
        //        if (iw != null)
        //        {
        //            // Optimize index
        //            iw.Commit();
        //            //iw.Optimize(1, true);
        //            iw.Dispose();
        //            iw = null;
        //            GC.WaitForPendingFinalizers();
        //        }
        //    }

        //    endTime = DateTime.Now;
        //    TimeSpan ts = (endTime - startTime);
        //    Console.WriteLine(String.Format("Elapsed index time {0:00}:{1:00}:{2:00}.{3:000}", ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds));
        //    Console.WriteLine();

        //    return true;
        //}

        #endregion

        private bool RetryDatabaseError()
        {
            Console.WriteLine();
            Console.Write("Database error: Retry (Y/N)? ");
            ConsoleKeyInfo key = Console.ReadKey(true);
            if (char.ToUpper(key.KeyChar) == 'Y')
            {
                Console.WriteLine("Y");
                return true;
            }

            Console.WriteLine("N");
            Console.WriteLine("Stopping.");
            return false;
        }

        #region MySQL

        public static bool Exec_MySQL_MinAndMax_IDS(out int minID, out int maxID)
        {
            minID = -1;
            maxID = -1;
            // nu zorgen dat computer naam in de database komt en wij deze als ID
            // in deze class opslaan
            try
            {
                using (MySqlConnection conn = CDR.DB_Helper.NewMySQLConnection())
                {
                    MySqlCommand command = new MySqlCommand();
                    command.CommandType = CommandType.Text;
                    command.CommandText = "SELECT MIN(ID) AS MIN_ID,\r\n" +
                                          "       MAX(ID) AS MAX_ID\r\n" +
                                          "FROM   SONGS\r\n";
                    command.Connection = conn;
                    command.CommandTimeout = 10 * 60; // max 10 minuten voordat we een timeout genereren

                    MySqlDataAdapter adapter = new MySqlDataAdapter(command);
                    DataSet ds = new DataSet();
                    adapter.Fill(ds);
                    if (ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                    {
                        minID = Convert.ToInt32(ds.Tables[0].Rows[0]["MIN_ID"]);
                        maxID = Convert.ToInt32(ds.Tables[0].Rows[0]["MAX_ID"]);
                        return true;
                    }
                }
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e.ToString());
            }

            return false;
        }


        private static bool Exec_MySQL_LOADSUBFINGERIDS(int start, int end, out DataTable dt)
        {
            dt = null;
            // nu zorgen dat computer naam in de database komt en wij deze als ID
            // in deze class opslaan
            try
            {
                using (MySqlConnection conn = CDR.DB_Helper.NewMySQLConnection())
                {
                    if (conn == null)
                    {
                        return false;
                    }

                    MySqlCommand command = new MySqlCommand("SELECT *\r\n" +
                                                            "FROM   SONGS AS T1,\r\n" +
                                                            "       SUBFINGERID AS T2\r\n" +
                                                            "WHERE  T1.ID = T2.ID\r\n" +
                                                            "AND    T1.ID BETWEEN " + start.ToString() + " AND " + end.ToString() + "\r\n",
                        conn);
                    command.CommandTimeout = 20 * 60; // max 10 minuten voordat we een timeout genereren

                    MySqlDataAdapter adapter = new MySqlDataAdapter(command);
                    DataSet ds = new DataSet();
                    adapter.Fill(ds);
                    if (ds.Tables.Count > 0)
                    {
                        dt = ds.Tables[0];
                        return true;
                    }
                }
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e.ToString());
            }

            return false;
        }

        public static bool Exec_MySQL_LOADFINGERIDS(int start, int end, out DataTable dt)
        {
            dt = null;
            // nu zorgen dat computer naam in de database komt en wij deze als ID
            // in deze class opslaan
            try
            {
                using (MySqlConnection conn = CDR.DB_Helper.NewMySQLConnection())
                {
                    if (conn == null)
                    {
                        return false;
                    }

                    MySqlCommand command = new MySqlCommand("SELECT *\r\n" +
                                                            "FROM   SONGS AS T1,\r\n" +
                                                            "       FINGERID AS T2\r\n" +
                                                            "WHERE T1.ID = T2.ID\r\n" +
                                                            "AND   T1.ID BETWEEN " + start.ToString() + " AND " + end.ToString() + "\r\n",
                        conn);

                    command.CommandTimeout = 10 * 60; // max 10 minuten voordat we een timeout genereren

                    MySqlDataAdapter adapter = new MySqlDataAdapter(command);
                    DataSet ds = new DataSet();
                    adapter.Fill(ds);
                    if (ds.Tables.Count > 0)
                    {
                        dt = ds.Tables[0];
                        return true;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

            return false;
        }
        private static bool Exec_MySQL_LOADSUBFINGERIDS_NEW(int start, int end, out DataTable dt)
        {
            dt = null;
            // nu zorgen dat computer naam in de database komt en wij deze als ID
            // in deze class opslaan
            try
            {
                using (MySqlConnection conn = CDR.DB_Helper.NewMySQLConnection())
                {
                    if (conn == null)
                    {
                        return false;
                    }

                    MySqlCommand command = new MySqlCommand("SELECT *\r\n" +
                                                                                "FROM   NEWSONGS AS T1,\r\n" +
                                                                                "       SUBFINGERID AS T2\r\n" +
                                                                                "WHERE  T1.ID = T2.ID\r\n" +
                                                                                "AND    T1.ID BETWEEN " + start.ToString() + " AND " + end.ToString() + " AND \r\n" +
                                                                                "ISPRIORITY \r\n",
                                        conn);

                                        command.CommandTimeout = 20 * 60; // max 10 minuten voordat we een timeout genereren

                    MySqlDataAdapter adapter = new MySqlDataAdapter(command);
                    DataSet ds = new DataSet();
                    adapter.Fill(ds);
                    if (ds.Tables.Count > 0)
                    {
                        dt = ds.Tables[0];
                        return true;
                    }
                }
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e.ToString());
            }

            return false;
        }
        #endregion
    }
}
