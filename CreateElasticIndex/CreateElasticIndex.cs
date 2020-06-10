using AudioFingerprint.Audio;
using MySql.Data.MySqlClient;
using Nest;
using System;
using System.Data;
using System.Globalization;
using System.Text;

namespace CreateElasticIndex
{
    public class CreateElasticIndex
    {

        public bool CreateIndex(string indexName, int id_offset = 0, int id_Max = -1)
        {
          //  new SQLCommunication().InsertJob(JobType.CreateLuceneIndex.ToString(), DateTime.Now, 0, -1, $"machine : {Environment.MachineName}", out int jobID);
            string IniPath = CDR.DB_Helper.FingerprintIniFile;
            CDR.Ini.IniFile ini = new CDR.Ini.IniFile(IniPath);

            // "dr"   "dr200k"
            var settings = new ConnectionSettings(new Uri("http://itumur-search01:9200/")).DefaultIndex(indexName);

            var client = new ElasticClient(settings);

            int minID;
            int maxID;
            //TODO - properly make it async

            if (id_Max == -1)
            {
                if (!Exec_MySQL_MinAndMax_IDS(out minID, out maxID) && minID >= 1)
                {
                    return false;
                }
            }
            else maxID = id_Max;

            minID = id_offset;
            int fingerCount = minID;

            //float percentage = (((float)fingerCount) / maxID) * 100;
            //new SQLCommunication().UpdateJob(jobID, percentage);

            try
            {
                StringBuilder sb = new StringBuilder(256 * 1024);

                int start = minID;
                int count = 5000;
                while (start <= maxID)
                {
                    DataTable dt;
                    //HACK
                    if (Exec_MySQL_LOADSUBFINGERIDS_NEW(start, (start + count - 1), out dt))
                    {
                        foreach (DataRow row in dt.Rows)
                        {
                            var ep = new ElasticFingerprints.ElasticFingerprint();

                            fingerCount++;
                            if ((fingerCount % 100) == 0 || fingerCount <= 1)
                            {
                                Console.Write("\rIndexing subfingerprint #" + fingerCount.ToString() + " out of #" + maxID + "\n");
                                Console.Write("MinID: #" + minID);
                                //percentage = (((float)fingerCount) / maxID) * 100;
                                //new SQLCommunication().UpdateJob(jobID, percentage);
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
                        Console.WriteLine("\rIndexing subfingerprint #" + fingerCount.ToString() + " out of #" + maxID + "\n");
                        start += count; 
                    } else
                    {
                        if(!RetryDatabaseError())
                        {
                            return false; 
                        }
                    }
                }
            }
            catch
            {
                Console.WriteLine("Something went wrong");
            }
            
                Console.WriteLine();

                return true;
            
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
                                                            "FROM  SONGS AS T1,\r\n" +
                                                            "      SUBFINGERID AS T2\r\n" +
                                                            "WHERE T1.ID = T2.ID\r\n" +
                                                            "AND   T1.ID BETWEEN " + start.ToString() + " AND " + end.ToString() + "\r\n",
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
                                                                                "FROM  SONGS AS T1,\r\n" +
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