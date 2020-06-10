
using Framework;
using Framework.DTO;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TaskQueueWatchDog
{
    public class TaskQueueWatchDog
    {

        DrRepository repo;

        public TaskQueueWatchDog()
        {
            this.repo = new DrRepository(new drfingerprintsContext());
        }

        public void Monitor()
        {
            var time = DateTime.Now.ToString("HH");

            while (true)
            {
                //Looks to see if there are any crashed tasks
                Console.WriteLine("checking for failed tasks");
                repo.CheckForFailedTasks();
                //new SQLCommunication().ResetCrashedRadioTasks();
                var begin = DateTime.Now.ToString("yyyy-MM-dd");
                if (time != DateTime.Now.ToString("HH"))
                {
                    //Saves all live_stream results every hour.
                    Console.WriteLine("Saving results from radio");
                    SaveAllRadioResults(begin, time);                
                    time = DateTime.Now.ToString("HH");
                }
                //Deletes all livestream and OD-results older than a week. 
                //Checks every hour.
                //All results are saved as JSON files in
                // \\musa01\Download\ITU\MUR\JSONResults & \\musa01\Download\ITU\MUR\JSONResults_Livestream
                DeleteAllResultsWithinTimespan(7); //Default 7 days
                //}
                Thread.Sleep(10000);
            }
        }


        //TODO the delete can take a lot of time if there is a lot of old results,
       // with 20 mil old rows in livestream_results the watchdog crashes due to timeout to the DB.
        private void DeleteAllResultsWithinTimespan(int days)
        {
            repo.DeleteLiveStreamResultsOlderThan(days);
            repo.DeleteODResultsOlderThan(days);
        }

        public void SaveAllRadioResults(string begin, string lastHour)
        {
            var channels = repo.GetStationIds();

            foreach (var channel in channels)
            {
                var currentDT = DateTime.Now.ToString("HH");
                var json = new WebClient().DownloadString($"http://itumur-api01:8081/api/channels/{channel}/results?filters=true&begin={begin}T{lastHour}:00&end={begin}T{currentDT}:00");
                var res = JsonConvert.DeserializeObject<LivestreamResultsDTO>(json);
               
                if (res.results.Count == 0){
                    continue;
                }

                var currentDate = DateTime.Now.ToString("yyyy-MM-dd");
                if (!Directory.Exists($@"\\musa01\download\ITU\MUR\JSONResults_Livestream\{channel}\"))
                {
                    Directory.CreateDirectory($@"\\musa01\download\ITU\MUR\JSONResults_Livestream\{channel}\");
                }
                string savePath = $@"\\musa01\download\ITU\MUR\JSONResults_Livestream\{channel}\{begin}T{lastHour}_{currentDate}T{DateTime.Now.ToString("HH")}.json";

                //string path = $@"\\musa01\download\ITU\MUR\JSONResults_Livestream\{channel}_{begin}T{time}-{channel}_{begin}T{DateTime.Now.TimeOfDay.Hours}.txt";
                // This text is added only once to the file.
                if (!File.Exists(savePath))
                {
                    File.WriteAllText(savePath, json);
                    Console.WriteLine("Saved last hour to JSON for: " + channel);
                }
            }
        }
    }
}