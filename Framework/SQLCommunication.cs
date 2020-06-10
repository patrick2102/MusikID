using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;


namespace Framework
{
    /*
     * This class is used for accessing data in the database. All functions return a bool, which describes if the actions were successful.
     * If additional information is needed from the query, then it is returned with the "out" variables.
     * 
     */


    public class SQLCommunication
    {


        //TODO not used anymore ?
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

                        trackID = (int)command.ExecuteScalar();

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
        #region Get Tasks queries
        //using SQL direct for getting task to be able to do transactions to ensure that only one workers gets one task at a time.
        public bool GetRadioTask(out RadioTaskQueue ass)
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

                        //command.Parameters.Add(DatabaseArgumentEnums.parMACHINE.ToString(), MySqlDbType.VarChar).Value = Environment.MachineName;

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
                                ass = new RadioTaskQueue()
                                {
                                    ChunkPath = chunk_path,
                                    ChannelId = channel_id,
                                    Id = id,
                                    JobId = Job_Id,
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
        public bool GetTask(out TaskQueue ass)
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
                                var taskType = data_reader.GetValue(1) as string;
                                var Arguments = data_reader.GetValue(2) as string;
                                var Job_Id = (int)Convert.ToInt64(data_reader.GetValue(3));
                                var file_id = (int)Convert.ToInt64(data_reader.GetValue(4));
                                ass = new TaskQueue()
                                {
                                    Arguments = Arguments,
                                    JobId = Job_Id,
                                    Id = id,
                                    TaskType = taskType,
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
        //using SQL direct for getting task to be able to do transactions to ensure that only one workers gets one taskat a time. 
        public bool GetFingerTask(out FingerTaskQueue ass)
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
                                var rawType = (data_reader.GetValue(1) as string);
                                TaskType taskType;
                                if (rawType.Equals("Fingerprint")) taskType = TaskType.CreateFingerprint;
                                else taskType = (TaskType)Enum.Parse(typeof(TaskType), data_reader.GetValue(1) as string);
                                var Arguments = data_reader.GetValue(2) as string;
                                var Job_Id = (int)Convert.ToInt64(data_reader.GetValue(3));
                                var file_id = (int)Convert.ToInt64(data_reader.GetValue(4));
                                ass = new FingerTaskQueue()
                                {
                                    Arguments = Arguments,
                                    JobId = Job_Id,
                                    Id = id,
                                    TaskType = taskType.ToString(),
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
            } //whiles
            ass = null;
            return false;
        }
        #endregion
    }
}
