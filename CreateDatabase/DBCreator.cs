using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CDR;
using MySql.Data.MySqlClient;

namespace CreateDatabase
{
    class DBCreator
    {
        public void Run()
        {
            CDR.DB_Helper.Initialize();

            Console.WriteLine("Connecting to MYSQL database.");

            string server = "localhost";
            string database = "drfingerprints";
            string uid = "root";
            string password = "";
            string connectionString;
            connectionString = "SERVER=" + server + ";" + "DATABASE=" + database + ";" + "UID=" + uid + ";" + "PASSWORD=" + password + ";";

            string path = Path.GetFullPath(@"..\..\..\DatabaseScripts\1.CreateDatabase.sql");

            Console.WriteLine();
            Console.WriteLine("Do you want to create the database? y/n");
            AskYesNo();
            Console.WriteLine();

            ExecuteScript(path, connectionString);
        }

        public void ExecuteScript(string path, string connectionString)
        {

            MySqlConnection conn = null;

            conn = new MySqlConnection(connectionString);
            conn.Open();

            if (conn == null)
            {
                Console.WriteLine("Connection failed.");
                return;
            }


            if (!File.Exists(path))
            {
                Console.WriteLine(string.Format("Can't find Create database script : {0}", Path.GetFileName(path)));
                return;
            }
            var text = File.ReadAllText(path);
            MySqlScript script = new MySqlScript(conn, File.ReadAllText(path));

            script.Execute();

            Console.WriteLine("Database created!");
        }

        private bool AskYesNo()
        {
            while (true)
            {
                ConsoleKeyInfo key = Console.ReadKey(true);
                if (char.ToUpper(key.KeyChar) == 'Y')
                {
                    Console.WriteLine("Y");
                    return true;
                }
                else if (char.ToUpper(key.KeyChar) == 'N')
                {
                    Console.WriteLine("N");
                    return false;
                }
            }
        }

        /// <summary>
        /// Simple MySQLDump parser. Not all cases are supported.
        /// </summary>
        //private bool RestoreFromMySQLDump(string filename)
        //{
        //    if (File.Exists(filename))
        //    {
        //        long filesize = new System.IO.FileInfo(filename).Length;
        //        int countSkipStep = 1;
        //        // guess number of lines based on filesize
        //        if ((filesize / 80) > 10000)
        //        {
        //            //countSkipStep = 1000;
        //        }

        //        using (MySqlConnection conn = CDR.DB_Helper.NewMySQLConnection())
        //        {
        //            using (StreamReader sr = new StreamReader(filename))
        //            {
        //                StringBuilder sb = new StringBuilder();
        //                String line;
        //                long count = 0;
        //                try
        //                {
        //                    while ((line = sr.ReadLine()) != null)
        //                    {
        //                        // ignore comments
        //                        if (line.Length >= 2 && line.Substring(0, 2) == "--")
        //                        {
        //                            continue;
        //                        }
        //                        else if (line.Length >= 2 && (line.Substring(0, 2) == "//" || line.Substring(0, 2) == "/*" || line.Substring(0, 2) == "*/"))
        //                        {
        //                            continue;
        //                        }
        //                        else if (line.Length > 6 && line.Substring(0, 3) == "/*!" && line.Substring(line.Length - 4, 3) == "*/;")
        //                        {
        //                            // Special command probally
        //                            line = line.Replace("/*!", "").Replace("*/;", "");
        //                            int p = line.IndexOf(' ');
        //                            if (p > 0)
        //                            {
        //                                line = line.Substring(p).Trim();
        //                                if (!Exec_MySQL_Text(conn, line))
        //                                {
        //                                    Console.WriteLine();
        //                                    Console.WriteLine("Error while restoring. Script stopped!");
        //                                    return false;
        //                                }
        //                            }
        //                        }
        //                        else if (line.Length > 0)
        //                        {
        //                            if (line.Substring(0, 12) == "INSERT INTO ")
        //                            {
        //                                if (sb.Length > 0)
        //                                {
        //                                    Exec_MySQL_Text(conn, sb);
        //                                    sb.Clear();
        //                                }

        //                                Exec_MySQL_Text(conn, line);
        //                            }
        //                            else
        //                            {
        //                                sb.Append(line);
        //                            }
        //                        }
        //                        else if (line.Length > 0 && sb.Length > 0)
        //                        {
        //                            Exec_MySQL_Text(conn, sb);
        //                            sb.Clear();
        //                        }

        //                        count++;
        //                        if (count % countSkipStep == 0)
        //                        {
        //                            Console.Write(string.Format("\rLine #{0:0000000000}", count));
        //                        }
        //                    }
        //                }
        //                finally
        //                {
        //                    Console.WriteLine(string.Format("\rLine #{0:0000000000}", count));
        //                }
        //            } // using Streamreader
        //        } //using Conn

        //        return true;
        //    }

        //    return false;
        //}

        /// <summary>
        /// Reads entire script and dumps it to MySQL
        /// </summary>
        //private bool RestoreFromScript(string filename)
        //{
        //    if (File.Exists(filename))
        //    {
        //        using (MySqlConnection conn = CDR.DB_Helper.NewMySQLConnection())
        //        {
        //            StringBuilder sb = new StringBuilder();
        //            using (StreamReader sr = new StreamReader(filename))
        //            {
        //                String line;
        //                long count = 0;
        //                try
        //                {
        //                    while ((line = sr.ReadLine()) != null)
        //                    {
        //                        sb.Append(line);
        //                        count++;
        //                        Console.Write(string.Format("\rLine #{0:0000000000}", count));
        //                    } //while
        //                }
        //                finally
        //                {
        //                    Console.WriteLine(string.Format("\rLine #{0:0000000000}", count));
        //                }
        //            } // using Streamreader

        //            Exec_MySQL_Text(conn, sb);
        //        } //using Conn

        //        return true;
        //    }

        //    return false;
        //}
    }
}
