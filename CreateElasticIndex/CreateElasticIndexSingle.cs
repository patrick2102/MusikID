using AudioFingerprint.Audio;
using ElasticFingerprints;
using MySql.Data.MySqlClient;
using Nest;
using System;
using System.Data;
using System.Text;

namespace CreateElasticIndex
{
    public class CreateElasticIndexSingle
    {

        public bool IndexSingleElement(string indexName, int id)
        {
            DateTime startTime = DateTime.Now;
            DateTime endTime;

            string IniPath = CDR.DB_Helper.FingerprintIniFile;
            CDR.Ini.IniFile ini = new CDR.Ini.IniFile(IniPath);

            string subFingerMap = ini.IniReadValue("Program", "SubFingerMap", "SubFingerLookup");

            BassLifetimeManager.bass_EMail = ini.IniReadValue("BASS", "bass_EMail", "");
            BassLifetimeManager.bass_RegistrationKey = ini.IniReadValue("BASS", "bass_RegistrationKey", "");

            var settings = new ConnectionSettings(new Uri("http://itumur-search01:9200/")).DefaultIndex(indexName);
           
            var client = new ElasticClient(settings);

            //TODO - properly make it async

            int fingerCount = 0;

            try
            {
                StringBuilder sb = new StringBuilder(256 * 1024);

                DataTable dt;
                
                if (Exec_MySQL_FindSingleSong(id, out dt))
                {
                    foreach (DataRow row in dt.Rows)
                    {
                        var ep = new ElasticFingerprint();
                        fingerCount++;
                        if ((fingerCount % 100) == 0 || fingerCount <= 1)
                        {
                            Console.Write("\rIndexing song #" + id);
                            //  float percentage = (((float)fingerCount) / 1) * 100;
                            //  new SQLCommunication().UpdateJob(jobID, percentage);
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

                        ep.Id = row["ID"].ToString();
                        ep.Fp = sb.ToString();

                        var indexResponse = client.CreateDocument(ep);
                    }
                }
                else
                {
                    if (!RetryDatabaseError())
                    {
                        return false;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Something went wrong");
                Console.WriteLine(e.Message);
                return false;
            }
                endTime = DateTime.Now;
                TimeSpan ts = (endTime - startTime);
                Console.WriteLine(String.Format("Elapsed index time {0:00}:{1:00}:{2:00}.{3:000}", ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds));
                Console.WriteLine();

                return true;
            
        }

        //TODO rewrite to repo??
        #region MySQL

        private static bool Exec_MySQL_FindSingleSong(int id, out DataTable dt)
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
                                                            "FROM  SONGS AS T1,\r\n" +
                                                            "       SUBFINGERID AS T2\r\n" +
                                                            "WHERE  T1.ID = T2.ID\r\n" +
                                                            "AND    T1.ID = " + id + "\r\n",
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
    }
}