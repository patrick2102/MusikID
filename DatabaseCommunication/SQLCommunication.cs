using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;


namespace DatabaseCommunication
{
    /*
     * This class is used for accessing data in the database. All functions return a bool, which describes if the actions were successful.
     * If additional information is needed from the query, then it is returned with the "out" variables.
     * 
     */
    public enum JobType
    {
        Fingerprint, AudioMatch,
        RollingWindow,
        StartRadioMonitoring,
        CreateLuceneIndex
    }

    public class SQLCommunication : IDBCommunication
    {
        public bool GetRadioURLFromID(string id, out string url)
        {
            bool deadlocked = true;
            while (deadlocked)
            {
                deadlocked = false;
                try
                {
                    using (MySqlConnection conn = DB_Helper.NewMySQLConnection())
                    {
                        MySqlCommand command = new MySqlCommand();
                        command.Connection = conn;

                        command.CommandText = StoredProceduresEnums.GET_RADIO_URL_FROM_ID.ToString();
                        command.CommandType = CommandType.StoredProcedure;
                        command.CommandTimeout = 3000;
                        command.Parameters.Add(DatabaseArgumentEnums.parCHANNEL_ID.ToString(), MySqlDbType.VarChar).Value = id;

                        url = Convert.ToString(command.ExecuteScalar());
                        return true;
                    }
                }
                catch (Exception e)
                {
                    if (e.ToString().Contains("deadlocked"))
                    {
                        System.Threading.Thread.Sleep(new Random().Next(50, 500));
                        deadlocked = true;
                    }
                    else
                    {
                        Console.Error.WriteLine(e.ToString());
                    }
                }
            } //while
            url = "";
            return false;
        }

        public bool DeleteResults(int fileID)
        {
            bool deadlocked = true;
            while (deadlocked)
            {
                deadlocked = false;
                try
                {
                    using (MySqlConnection conn = DB_Helper.NewMySQLConnection())
                    {
                        MySqlCommand command = new MySqlCommand();
                        command.Connection = conn;

                        command.CommandText = StoredProceduresEnums.DELETE_RESULTS.ToString();
                        command.CommandType = CommandType.StoredProcedure;
                        command.CommandTimeout = 3000;
                        command.Parameters.Add(DatabaseArgumentEnums.parFILE_ID.ToString(), MySqlDbType.Int64).Value = (long) fileID;


                        command.ExecuteScalar();
                        conn.Close();
                        return true;
                    }
                }
                catch (Exception e)
                {
                    if (e.ToString().Contains("deadlocked"))
                    {
                        System.Threading.Thread.Sleep(new Random().Next(50, 500));
                        deadlocked = true;
                    }
                    else
                    {
                        Console.Error.WriteLine(e.ToString());
                    }
                }
            } //while
            return false;
        }

        public bool GetRadioURLs(out Dictionary<string, string> dict)
        {
            bool deadlocked = true;
            while (deadlocked)
            {
                deadlocked = false;
                try
                {
                    using (MySqlConnection conn = DB_Helper.NewMySQLConnection())
                    {
                        MySqlCommand command = new MySqlCommand();
                        command.Connection = conn;

                        command.CommandText = StoredProceduresEnums.GET_RADIO_URLS.ToString();
                        command.CommandType = CommandType.StoredProcedure;
                        command.CommandTimeout = 3000;


                        using (MySqlDataReader reader = command.ExecuteReader())
                        {
                            var data_reader = command.ExecuteReader();

                            dict = new Dictionary<string, string>();

                            if (data_reader.HasRows)
                            {
                                int count = data_reader.FieldCount;
                                while (data_reader.Read())
                                {
                                    dict.Add((data_reader.GetValue(0) as string), (data_reader.GetValue(1) as string));
                                }
                                data_reader.Close();
                            }
                        }

                        return true;
                    }
                }
                catch (Exception e)
                {
                    if (e.ToString().Contains("deadlocked"))
                    {
                        System.Threading.Thread.Sleep(new Random().Next(50, 500));
                        deadlocked = true;
                    }
                    else
                    {
                        Console.Error.WriteLine(e.ToString());
                    }
                }
            } //while
            dict = null;
            return false;
        }

        public bool GetTracks(int limit, out List<SQLTrack> lst)
        {
            bool deadlocked = true;
            while (deadlocked)
            {
                deadlocked = false;
                try
                {
                    using (MySqlConnection conn = DB_Helper.NewMySQLConnection())
                    {
                        MySqlCommand command = new MySqlCommand();
                        command.Connection = conn;

                        command.CommandText = StoredProceduresEnums.GET_TRACKS.ToString();

                        command.Parameters.Add(DatabaseArgumentEnums.parLIMIT.ToString(), MySqlDbType.Int32).Value = limit;
                        command.CommandType = CommandType.StoredProcedure;
                        command.CommandTimeout = 3000;


                        using (MySqlDataReader data_reader = command.ExecuteReader())
                        {

                            lst = new List<SQLTrack>();

                            if (data_reader.HasRows)
                            {
                                while (data_reader.Read())
                                {
                                    var id = (int) data_reader.GetValue(0);
                                    var dr_diskoteksnummer = (int) data_reader.GetValue(1);
                                    var sidenummer = (int) data_reader.GetValue(2);
                                    var sekvensnummer = (int) data_reader.GetValue(3);
                                    var date_changed = (DateTime) data_reader.GetValue(4);
                                    var reference = (string)data_reader.GetValue(5);
                                    var duration = (long)data_reader.GetValue(6);

                                    var station = new SQLTrack()
                                    {
                                        id = id,
                                        dr_diskoteksnr = dr_diskoteksnummer,
                                        sidenummer = sidenummer, 
                                        sekvensnummer = sekvensnummer,
                                        date_changed = date_changed,
                                        reference = reference, 
                                        duration = duration
                                    };

                                    lst.Add(station);
                                }
                                data_reader.Close();
                            }
                        }

                        return true;
                    }
                }
                catch (Exception e)
                {
                    if (e.ToString().Contains("deadlocked"))
                    {
                        System.Threading.Thread.Sleep(new Random().Next(50, 500));
                        deadlocked = true;
                    }
                    else
                    {
                        Console.Error.WriteLine(e.ToString());
                    }
                }
            } //while
            lst = null;
            return false;

        }

        public bool GetTrack(int song_id, out List<SQLTrack> lst)
        {
            bool deadlocked = true;
            while (deadlocked)
            {
                deadlocked = false;
                try
                {
                    using (MySqlConnection conn = DB_Helper.NewMySQLConnection())
                    {
                        MySqlCommand command = new MySqlCommand();
                        command.Connection = conn;

                        command.CommandText = StoredProceduresEnums.GET_TRACK.ToString();

                        command.Parameters.Add(DatabaseArgumentEnums.parID.ToString(), MySqlDbType.Int32).Value = song_id;
                        command.CommandType = CommandType.StoredProcedure;
                        command.CommandTimeout = 3000;


                        using (MySqlDataReader data_reader = command.ExecuteReader())
                        {

                            lst = new List<SQLTrack>();

                            if (data_reader.HasRows)
                            {
                                while (data_reader.Read())
                                {
                                    var id = (int)data_reader.GetValue(0);
                                    var dr_diskoteksnummer = (int)data_reader.GetValue(1);
                                    var sidenummer = (int)data_reader.GetValue(2);
                                    var sekvensnummer = (int)data_reader.GetValue(3);
                                    var date_changed = (DateTime)data_reader.GetValue(4);
                                    var reference = (string)data_reader.GetValue(5);
                                    var duration = (long)data_reader.GetValue(6);

                                    var station = new SQLTrack()
                                    {
                                        id = id,
                                        dr_diskoteksnr = dr_diskoteksnummer,
                                        sidenummer = sidenummer,
                                        sekvensnummer = sekvensnummer,
                                        date_changed = date_changed,
                                        reference = reference,
                                        duration = duration
                                    };

                                    lst.Add(station);
                                }
                                data_reader.Close();
                            }
                        }

                        return true;
                    }
                }
                catch (Exception e)
                {
                    if (e.ToString().Contains("deadlocked"))
                    {
                        System.Threading.Thread.Sleep(new Random().Next(50, 500));
                        deadlocked = true;
                    }
                    else
                    {
                        Console.Error.WriteLine(e.ToString());
                    }
                }
            } //while
            lst = null;
            return false;

        }

        public bool GetTrackWithDiskoNumber(string diskoNumber, out List<SQLTrack> lst)
        {
            bool deadlocked = true;
            while (deadlocked)
            {
                deadlocked = false;
                try
                {
                    using (MySqlConnection conn = DB_Helper.NewMySQLConnection())
                    {
                        MySqlCommand command = new MySqlCommand();
                        command.Connection = conn;

                        command.CommandText = StoredProceduresEnums.GET_TRACK_DISKONUMBER.ToString();

                        command.Parameters.Add(DatabaseArgumentEnums.parID.ToString(), MySqlDbType.VarChar).Value = diskoNumber;
                        command.CommandType = CommandType.StoredProcedure;
                        command.CommandTimeout = 3000;


                        using (MySqlDataReader data_reader = command.ExecuteReader())
                        {

                            lst = new List<SQLTrack>();

                            if (data_reader.HasRows)
                            {
                                while (data_reader.Read())
                                {
                                    var id = (int)data_reader.GetValue(0);
                                    var dr_diskoteksnummer = (int)data_reader.GetValue(1);
                                    var sidenummer = (int)data_reader.GetValue(2);
                                    var sekvensnummer = (int)data_reader.GetValue(3);
                                    var date_changed = (DateTime)data_reader.GetValue(4);
                                    var reference = (string)data_reader.GetValue(5);
                                    var duration = (long)data_reader.GetValue(6);

                                    var station = new SQLTrack()
                                    {
                                        id = id,
                                        dr_diskoteksnr = dr_diskoteksnummer,
                                        sidenummer = sidenummer,
                                        sekvensnummer = sekvensnummer,
                                        date_changed = date_changed,
                                        reference = reference,
                                        duration = duration
                                    };

                                    lst.Add(station);
                                }
                                data_reader.Close();
                            }
                        }

                        return true;
                    }
                }
                catch (Exception e)
                {
                    if (e.ToString().Contains("deadlocked"))
                    {
                        System.Threading.Thread.Sleep(new Random().Next(50, 500));
                        deadlocked = true;
                    }
                    else
                    {
                        Console.Error.WriteLine(e.ToString());
                    }
                }
            } //while
            lst = null;
            return false;

        }


        public bool InsertTask(TaskType type, string arguments, int job_id, out int task_id)
        {
            bool deadlocked = true;
            while (deadlocked)
            {
                deadlocked = false;
                try
                {
                    using (MySqlConnection conn = DB_Helper.NewMySQLConnection())
                    {
                        MySqlCommand command = new MySqlCommand();
                        command.Connection = conn;

                        command.CommandText = StoredProceduresEnums.INSERT_TASK.ToString();
                        command.CommandType = CommandType.StoredProcedure;
                        command.CommandTimeout = 3000;

                        command.Parameters.Add(DatabaseArgumentEnums.parTASK_TYPE.ToString(), MySqlDbType.VarChar).Value = type.ToString();
                        command.Parameters.Add(DatabaseArgumentEnums.parARGUMENTS.ToString(), MySqlDbType.VarChar).Value = arguments;
                        command.Parameters.Add(DatabaseArgumentEnums.parJOB_ID.ToString(), MySqlDbType.Int64).Value = job_id;

                        task_id = (int)Convert.ToInt64(command.ExecuteScalar());

                        return true;
                    }
                }
                catch (Exception e)
                {
                    if (e.ToString().Contains("deadlocked"))
                    {
                        System.Threading.Thread.Sleep(new Random().Next(50, 500));
                        deadlocked = true;
                    }
                    else
                    {
                        Console.Error.WriteLine(e.ToString());
                    }
                }
            } //while
            task_id = -1;
            return false;
        }

        public bool InsertFingerTask(TaskType type, string arguments, int job_id, out int task_id)
        {
            bool deadlocked = true;
            while (deadlocked)
            {
                deadlocked = false;
                try
                {
                    using (MySqlConnection conn = DB_Helper.NewMySQLConnection())
                    {
                        MySqlCommand command = new MySqlCommand();
                        command.Connection = conn;

                        command.CommandText = StoredProceduresEnums.INSERT_FINGER_TASK.ToString();
                        command.CommandType = CommandType.StoredProcedure;
                        command.CommandTimeout = 3000;

                        command.Parameters.Add(DatabaseArgumentEnums.parTASK_TYPE.ToString(), MySqlDbType.VarChar).Value = type.ToString();
                        command.Parameters.Add(DatabaseArgumentEnums.parARGUMENTS.ToString(), MySqlDbType.VarChar).Value = arguments;
                        command.Parameters.Add(DatabaseArgumentEnums.parJOB_ID.ToString(), MySqlDbType.Int64).Value = job_id;

                        task_id = (int)Convert.ToInt64(command.ExecuteScalar());

                        return true;
                    }
                }
                catch (Exception e)
                {
                    if (e.ToString().Contains("deadlocked"))
                    {
                        System.Threading.Thread.Sleep(new Random().Next(50, 500));
                        deadlocked = true;
                    }
                    else
                    {
                        Console.Error.WriteLine(e.ToString());
                    }
                }
            } //while
            task_id = -1;
            return false;
        }

        public bool UpdateStation(string channel_id, bool running, out List<Station> lst)
        {
            bool deadlocked = true;
            while (deadlocked)
            {
                deadlocked = false;
                try
                {
                    using (MySqlConnection conn = DB_Helper.NewMySQLConnection())
                    {
                        MySqlCommand command = new MySqlCommand();
                        command.Connection = conn;

                        command.CommandText = StoredProceduresEnums.UPDATE_STATION.ToString();
                        command.Parameters.Add(DatabaseArgumentEnums.parCHANNEL_ID.ToString(), MySqlDbType.VarChar).Value = channel_id;
                        command.Parameters.Add(DatabaseArgumentEnums.parRUNNING.ToString(), MySqlDbType.Binary).Value = running ? 1 : 0;
                        command.CommandType = CommandType.StoredProcedure;
                        command.CommandTimeout = 3000;


                        using (MySqlDataReader data_reader = command.ExecuteReader())
                        {

                            lst = new List<Station>();

                            if (data_reader.HasRows)
                            {
                                while (data_reader.Read())
                                {
                                    var id = data_reader.GetValue(0) as string;
                                    var channel_name = data_reader.GetValue(1) as string;
                                    var channel_type = data_reader.GetValue(2) as string;
                                    var streaming_url = data_reader.GetValue(3) as string;
                                    var status = (bool)data_reader.GetValue(4);

                                    var station = new Station()
                                    {
                                        DR_ID = id,
                                        channel_name = channel_name,
                                        channel_type = channel_type,
                                        streaming_url = streaming_url,
                                        running = status
                                    };

                                    lst.Add(station);
                                }
                                data_reader.Close();
                            }
                        }

                        return true;
                    }
                }
                catch (Exception e)
                {
                    if (e.ToString().Contains("deadlocked"))
                    {
                        System.Threading.Thread.Sleep(new Random().Next(50, 500));
                        deadlocked = true;
                    }
                    else
                    {
                        Console.Error.WriteLine(e.ToString());
                    }
                }
            } //while
            lst = null;
            return false;
        }

        public bool GetStations(out List<Station> lst)
        {
            bool deadlocked = true;
            while (deadlocked)
            {
                deadlocked = false;
                try
                {
                    using (MySqlConnection conn = DB_Helper.NewMySQLConnection())
                    {
                        MySqlCommand command = new MySqlCommand();
                        command.Connection = conn;

                        command.CommandText = StoredProceduresEnums.GET_STATIONS_ALL.ToString();
                        command.CommandType = CommandType.StoredProcedure;
                        command.CommandTimeout = 3000;


                        using (MySqlDataReader data_reader = command.ExecuteReader())
                        {

                            lst = new List<Station>();

                            if (data_reader.HasRows)
                            {
                                while (data_reader.Read())
                                {
                                    var id = data_reader.GetValue(0) as string;
                                    var channel_name = data_reader.GetValue(1) as string;
                                    var channel_type = data_reader.GetValue(2) as string;
                                    var streaming_url = data_reader.GetValue(3) as string;
                                    var status = (bool)data_reader.GetValue(4);

                                    var station = new Station()
                                    {
                                        DR_ID = id,
                                        channel_name = channel_name,
                                        channel_type = channel_type,
                                        streaming_url = streaming_url,
                                        running = status
                                    };

                                    lst.Add(station);
                                }
                                data_reader.Close();
                            }
                        }

                        return true;
                    }
                }
                catch (Exception e)
                {
                    if (e.ToString().Contains("deadlocked"))
                    {
                        System.Threading.Thread.Sleep(new Random().Next(50, 500));
                        deadlocked = true;
                    }
                    else
                    {
                        Console.Error.WriteLine(e.ToString());
                    }
                }
            } //while
            lst = null;
            return false;
        }

        public bool InsertRadioTask(string channel_id, string chunk_path, int job_id, out int task_id)
        {
            bool deadlocked = true;
            while (deadlocked)
            {
                deadlocked = false;
                try
                {
                    using (MySqlConnection conn = DB_Helper.NewMySQLConnection())
                    {
                        MySqlCommand command = new MySqlCommand();
                        command.Connection = conn;

                        command.CommandText = StoredProceduresEnums.INSERT_RADIO_TASK.ToString();
                        command.CommandType = CommandType.StoredProcedure;
                        command.CommandTimeout = 3000;

                        command.Parameters.Add(DatabaseArgumentEnums.parCHANNEL_ID.ToString(), MySqlDbType.VarChar).Value = channel_id;
                        command.Parameters.Add(DatabaseArgumentEnums.parCHUNK_PATH.ToString(), MySqlDbType.VarChar).Value = chunk_path;
                        command.Parameters.Add(DatabaseArgumentEnums.parJOB_ID.ToString(), MySqlDbType.Int32).Value = job_id;


                        task_id = (int)Convert.ToInt64(command.ExecuteScalar());

                        return true;
                    }
                }
                catch (Exception e)
                {
                    if (e.ToString().Contains("deadlocked"))
                    {
                        System.Threading.Thread.Sleep(new Random().Next(50, 500));
                        deadlocked = true;
                    }
                    else
                    {
                        Console.Error.WriteLine(e.ToString());
                    }
                }
            } //while
            task_id = -1;
            return false;
        }

        public bool UpdateTask(int id)
        {
            bool deadlocked = true;
            while (deadlocked)
            {
                deadlocked = false;
                try
                {
                    using (MySqlConnection conn = DB_Helper.NewMySQLConnection())
                    {
                        MySqlCommand command = new MySqlCommand();
                        command.Connection = conn;

                        command.CommandText = StoredProceduresEnums.UPDATE_TASK.ToString();
                        command.CommandType = CommandType.StoredProcedure;
                        command.CommandTimeout = 3000;

                        command.Parameters.Add(DatabaseArgumentEnums.parID.ToString(), MySqlDbType.Int32).Value = id;
                        command.Parameters.Add(DatabaseArgumentEnums.parMACHINE.ToString(), MySqlDbType.VarChar).Value = Environment.MachineName;
                        command.ExecuteNonQuery();
                        return true;
                    }
                }
                catch (Exception e)
                {
                    if (e.ToString().Contains("deadlocked"))
                    {
                        System.Threading.Thread.Sleep(new Random().Next(50, 500));
                        deadlocked = true;
                    }
                    else
                    {
                        Console.Error.WriteLine(e.ToString());
                    }
                }
            } //while
            id = -1;
            return false;
        }

        public bool UpdateFingerTask(int id)
        {
            bool deadlocked = true;
            while (deadlocked)
            {
                deadlocked = false;
                try
                {
                    using (MySqlConnection conn = DB_Helper.NewMySQLConnection())
                    {
                        MySqlCommand command = new MySqlCommand();
                        command.Connection = conn;

                        command.CommandText = StoredProceduresEnums.UPDATE_FINGER_TASK.ToString();
                        command.CommandType = CommandType.StoredProcedure;
                        command.CommandTimeout = 3000;

                        command.Parameters.Add(DatabaseArgumentEnums.parID.ToString(), MySqlDbType.Int32).Value = id;
                        command.Parameters.Add(DatabaseArgumentEnums.parMACHINE.ToString(), MySqlDbType.VarChar).Value = Environment.MachineName;
                        command.ExecuteNonQuery();
                        return true;
                    }
                }
                catch (Exception e)
                {
                    if (e.ToString().Contains("deadlocked"))
                    {
                        System.Threading.Thread.Sleep(new Random().Next(50, 500));
                        deadlocked = true;
                    }
                    else
                    {
                        Console.Error.WriteLine(e.ToString());
                    }
                }
            } //while
            id = -1;
            return false;
        }

        public bool GetTask(out Assignment ass)
        {
            bool deadlocked = true;
            while (deadlocked)
            {
                deadlocked = false;
                try
                {
                    using (MySqlConnection conn = DB_Helper.NewMySQLConnection())
                    {
                        MySqlCommand command = new MySqlCommand();
                        command.Connection = conn;

                        command.CommandText = StoredProceduresEnums.GET_TASK.ToString();
                        command.CommandType = CommandType.StoredProcedure;
                        command.CommandTimeout = 3000;

                        command.Parameters.Add(DatabaseArgumentEnums.parMACHINE.ToString(), MySqlDbType.VarChar).Value = Environment.MachineName;

                        using (MySqlDataReader data_reader = command.ExecuteReader())
                        {
                            //SELECT id, task_type, arguments, job_id FROM TASK_QUEUE WHERE id = TASK_ID;
                            if (data_reader.HasRows)
                            {
                                int count = data_reader.FieldCount;
                                data_reader.Read();
                                var id = (int)Convert.ToInt64(data_reader.GetValue(0));
                                var taskType = (TaskType)Enum.Parse(typeof(TaskType), data_reader.GetValue(1) as string);
                                var Arguments = data_reader.GetValue(2) as string;
                                var Job_Id = (int)Convert.ToInt64(data_reader.GetValue(3));
                                var file_id = (int)Convert.ToInt64(data_reader.GetValue(4));
                                ass = new Assignment()
                                {
                                    Arguments = Arguments,
                                    JobID = Job_Id,
                                    ID = id,
                                    Type = taskType,
                                    FileID = file_id,
                                };

                                data_reader.Close();

                            } else
                            {
                                ass = null;
                            }
                        }
                        return true;
                    }
                }
                catch (Exception e)
                {
                    if (e.ToString().Contains("deadlocked"))
                    {
                        System.Threading.Thread.Sleep(new Random().Next(50, 500));
                        deadlocked = true;
                    }
                    else
                    {
                        Console.Error.WriteLine(e.ToString());
                    }
                }
            } //while
            ass = null;
            return false;
        }

        public bool GetFingerTask(out Assignment ass)
        {
            bool deadlocked = true;
            while (deadlocked)
            {
                deadlocked = false;
                try
                {
                    using (MySqlConnection conn = DB_Helper.NewMySQLConnection())
                    {
                        MySqlCommand command = new MySqlCommand();
                        command.Connection = conn;

                        command.CommandText = StoredProceduresEnums.GET_FINGER_TASK.ToString();
                        command.CommandType = CommandType.StoredProcedure;
                        command.CommandTimeout = 3000;

                        command.Parameters.Add(DatabaseArgumentEnums.parMACHINE.ToString(), MySqlDbType.VarChar).Value = Environment.MachineName;

                        using (MySqlDataReader data_reader = command.ExecuteReader())
                        {
                            //SELECT id, task_type, arguments, job_id FROM TASK_QUEUE WHERE id = TASK_ID;
                            if (data_reader.HasRows)
                            {
                                int count = data_reader.FieldCount;
                                data_reader.Read();
                                var id = (int)Convert.ToInt64(data_reader.GetValue(0));
                                var taskType = (TaskType)Enum.Parse(typeof(TaskType), data_reader.GetValue(1) as string);
                                var Arguments = data_reader.GetValue(2) as string;
                                var Job_Id = (int)Convert.ToInt64(data_reader.GetValue(3));
                                var file_id = (int)Convert.ToInt64(data_reader.GetValue(4));
                                ass = new Assignment()
                                {
                                    Arguments = Arguments,
                                    JobID = Job_Id,
                                    ID = id,
                                    Type = taskType,
                                    FileID = file_id,
                                };

                                data_reader.Close();

                            }
                            else
                            {
                                ass = null;
                            }
                        }
                        return true;
                    }
                }
                catch (Exception e)
                {
                    if (e.ToString().Contains("deadlocked"))
                    {
                        System.Threading.Thread.Sleep(new Random().Next(50, 500));
                        deadlocked = true;
                    }
                    else
                    {
                        Console.Error.WriteLine(e.ToString());
                    }
                }
            } //while
            ass = null;
            return false;
        }

        public bool GetRadioTask(out RadioAssignment ass)
        {
            bool deadlocked = true;
            while (deadlocked)
            {
                deadlocked = false;
                try
                {
                    using (MySqlConnection conn = DB_Helper.NewMySQLConnection())
                    {
                        MySqlCommand command = new MySqlCommand();
                        command.Connection = conn;

                        command.CommandText = StoredProceduresEnums.GET_RADIO_TASK.ToString();
                        command.CommandType = CommandType.StoredProcedure;
                        command.CommandTimeout = 3000;

                        command.Parameters.Add(DatabaseArgumentEnums.parMACHINE.ToString(), MySqlDbType.VarChar).Value = Environment.MachineName;

                        using (MySqlDataReader data_reader = command.ExecuteReader())
                        {
                            //id, channel_id, chunk_path, job_id
                            if (data_reader.HasRows)
                            {
                                int count = data_reader.FieldCount;
                                data_reader.Read();
                                var id = (int)Convert.ToInt64(data_reader.GetValue(0));
                                var channel_id = data_reader.GetValue(1) as string;
                                var chunk_path = data_reader.GetValue(2) as string;
                                var Job_Id = (int)Convert.ToInt64(data_reader.GetValue(3));
                                ass = new RadioAssignment()
                                {
                                    Chunk_path = chunk_path,
                                    Channel_id = channel_id,
                                    ID = id,
                                    JobID = Job_Id,
                                };

                                data_reader.Close();

                            }
                            else
                            {
                                ass = null;
                            }
                            
                        }
                            return true;
                    }
                }
                catch (Exception e)
                {
                    if (e.ToString().Contains("deadlocked"))
                    {
                        System.Threading.Thread.Sleep(new Random().Next(50, 500));
                        deadlocked = true;
                    }
                    else
                    {
                        Console.Error.WriteLine(e.ToString());
                    }
                }
            } //while
            ass = null;
            return false;
        }

        public bool DeleteTask(int Task_id)
        {
            bool deadlocked = true;
            while (deadlocked)
            {
                deadlocked = false;
                try
                {
                    using (MySqlConnection conn = DB_Helper.NewMySQLConnection())
                    {
                        MySqlCommand command = new MySqlCommand();
                        command.Connection = conn;

                        command.CommandText = StoredProceduresEnums.DELETE_TASK.ToString();
                        command.CommandType = CommandType.StoredProcedure;
                        command.CommandTimeout = 3000;

                        command.Parameters.Add(DatabaseArgumentEnums.parTASK_ID.ToString(), MySqlDbType.Int64).Value = (long)Task_id;


                        command.ExecuteScalar();

                        return true;
                    }
                }
                catch (Exception e)
                {
                    if (e.ToString().Contains("deadlocked"))
                    {
                        System.Threading.Thread.Sleep(new Random().Next(50, 500));
                        deadlocked = true;
                    }
                    else
                    {
                        Console.Error.WriteLine(e.ToString());
                    }
                }
            } //while
            return false;
        }

        public bool DeleteFingerTask(int Task_id)
        {
            bool deadlocked = true;
            while (deadlocked)
            {
                deadlocked = false;
                try
                {
                    using (MySqlConnection conn = DB_Helper.NewMySQLConnection())
                    {
                        MySqlCommand command = new MySqlCommand();
                        command.Connection = conn;

                        command.CommandText = StoredProceduresEnums.DELETE_FINGER_TASK.ToString();
                        command.CommandType = CommandType.StoredProcedure;
                        command.CommandTimeout = 3000;

                        command.Parameters.Add(DatabaseArgumentEnums.parTASK_ID.ToString(), MySqlDbType.Int64).Value = (long)Task_id;


                        command.ExecuteScalar();

                        return true;
                    }
                }
                catch (Exception e)
                {
                    if (e.ToString().Contains("deadlocked"))
                    {
                        System.Threading.Thread.Sleep(new Random().Next(50, 500));
                        deadlocked = true;
                    }
                    else
                    {
                        Console.Error.WriteLine(e.ToString());
                    }
                }
            } //while
            return false;
        }

        public bool GetJobs(out List<Job> jobs)
        {
            bool deadlocked = true;
            while (deadlocked)
            {
                deadlocked = false;
                try
                {
                    using (MySqlConnection conn = DB_Helper.NewMySQLConnection())
                    {
                        MySqlCommand command = new MySqlCommand();
                        command.Connection = conn;

                        command.CommandText = StoredProceduresEnums.GET_JOBS.ToString();
                        command.CommandType = CommandType.StoredProcedure;
                        command.CommandTimeout = 3000;


                        using (MySqlDataReader data_reader = command.ExecuteReader())
                        {

                            jobs = new List<Job>();

                            if (data_reader.HasRows)
                            {
                                int count = data_reader.FieldCount;
                                while (data_reader.Read())
                                {
                                    var id = (long)data_reader.GetValue(0);
                                    var job_type = (JobType)Enum.Parse(typeof(JobType), data_reader.GetValue(1) as string);
                                    var file_id = data_reader.GetValue(2) as string;
                                    var start_time = (DateTime)data_reader.GetValue(3);
                                    var last_updated = (DateTime)data_reader.GetValue(4);

                                    var arguments = data_reader.GetValue(5) as string;

                                    var percentage = (float)data_reader.GetValue(6);

                                    var job = new Job(id, job_type, file_id, start_time, last_updated, arguments, percentage);

                                    jobs.Add(job);
                                }
                                data_reader.Close();
                            }
                        }
                        return true;
                    }
                }
                catch (Exception e)
                {
                    if (e.ToString().Contains("deadlocked"))
                    {
                        System.Threading.Thread.Sleep(new Random().Next(50, 500));
                        deadlocked = true;
                    }
                    else
                    {
                        Console.Error.WriteLine(e.ToString());
                    }
                }
            } //while
            jobs = null;
            return false;
        }

        public void GetOnDemandFile(int file_id, out OnDemandFile file)
        {
            throw new NotImplementedException();
        }

        public bool DeleteRadioTask(int Task_id)
        {
            bool deadlocked = true;
            while (deadlocked)
            {
                deadlocked = false;
                try
                {
                    using (MySqlConnection conn = DB_Helper.NewMySQLConnection())
                    {
                        MySqlCommand command = new MySqlCommand();
                        command.Connection = conn;

                        command.CommandText = StoredProceduresEnums.DELETE_RADIO_TASK.ToString();
                        command.CommandType = CommandType.StoredProcedure;
                        command.CommandTimeout = 3000;

                        command.Parameters.Add(DatabaseArgumentEnums.parTASK_ID.ToString(), MySqlDbType.Int64).Value = (long)Task_id;


                        command.ExecuteScalar();

                        return true;
                    }
                }
                catch (Exception e)
                {
                    if (e.ToString().Contains("deadlocked"))
                    {
                        System.Threading.Thread.Sleep(new Random().Next(50, 500));
                        deadlocked = true;
                    }
                    else
                    {
                        Console.Error.WriteLine(e.ToString());
                    }
                }
            } //while
            return false;
        }

       public bool GetLivestreamResults(DateTime start, DateTime end, string channel_id, out List<Result> lst)
        {
            bool deadlocked = true;
            while (deadlocked)
            {
                deadlocked = false;
                try
                {
                    using (MySqlConnection conn = DB_Helper.NewMySQLConnection())
                    {
                        MySqlCommand command = new MySqlCommand();
                        command.Connection = conn;

                        command.CommandText = StoredProceduresEnums.GET_LIVESTREAM_RESULTS.ToString();
                        command.Parameters.Add(DatabaseArgumentEnums.parSTART.ToString(), MySqlDbType.Timestamp).Value = start;
                        command.Parameters.Add(DatabaseArgumentEnums.parEND.ToString(), MySqlDbType.Timestamp).Value = end;
                        command.Parameters.Add(DatabaseArgumentEnums.parCHANNEL_ID.ToString(), MySqlDbType.VarChar).Value = channel_id;

                        command.CommandType = CommandType.StoredProcedure;
                        command.CommandTimeout = 3000;


                        using (MySqlDataReader data_reader = command.ExecuteReader())
                        {

                            lst = new List<Result>();

                            if (data_reader.HasRows)
                            {
                                int count = data_reader.FieldCount;
                                while (data_reader.Read())
                                {
                                    var song_id = data_reader.GetValue(0) as string;
                                    var start_time = new DateTime(((TimeSpan)data_reader.GetValue(1)).Ticks);
                                    var duration = (int)data_reader.GetValue(2);
                                    var accuracy = (float)data_reader.GetValue(3);

                                    var diskoteks_nr = (int)data_reader.GetValue(4);
                                    var side_nr = (int)data_reader.GetValue(5);
                                    var sekvens_nr = (int)data_reader.GetValue(6);

                                    var song_offset = (float)data_reader.GetValue(7);
                                    var song_duration = (Int64)data_reader.GetValue(8);


                                    var result = new Result(song_id, start_time, start_time.AddSeconds(duration), accuracy, diskoteks_nr, side_nr, sekvens_nr, song_offset, song_duration);
                                    lst.Add(result);
                                }
                                data_reader.Close();
                            }
                        }
                            return true;
                    }
                }
                catch (Exception e)
                {
                    if (e.ToString().Contains("deadlocked"))
                    {
                        System.Threading.Thread.Sleep(new Random().Next(50, 500));
                        deadlocked = true;
                    }
                    else
                    {
                        Console.Error.WriteLine(e.ToString());
                    }
                }
            } //while
            lst = null;
            return false;
        }

        public bool GetOnDemandResults(int file_id, out List<Result> lst)
        {
            bool deadlocked = true;
            while (deadlocked)
            {
                deadlocked = false;
                try
                {
                    using (MySqlConnection conn = DB_Helper.NewMySQLConnection())
                    {
                        MySqlCommand command = new MySqlCommand();
                        command.Connection = conn;

                        command.CommandText = StoredProceduresEnums.GET_ON_DEMAND_RESULTS.ToString();
                        command.Parameters.Add(DatabaseArgumentEnums.parFILE_ID.ToString(), MySqlDbType.Int64).Value = file_id;

                        command.CommandType = CommandType.StoredProcedure;
                        command.CommandTimeout = 3000;


                        using (MySqlDataReader data_reader = command.ExecuteReader())
                        {

                            lst = new List<Result>();

                            if (data_reader.HasRows)
                            {
                                int count = data_reader.FieldCount;
                                while (data_reader.Read())
                                {
                                    var song_id = data_reader.GetValue(0) as string;
                                    var start_time = new DateTime(((TimeSpan)data_reader.GetValue(1)).Ticks);
                                    var duration = (int)data_reader.GetValue(2);
                                    var accuracy = (float)data_reader.GetValue(3);

                                    var diskoteks_nr = (int)data_reader.GetValue(4);
                                    var side_nr = (int)data_reader.GetValue(5);
                                    var sekvens_nr = (int)data_reader.GetValue(6);

                                    var song_offset = (float)data_reader.GetValue(7);
                                    var song_duration = (Int64)data_reader.GetValue(8);


                                    var result = new Result(song_id, start_time, start_time.AddSeconds(duration), accuracy, diskoteks_nr, side_nr, sekvens_nr, song_offset, song_duration);
                                    lst.Add(result);

                                }
                                data_reader.Close();
                            }
                        }
                            return true;
                    }
                }
                catch (Exception e)
                {
                    if (e.ToString().Contains("deadlocked"))
                    {
                        System.Threading.Thread.Sleep(new Random().Next(50, 500));
                        deadlocked = true;
                    }
                    else
                    {
                        Console.Error.WriteLine(e.ToString());
                    }
                }
            } //while
            lst = new List<Result>();
            return false;
        }

        public bool GetJob(int file_id_in, out Job job)
        {
            bool deadlocked = true;
            while (deadlocked)
            {
                deadlocked = false;
                try
                {
                    using (MySqlConnection conn = DB_Helper.NewMySQLConnection())
                    {
                        MySqlCommand command = new MySqlCommand();
                        command.Connection = conn;

                        command.CommandText = StoredProceduresEnums.GET_JOB.ToString();
                        command.Parameters.Add(DatabaseArgumentEnums.parFILE_ID.ToString(), MySqlDbType.Int64).Value = (long)file_id_in;

                        command.CommandType = CommandType.StoredProcedure;
                        command.CommandTimeout = 3000;



                        using (MySqlDataReader data_reader = command.ExecuteReader())
                        {

                            data_reader.Read();

                            var id = (long)data_reader.GetValue(0);
                            var job_type = (JobType)Enum.Parse(typeof(JobType), data_reader.GetValue(1) as string);
                            var file_id = data_reader.GetValue(2) as string;
                            var start_time = (DateTime)data_reader.GetValue(3);
                            var last_updated = (DateTime)data_reader.GetValue(4);

                            var arguments = data_reader.GetValue(5) as string;

                            var percentage = (float)data_reader.GetValue(6);

                            var val = data_reader.GetValue(7);

                            var user = val.GetType() == typeof(DBNull) ? null : (string)val;
                        
                            job = new Job(id, job_type, file_id, start_time, last_updated, arguments, percentage, user);

                        }
                        return true;
                    }
                }
                catch (Exception e)
                {
                    if (e.ToString().Contains("deadlocked"))
                    {
                        System.Threading.Thread.Sleep(new Random().Next(50, 500));
                        deadlocked = true;
                    }
                    else
                    {
                        Console.Error.WriteLine(e.ToString());
                    }
                }
            } //while
            job = null;
            return false;
        }

        public object GetFile(long file_id, out SQLFile file)
        {
            bool deadlocked = true;
            while (deadlocked)
            {
                deadlocked = false;
                try
                {
                    using (MySqlConnection conn = DB_Helper.NewMySQLConnection())
                    {
                        MySqlCommand command = new MySqlCommand();
                        command.Connection = conn;

                        command.CommandText = StoredProceduresEnums.GET_FILE.ToString();
                        command.Parameters.Add(DatabaseArgumentEnums.parFILE_ID.ToString(), MySqlDbType.Int64).Value = file_id;

                        command.CommandType = CommandType.StoredProcedure;
                        command.CommandTimeout = 3000;



                        using (MySqlDataReader data_reader = command.ExecuteReader())
                        {
                            data_reader.Read();

                            var id = (long)data_reader.GetValue(0);
                            var path = data_reader.GetValue(1) as string;
                            var val = data_reader.GetValue(2);

                            float duration = val.GetType() == typeof(DBNull) ? -1f : (float)val;

                            file = new SQLFile()
                            {
                                id = id,
                                path = path,
                                file_duration = TimeSpan.FromSeconds(duration)
                            };
                        }
                        return true;
                    }
                }
                catch (Exception e)
                {
                    if (e.ToString().Contains("deadlocked"))
                    {
                        System.Threading.Thread.Sleep(new Random().Next(50, 500));
                        deadlocked = true;
                    }
                    else
                    {
                        Console.Error.WriteLine(e.ToString());
                    }
                }
            } //while
            file = null;
            return false;
        }

        public object UpdateFile(long job_id, int duration)
        {
            bool deadlocked = true;
            while (deadlocked)
            {
                deadlocked = false;
                try
                {
                    using (MySqlConnection conn = DB_Helper.NewMySQLConnection())
                    {
                        MySqlCommand command = new MySqlCommand();
                        command.Connection = conn;

                        command.CommandText = StoredProceduresEnums.UPDATE_FILE.ToString();
                        command.Parameters.Add(DatabaseArgumentEnums.parJOB_ID.ToString(), MySqlDbType.Int64).Value = job_id;
                        command.Parameters.Add(DatabaseArgumentEnums.parFILE_DURATION.ToString(), MySqlDbType.Int64).Value = duration;

                        command.CommandType = CommandType.StoredProcedure;
                        command.CommandTimeout = 3000;

                        command.ExecuteScalar();

                        return true;
                    }
                }
                catch (Exception e)
                {
                    if (e.ToString().Contains("deadlocked"))
                    {
                        System.Threading.Thread.Sleep(new Random().Next(50, 500));
                        deadlocked = true;
                    }
                    else
                    {
                        Console.Error.WriteLine(e.ToString());
                    }
                }
            } //while
            return false;
        }

        public void GetOnDemandFiles(int limit, out List<OnDemandFile> files)
        {
            bool deadlocked = true;
            while (deadlocked)
            {
                deadlocked = false;
                try
                {
                    using (MySqlConnection conn = DB_Helper.NewMySQLConnection())
                    {
                        MySqlCommand command = new MySqlCommand();
                        command.Connection = conn;

                        command.CommandText = StoredProceduresEnums.GET_ON_DEMAND_FILES.ToString();
                        command.CommandType = CommandType.StoredProcedure;

                        command.Parameters.Add(DatabaseArgumentEnums.parLIMIT.ToString(), MySqlDbType.Int32).Value = limit;
                        command.CommandTimeout = 3000;


                        using (MySqlDataReader data_reader = command.ExecuteReader())
                        {

                            files = new List<OnDemandFile>();

                            if (data_reader.HasRows)
                            {
                                int count = data_reader.FieldCount;
                                while (data_reader.Read())
                                {
                                    var val = data_reader.GetValue(3);

                                    var user = val.GetType() == typeof(DBNull) ? null : (string)val;

                                    var file = new OnDemandFile((data_reader.GetValue(0) as string), (int)(Convert.ToInt64(data_reader.GetValue(1))), (float)(Convert.ToInt64(data_reader.GetValue(2))), user);
                                    files.Add(file);
                                }
                                data_reader.Close();
                            }
                        }
                        return;
                    }
                }
                catch (Exception e)
                {
                    if (e.ToString().Contains("deadlocked"))
                    {
                        System.Threading.Thread.Sleep(new Random().Next(50, 500));
                        deadlocked = true;
                    }
                    else
                    {
                        Console.Error.WriteLine(e.ToString());
                    }
                }
            } //while
            files = null;
        }

        public void GetOnDemandFile(int file_id, out List<OnDemandFile> files)
        {
            bool deadlocked = true;
            while (deadlocked)
            {
                deadlocked = false;
                try
                {
                    using (MySqlConnection conn = DB_Helper.NewMySQLConnection())
                    {
                        MySqlCommand command = new MySqlCommand();
                        command.Connection = conn;

                        command.CommandText = StoredProceduresEnums.GET_ON_DEMAND_FILE.ToString();

                        command.Parameters.Add(DatabaseArgumentEnums.parFILE_ID.ToString(), MySqlDbType.Int64).Value = file_id;
                        command.CommandType = CommandType.StoredProcedure;
                        command.CommandTimeout = 3000;


                        using (MySqlDataReader data_reader = command.ExecuteReader())
                        {

                            files = new List<OnDemandFile>();

                            if (data_reader.HasRows)
                            {
                                int count = data_reader.FieldCount;
                                while (data_reader.Read())
                                {
                                    var val = data_reader.GetValue(0);

                                    var user = val.GetType() == typeof(DBNull) ? null : (string)val;

                                    var file = new OnDemandFile((data_reader.GetValue(0) as string), (int)(Convert.ToInt64(data_reader.GetValue(1))), (float)(Convert.ToInt64(data_reader.GetValue(2))), user);
                                    files.Add(file);
                                }
                                data_reader.Close();
                            }
                        }
                        return;
                    }
                }
                catch (Exception e)
                {
                    if (e.ToString().Contains("deadlocked"))
                    {
                        System.Threading.Thread.Sleep(new Random().Next(50, 500));
                        deadlocked = true;
                    }
                    else
                    {
                        Console.Error.WriteLine(e.ToString());
                    }
                }
            } //while
            files = null;
        }

        public bool InsertStation(string ID, string URL, out Station station)
        {
            bool deadlocked = true;
            while (deadlocked)
            {
                deadlocked = false;
                try
                {
                    using (MySqlConnection conn = DB_Helper.NewMySQLConnection())
                    {
                        MySqlCommand command = new MySqlCommand();
                        command.Connection = conn;

                        command.CommandText = StoredProceduresEnums.INSERT_STATIONS.ToString();
                        command.CommandType = CommandType.StoredProcedure;
                        command.CommandTimeout = 3000;

                        command.Parameters.Add(DatabaseArgumentEnums.parCHANNEL_ID.ToString(), MySqlDbType.VarChar).Value = ID;
                        command.Parameters.Add(DatabaseArgumentEnums.parSTREAMING_URL.ToString(), MySqlDbType.VarChar).Value = URL;


                        using(MySqlDataReader data_reader = command.ExecuteReader())
                        {


                            if (data_reader.HasRows)
                            {
                                while (data_reader.Read())
                                {
                                    var id = data_reader.GetValue(0) as string;
                                    var channel_name = data_reader.GetValue(1) as string;
                                    var channel_type = data_reader.GetValue(2) as string;
                                    var streaming_url = data_reader.GetValue(3) as string;
                                    var status = (bool)data_reader.GetValue(4);

                                    station = new Station()
                                    {
                                        DR_ID = id,
                                        channel_name = channel_name,
                                        channel_type = channel_type,
                                        streaming_url = streaming_url,
                                        running = status
                                    };


                                    data_reader.Close();
                                    return true;
                                }
                            }
                        }

                        station = null;
                        return true;
                    }
                }
                catch (Exception e)
                {
                    if (e.ToString().Contains("deadlocked"))
                    {
                        System.Threading.Thread.Sleep(new Random().Next(50, 500));
                        deadlocked = true;
                    }
                    else
                    {
                        Console.Error.WriteLine(e.ToString());
                    }
                }
            } //while
            station = null;
            return false;
        }

        public bool SongAlreadyFingerprinted(int diskotekNr, int sideNr, int sequenceNr)
        {
            bool deadlocked = true;
            while (deadlocked)
            {
                deadlocked = false;
                try
                {
                    using (MySqlConnection conn = DB_Helper.NewMySQLConnection())
                    {
                        MySqlCommand command = new MySqlCommand();
                        command.Connection = conn;

                        //HACK
                        command.CommandText = StoredProceduresEnums.CHECK_IF_SONG_FP.ToString();
                        command.CommandType = CommandType.StoredProcedure;
                        command.CommandTimeout = 3000;

                        command.Parameters.Add(DatabaseArgumentEnums.parDR_DISKOTEKSNR.ToString(), MySqlDbType.Int32).Value = diskotekNr;
                        command.Parameters.Add(DatabaseArgumentEnums.parSIDENUMMER.ToString(), MySqlDbType.Int32).Value = sideNr;
                        command.Parameters.Add(DatabaseArgumentEnums.parSEKVENSNUMMER.ToString(), MySqlDbType.Int32).Value = sequenceNr;

                        int count = Convert.ToInt32(command.ExecuteScalar());

                        if (count > 0)
                            return true;
                        else
                            return false;
                    }
                }
                catch (Exception e)
                {
                    if (e.ToString().Contains("deadlocked"))
                    {
                        System.Threading.Thread.Sleep(new Random().Next(50, 500));
                        deadlocked = true;
                    }
                    else
                    {
                        Console.Error.WriteLine(e.ToString());
                    }
                }
            } //while
            return false;
        }

        public bool Exec_MySQL_SUBFINGERID_IU(int diskotekNr, int sideNr, int sequenceNr, string reference, long duration, byte[] signature, out int trackID)
        {
            Exec_MySQL_SUBFINGERID_D(reference);

            trackID = -1;

            bool deadlocked = true;
            while (deadlocked)
            {
                deadlocked = false;
                try
                {
                    using (MySqlConnection conn = DB_Helper.NewMySQLConnection())
                    {
                        MySqlCommand command = new MySqlCommand();
                        command.Connection = conn;

                        command.CommandText = StoredProceduresEnums.SUBFINGERID_IU.ToString();
                        command.CommandType = CommandType.StoredProcedure;
                        command.CommandTimeout = 3000;

                        command.Parameters.Add(DatabaseArgumentEnums.parREFERENCE.ToString(), MySqlDbType.VarChar, 20).Value = reference;
                        command.Parameters.Add(DatabaseArgumentEnums.parDR_DISKOTEKSNR.ToString(), MySqlDbType.Int32).Value = diskotekNr;
                        command.Parameters.Add(DatabaseArgumentEnums.parSIDENUMMER.ToString(), MySqlDbType.Int32).Value = sideNr;
                        command.Parameters.Add(DatabaseArgumentEnums.parSEKVENSNUMMER.ToString(), MySqlDbType.Int32).Value = sequenceNr;
                        command.Parameters.Add(DatabaseArgumentEnums.parDURATION.ToString(), MySqlDbType.Int64).Value = duration;
                        command.Parameters.Add(DatabaseArgumentEnums.parSIGNATURE.ToString(), MySqlDbType.Blob, 0).Value = signature;

                        trackID = (int) command.ExecuteScalar();

                        return true;
                    }
                }
                catch (Exception e)
                {
                    if (e.ToString().Contains("deadlocked"))
                    {
                        System.Threading.Thread.Sleep(new Random().Next(50, 500));
                        deadlocked = true;
                    }
                    else if (e.ToString().Contains("duplicate key row"))
                    {
                        // ignore

                        Console.WriteLine(reference + " | duplicate key");

                        return true;
                    }
                    else
                    {
                        Console.Error.WriteLine(e.ToString());
                    }
                }
            } //while

            return false;
        }

        public bool Exec_MySQL_SUBFINGERID_D(string reference)
        {
            bool deadlocked = true;
            while (deadlocked)
            {
                try
                {
                    using (MySqlConnection conn = DB_Helper.NewMySQLConnection())
                    {
                        MySqlCommand command = new MySqlCommand();
                        command.Connection = conn;

                        command.CommandText = StoredProceduresEnums.SUBFINGERID_D.ToString();
                        command.CommandType = CommandType.StoredProcedure;
                        command.CommandTimeout = 3000;

                        command.Parameters.Add(DatabaseArgumentEnums.parREFERENCE.ToString(), MySqlDbType.VarChar, 20).Value = reference;

                        command.ExecuteNonQuery();
                        return true;
                    }
                }
                catch (Exception e)
                {
                    if (e.ToString().Contains("deadlocked"))
                    {
                        System.Threading.Thread.Sleep(new Random().Next(50, 500));
                        deadlocked = true;
                    }
                    else
                    {
                        Console.Error.WriteLine(e.ToString());
                    }
                }
            } //while

            return false;
        }

        public bool InsertJob(string jobType, DateTime startTime, float percentage, int fileID, string arguments, out int jobID, string user = null)
        {
            bool deadlocked = true;
            while (deadlocked)
            {
                deadlocked = false;
                try
                {
                    using (MySqlConnection conn = DB_Helper.NewMySQLConnection())
                    {
                        MySqlCommand command = new MySqlCommand();
                        command.Connection = conn;

                        command.CommandText = StoredProceduresEnums.INSERT_JOB.ToString();
                        command.CommandType = CommandType.StoredProcedure;
                        command.CommandTimeout = 3000;

                        command.Parameters.Add(DatabaseArgumentEnums.parJOB_TYPE.ToString(), MySqlDbType.VarChar).Value = jobType;
                        command.Parameters.Add(DatabaseArgumentEnums.parFILE_ID.ToString(), MySqlDbType.Int32).Value = fileID;
                        command.Parameters.Add(DatabaseArgumentEnums.parSTART_DATE.ToString(), MySqlDbType.Timestamp).Value = startTime;
                        command.Parameters.Add(DatabaseArgumentEnums.parPERCENTAGE.ToString(), MySqlDbType.Float).Value = percentage;
                        command.Parameters.Add(DatabaseArgumentEnums.parARGUMENTS.ToString(), MySqlDbType.VarChar).Value = arguments;
                        command.Parameters.Add(DatabaseArgumentEnums.parUSER.ToString(), MySqlDbType.VarChar).Value = user;

                        jobID = Convert.ToInt32(command.ExecuteScalar());


                        return true;
                    }
                }
                catch (Exception e)
                {
                    if (e.ToString().Contains("deadlocked"))
                    {
                        System.Threading.Thread.Sleep(new Random().Next(50, 500));
                        deadlocked = true;
                    }
                    else
                    {
                        Console.Error.WriteLine(e.ToString());
                    }
                }
            } //while
            jobID = -1;
            return false;
        }

        public bool InsertError(string msg, int jobID)
        {
            bool deadlocked = true;
            while (deadlocked)
            {
                deadlocked = false;
                try
                {
                    using (MySqlConnection conn = DB_Helper.NewMySQLConnection())
                    {
                        MySqlCommand command = new MySqlCommand();
                        command.Connection = conn;

                        command.CommandText = StoredProceduresEnums.INSERT_ERROR.ToString();
                        command.CommandType = CommandType.StoredProcedure;
                        command.CommandTimeout = 3000;

                        command.Parameters.Add(DatabaseArgumentEnums.parJOB_ID.ToString(), MySqlDbType.Int32).Value = jobID;
                        command.Parameters.Add(DatabaseArgumentEnums.parERROR_MSG.ToString(), MySqlDbType.VarChar).Value = msg;

                        command.ExecuteScalar();
                        return true;
                    }
                }
                catch (Exception e)
                {
                    if (e.ToString().Contains("deadlocked"))
                    {
                        System.Threading.Thread.Sleep(new Random().Next(50, 500));
                        deadlocked = true;
                    }
                    else
                    {
                        Console.Error.WriteLine(e.ToString());
                    }
                }
            } //while
            return false;
        }

        public bool UpdateJob(int jobID, float percentage)
        {
            bool deadlocked = true;
            while (deadlocked)
            {
                deadlocked = false;
                try
                {
                    using (MySqlConnection conn = DB_Helper.NewMySQLConnection())
                    {
                        MySqlCommand command = new MySqlCommand();
                        command.Connection = conn;

                        command.CommandText = StoredProceduresEnums.UPDATE_JOB.ToString();
                        command.CommandType = CommandType.StoredProcedure;
                        command.CommandTimeout = 3000;

                        command.Parameters.Add(DatabaseArgumentEnums.parJOB_ID.ToString(), MySqlDbType.Int32).Value = jobID;
                        command.Parameters.Add(DatabaseArgumentEnums.parPERCENTAGE.ToString(), MySqlDbType.Float).Value = percentage;


                        command.ExecuteScalar();


                        return true;
                    }
                }
                catch (Exception e)
                {
                    if (e.ToString().Contains("deadlocked"))
                    {
                        System.Threading.Thread.Sleep(new Random().Next(50, 500));
                        deadlocked = true;
                    }
                    else
                    {
                        Console.Error.WriteLine(e.ToString());
                    }
                }
            } //while
            jobID = -1;
            return false;
        }

        public bool InsertLivestreamResult(int diskotekNr, int sideNr, int sequenceNr, DateTime offset, int duration, string channelID, float accuracy, float song_offset, out int resultID)
        {
            resultID = -1;
            bool deadlocked = true;
            while (deadlocked)
            {
                deadlocked = false;
                try
                {
                    using (MySqlConnection conn = DB_Helper.NewMySQLConnection())
                    {
                        MySqlCommand command = new MySqlCommand();
                        command.Connection = conn;

                        command.CommandText = StoredProceduresEnums.INSERT_LIVESTREAM_RESULTS.ToString();
                        command.CommandType = CommandType.StoredProcedure;
                        command.CommandTimeout = 3000;

                        command.Parameters.Add(DatabaseArgumentEnums.parDR_DISKOTEKSNR.ToString(), MySqlDbType.Int32).Value = diskotekNr;
                        command.Parameters.Add(DatabaseArgumentEnums.parSIDENUMMER.ToString(), MySqlDbType.Int32).Value = sideNr;
                        command.Parameters.Add(DatabaseArgumentEnums.parSEKVENSNUMMER.ToString(), MySqlDbType.Int32).Value = sequenceNr;

                        command.Parameters.Add(DatabaseArgumentEnums.parCHANNEL_ID.ToString(), MySqlDbType.VarChar).Value = channelID;
                        command.Parameters.Add(DatabaseArgumentEnums.parPLAY_DATE.ToString(), MySqlDbType.Timestamp).Value = offset;
                        command.Parameters.Add(DatabaseArgumentEnums.parOFFSET.ToString(), MySqlDbType.Time).Value = offset.TimeOfDay;
                        command.Parameters.Add(DatabaseArgumentEnums.parDURATION.ToString(), MySqlDbType.Int32).Value = duration;
                        command.Parameters.Add(DatabaseArgumentEnums.parACCURACY.ToString(), MySqlDbType.Float).Value = accuracy;
                        command.Parameters.Add(DatabaseArgumentEnums.parSONG_OFFSET.ToString(), MySqlDbType.Float).Value = song_offset;

                        resultID = Convert.ToInt32(command.ExecuteScalar());
                        return true;
                    }
                }
                catch (Exception e)
                {
                    if (e.ToString().Contains("deadlocked"))
                    {
                        System.Threading.Thread.Sleep(new Random().Next(50, 500));
                        deadlocked = true;
                    }
                    else if (e.ToString().Contains("duplicate key row"))
                    {
                        // ignore

                        Console.WriteLine(" | duplicate key");

                        return true;
                    }
                    else
                    {
                        Console.Error.WriteLine(e.ToString());
                    }
                }
            } //while

            return false;
        }

        public bool UpdateLivestreamResult(int resultID, DateTime offset, int duration, float accuracy)
        {
            bool deadlocked = true;
            while (deadlocked)
            {
                deadlocked = false;
                try
                {
                    using (MySqlConnection conn = DB_Helper.NewMySQLConnection())
                    {
                        MySqlCommand command = new MySqlCommand();
                        command.Connection = conn;

                        command.CommandText = StoredProceduresEnums.UPDATE_LIVESTREAM_RESULTS.ToString();
                        command.CommandType = CommandType.StoredProcedure;
                        command.CommandTimeout = 3000;

                        command.Parameters.Add(DatabaseArgumentEnums.parID.ToString(), MySqlDbType.Int64).Value = resultID;

                        command.Parameters.Add(DatabaseArgumentEnums.parOFFSET.ToString(), MySqlDbType.Time).Value = offset.TimeOfDay;
                        command.Parameters.Add(DatabaseArgumentEnums.parDURATION.ToString(), MySqlDbType.Int32).Value = duration;
                        command.Parameters.Add(DatabaseArgumentEnums.parACCURACY.ToString(), MySqlDbType.Float).Value = accuracy;

                        command.ExecuteNonQuery();
                        return true;
                    }
                }
                catch (Exception e)
                {
                    if (e.ToString().Contains("deadlocked"))
                    {
                        System.Threading.Thread.Sleep(new Random().Next(50, 500));
                        deadlocked = true;
                    }
                    else if (e.ToString().Contains("duplicate key row"))
                    {
                        // ignore


                        return true;
                    }
                    else
                    {
                        Console.Error.WriteLine(e.ToString());
                    }
                }
            } //while

            return false;
        }

        //Inserts the file that is analyzed into the database and uses its key to identify what segments correspond to what file in the 
        //InsertOnDemandResult and UpdateOnDemandResult methods.
        public int InsertFile(string filePath, string fileType)
        {
            bool deadlocked = true;
            while (deadlocked)
            {
                deadlocked = false;
                try
                {
                    Match regex = Regex.Match(filePath, @"(.*)\\(.+)\.(.+)$", RegexOptions.IgnoreCase);

                    // Here we check the Match instance.
                    
                        //TODO unnecessary csv, delete later.
                        var _fileName = $"{regex.Groups[2].Value}";
                        Match referenceRegex = Regex.Match(_fileName, @"^.*?(?=_)");

                        var _reference = $"{referenceRegex.Groups[0].Value}";
                        using (MySqlConnection conn = DB_Helper.NewMySQLConnection())
                        {
                            MySqlCommand command = new MySqlCommand();
                            command.Connection = conn;

                            command.CommandText = StoredProceduresEnums.INSERT_FILE.ToString();
                            command.CommandType = CommandType.StoredProcedure;
                            command.CommandTimeout = 3000;

                            command.Parameters.Add(DatabaseArgumentEnums.parFILE_PATH.ToString(), MySqlDbType.VarChar).Value = filePath;
                            command.Parameters.Add(DatabaseArgumentEnums.parFILE_TYPE.ToString(), MySqlDbType.VarChar).Value = fileType;
                            command.Parameters.Add(DatabaseArgumentEnums.parREFERENCE.ToString(), MySqlDbType.VarChar).Value = _reference;
                            

                            return Convert.ToInt32(command.ExecuteScalar());
                        }
                    
                    
                    
                    
                }
                catch (Exception e)
                {
                    if (e.ToString().Contains("deadlocked"))
                    {
                        System.Threading.Thread.Sleep(new Random().Next(50, 500));
                        deadlocked = true;
                    }
                    else if (e.ToString().Contains("duplicate key row"))
                    {
                        // ignore

                        Console.WriteLine(" | duplicate key");
                    }
                    else
                    {
                        Console.Error.WriteLine(e.ToString());
                    }
                }
            } //while

            return -1;
        }

        public bool InsertOnDemandResult(int diskotekNr, int sideNr, int sequenceNr, DateTime offset, int duration, int fileId, float accuracy, float _song_offset_seconds, out int resultID)
        {
            resultID = -1;
            bool deadlocked = true;
            while (deadlocked)
            {
                deadlocked = false;
                try
                {
                    using (MySqlConnection conn = DB_Helper.NewMySQLConnection())
                    {
                        MySqlCommand command = new MySqlCommand();
                        command.Connection = conn;

                        command.CommandText = StoredProceduresEnums.INSERT_ON_DEMAND_RESULTS.ToString();
                        command.CommandType = CommandType.StoredProcedure;
                        command.CommandTimeout = 3000;

                        command.Parameters.Add(DatabaseArgumentEnums.parDR_DISKOTEKSNR.ToString(), MySqlDbType.Int32).Value = diskotekNr;
                        command.Parameters.Add(DatabaseArgumentEnums.parSIDENUMMER.ToString(), MySqlDbType.Int32).Value = sideNr;
                        command.Parameters.Add(DatabaseArgumentEnums.parSEKVENSNUMMER.ToString(), MySqlDbType.Int32).Value = sequenceNr;

                        command.Parameters.Add(DatabaseArgumentEnums.parOFFSET.ToString(), MySqlDbType.Time).Value = offset.TimeOfDay;
                        command.Parameters.Add(DatabaseArgumentEnums.parDURATION.ToString(), MySqlDbType.Int32).Value = duration;
                        command.Parameters.Add(DatabaseArgumentEnums.parFILE_ID.ToString(), MySqlDbType.Int64).Value = fileId;

                        command.Parameters.Add(DatabaseArgumentEnums.parACCURACY.ToString(), MySqlDbType.Float).Value = accuracy;
                        command.Parameters.Add(DatabaseArgumentEnums.parSONG_OFFSET.ToString(), MySqlDbType.Float).Value = _song_offset_seconds;


                        resultID = Convert.ToInt32(command.ExecuteScalar());
                        return true;
                    }
                }
                catch (Exception e)
                {
                    if (e.ToString().Contains("deadlocked"))
                    {
                        System.Threading.Thread.Sleep(new Random().Next(50, 500));
                        deadlocked = true;
                    }
                    else if (e.ToString().Contains("duplicate key row"))
                    {
                        // ignore

                        Console.WriteLine(" | duplicate key");

                        return true;
                    }
                    else
                    {
                        Console.Error.WriteLine(e.ToString());
                    }
                }
            } //while
            return false;
        }

        public bool UpdateOnDemandResult(int resultID, int fileId, DateTime offset, int duration, float accuracy)
        {
            bool deadlocked = true;
            while (deadlocked)
            {
                deadlocked = false;
                try
                {
                    using (MySqlConnection conn = DB_Helper.NewMySQLConnection())
                    {
                        MySqlCommand command = new MySqlCommand();
                        command.Connection = conn;

                        command.CommandText = StoredProceduresEnums.UPDATE_ON_DEMAND_RESULTS.ToString();
                        command.CommandType = CommandType.StoredProcedure;
                        command.CommandTimeout = 3000;

                        command.Parameters.Add(DatabaseArgumentEnums.parID.ToString(), MySqlDbType.Int64).Value = resultID;
                        command.Parameters.Add(DatabaseArgumentEnums.parFILE_ID.ToString(), MySqlDbType.Int64).Value = fileId;

                        command.Parameters.Add(DatabaseArgumentEnums.parOFFSET.ToString(), MySqlDbType.Time).Value = offset.TimeOfDay;
                        command.Parameters.Add(DatabaseArgumentEnums.parDURATION.ToString(), MySqlDbType.Int32).Value = duration;
                        command.Parameters.Add(DatabaseArgumentEnums.parACCURACY.ToString(), MySqlDbType.Float).Value = accuracy;

                        command.ExecuteNonQuery();
                        return true;
                    }
                }
                catch (Exception e)
                {
                    if (e.ToString().Contains("deadlocked"))
                    {
                        System.Threading.Thread.Sleep(new Random().Next(50, 500));
                        deadlocked = true;
                    }
                    else if (e.ToString().Contains("duplicate key row"))
                    {
                        // ignore


                        return true;
                    }
                    else
                    {
                        Console.Error.WriteLine(e.ToString());
                    }
                }
            } //while

            return false;
        }

        public bool RemoveIntervalLivestreamResults(string channel_id, DateTime start, DateTime end)
        {
            bool deadlocked = true;
            while (deadlocked)
            {
                deadlocked = false;
                try
                {
                    using (MySqlConnection conn = DB_Helper.NewMySQLConnection())
                    {
                        MySqlCommand command = new MySqlCommand();
                        command.Connection = conn;

                        command.CommandText = StoredProceduresEnums.REMOVE_INTERVAL.ToString();
                        command.CommandType = CommandType.StoredProcedure;
                        command.CommandTimeout = 3000;

                        command.Parameters.Add(DatabaseArgumentEnums.parCHANNEL_ID.ToString(), MySqlDbType.VarChar).Value = channel_id;
                        command.Parameters.Add(DatabaseArgumentEnums.parSTART.ToString(), MySqlDbType.Timestamp).Value = start;
                        command.Parameters.Add(DatabaseArgumentEnums.parEND.ToString(), MySqlDbType.Timestamp).Value = end;

                        command.ExecuteNonQuery();
                        return true;
                    }
                }
                catch (Exception e)
                {
                    if (e.ToString().Contains("deadlocked"))
                    {
                        System.Threading.Thread.Sleep(new Random().Next(50, 500));
                        deadlocked = true;
                    }
                    else if (e.ToString().Contains("duplicate key row"))
                    {
                        // ignore


                        return true;
                    }
                    else
                    {
                        Console.Error.WriteLine(e.ToString());
                    }
                }
            } //while

            return false;
        }

        public bool GetRunningStations(out List<string[]> radios)
        {
            radios = new List<string[]>();
            bool deadlocked = true;
            while (deadlocked)
            {
                deadlocked = false;
                using (MySqlConnection conn = DB_Helper.NewMySQLConnection())
                {
                    MySqlCommand command = new MySqlCommand();
                    command.Connection = conn;

                    command.CommandText = StoredProceduresEnums.GET_RUNNING_RADIOS.ToString(); //TODO Placeholder
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandTimeout = 60;

                    MySqlDataReader reader = command.ExecuteReader();

                    while (reader.Read())
                    {
                        var channel_id = reader.GetValue(0) as string;
                        var url = reader.GetValue(1) as string;
                        radios.Add(new string[] { channel_id, url });
                    }

                }
            }
            return true;
        }

        public bool ResetCrashedTasks()
        {
            bool deadlocked = true;
            while (deadlocked)
            {
                deadlocked = false;
                try
                {
                    using (MySqlConnection conn = DB_Helper.NewMySQLConnection())
                    {
                        MySqlCommand command = new MySqlCommand();
                        command.Connection = conn;

                        command.CommandText = StoredProceduresEnums.RESET_CRASHED_TASKS.ToString(); //TODO Placeholder
                        command.CommandType = CommandType.StoredProcedure;
                        command.CommandTimeout = 3000;



                        command.ExecuteNonQuery();
                        return true;
                    }
                }
                catch (Exception e)
                {
                    if (e.ToString().Contains("deadlocked"))
                    {
                        System.Threading.Thread.Sleep(new Random().Next(50, 500));
                        deadlocked = true;
                    }
                    else if (e.ToString().Contains("duplicate key row"))
                    {
                        // ignore

                        return true;
                    }
                    else
                    {
                        Console.Error.WriteLine(e.ToString());
                    }
                }
            } //while
            return false;
        }

        public bool ResetCrashedRadioTasks()
        {
            bool deadlocked = true;
            while (deadlocked)
            {
                deadlocked = false;
                try
                {
                    using (MySqlConnection conn = DB_Helper.NewMySQLConnection())
                    {
                        MySqlCommand command = new MySqlCommand();
                        command.Connection = conn;

                        command.CommandText = StoredProceduresEnums.RESET_CRASHED_RADIO_TASKS.ToString(); //TODO Placeholder
                        command.CommandType = CommandType.StoredProcedure;
                        command.CommandTimeout = 3000;



                        command.ExecuteNonQuery();
                        return true;
                    }
                }
                catch (Exception e)
                {
                    if (e.ToString().Contains("deadlocked"))
                    {
                        System.Threading.Thread.Sleep(new Random().Next(50, 500));
                        deadlocked = true;
                    }
                    else if (e.ToString().Contains("duplicate key row"))
                    {
                        // ignore

                        return true;
                    }
                    else
                    {
                        Console.Error.WriteLine(e.ToString());
                    }
                }
            } //while
            return false;
        }

        public void GetRadios(out List<string> radios)
        {
            radios = new List<string>();
            bool deadlocked = true;
            while (deadlocked)
            {
                deadlocked = false;
                try
                {
                    using (MySqlConnection conn = DB_Helper.NewMySQLConnection())
                    {
                        var query = "SELECT DR_ID FROM stations";
                        MySqlCommand command = new MySqlCommand(query, conn);


                        using (MySqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var channel_id = reader.GetValue(0) as string;

                                radios.Add(channel_id);
                            }
                        }
                    }
                } catch (Exception e) {
                    Console.WriteLine(e.StackTrace);
                }
            }
        }
    }
}
