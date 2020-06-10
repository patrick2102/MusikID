using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RollingWindowCleaner
{
    public class RollingWindowCleaner
    {
        public static void Main(string[] args)
        {
            var path = @"\\musa01\download\ITU\MUR\RadioChannels\{0}\RollingWindow\";

            int deleteBefore = 7;

            if (args.Length > 0)
            {
                if (int.TryParse(args[0], out int days))
                {
                    deleteBefore = days;
                }
                else
                {
                    Console.WriteLine("Please add the number of days ago that you wish to delete from.");
                }
            }


            while (true)
            {
                new DatabaseCommunication.SQLCommunication().GetRadios(out List<string> radioIds);

                foreach (var radio in radioIds)
                {
                    Console.Write($"Currently cleaning: {radio}.");
                    var _path = string.Format(path, radio);
                    //Task.Run (() => RemoveRollingWindow(_path, deleteBefore));
                    RemoveRollingWindow(_path, deleteBefore);
                    Thread.Sleep(100);
                }

                Thread.Sleep(60 * 60 * 1000);
            }

        }

        public static void RemoveRollingWindow(string path, int daysBefore)
        {
            if (!Directory.Exists(path)) {
                Directory.CreateDirectory(path);
            }

            DirectoryInfo d = new DirectoryInfo(path);

            var now = DateTime.Now;

            var files = new ConcurrentQueue<FileInfo>(d.GetFiles().Where(x => (now - x.CreationTime).Days > daysBefore));

            var num = files.Count();

            Console.WriteLine($" Total files: {num}. \n");

            int n = 0;

            Parallel.ForEach(files, file => {
                try
                {
                    Console.Write($"\r{num-n} files remaining.");
                    file.Delete();
                    Interlocked.Increment(ref n);
                } catch (Exception)
                {
                    return;
                }
            });
            Console.WriteLine($"\n");
        }
    }
}
