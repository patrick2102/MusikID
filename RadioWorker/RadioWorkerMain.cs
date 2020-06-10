using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Framework;
using RadioWorker;

namespace RadioWorker
{
    public class RadioWorkerMain
    {
        public static void Main(string[] args) {

            var capacity = 1;

            var threads = new BlockingCollection<Task>(capacity);

            while (true)
            {
                while (threads.Count >= capacity)
                {
                    foreach (var task in threads)
                    {
                        if (task.IsCompleted)
                            threads.TryTake(out Task res);
                    }
                    Thread.Sleep(20);
                }

                new SQLCommunication().GetRadioTask(out RadioTaskQueue rass);

                if (rass != null)
                {
                    threads.TryAdd(Task.Run(() => new RadioWorker(rass)));
                    Thread.Sleep(10);
                    continue;
                } 
                
                Thread.Sleep(100);
            }

        }
    }
}
