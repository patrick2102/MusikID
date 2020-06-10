using Framework;
using Framework.DTO;
using MatchAudio;
using Newtonsoft.Json.Linq;
using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Worker
{
    public class Worker
    {
        readonly static private string sharedPathForRadioChannels = @"\\musa01\download\ITU\MUR\RadioChannels\";

        DrRepository _repo;

        public Worker()
        {
            _repo = new DrRepository(new drfingerprintsContext());
        }
        public async void Start()
        {
            while (true)
            {
               // var od_results = _repo.GetODFileResultsByID(2510, true);
                new SQLCommunication().GetTask(out TaskQueue task);
                //var task = new TaskQueue() {TaskType = TaskType.AudioMatch.ToString(), Arguments = @"\\musa01\Download\ITU\MUR\Dropfolder\120220 MIXTAPE ANDERS VALENTINES DONE_1492473_2020-02-12T13-08-58.000.wav", JobId = 411 };
                if (task != null)
                {
                   await ExecuteAssignment(task);

                   await CleanUp(task);
                    break;
                }
                else // if no new assignments where found then sleep before trying again.  Done in order to avoid too many queries.
                {
                    Thread.Sleep(2000);
                }
            }
        }

        private async Task<bool> ExecuteAssignment(TaskQueue ass)
        {
            string audio_path;
            string[] arg_splits;
            AudioMatcher am;
            switch (ass.TaskType)
            {
                case nameof(TaskType.AudioMatch):
                    var file_id = _repo.GetODFileByJobID(ass.JobId).Id;
                    await _repo.DeleteResults(file_id);
                    

                    am = new AudioMatcher(12);
                    audio_path = ass.Arguments;
                    try
                    {
                        am.Match(_repo, audio_path, ass.JobId, (int)file_id);
                    }
                    catch (Exception e)
                    {
                        var msg = e.Message;
                        var _repo = new DrRepository(new drfingerprintsContext());

                        switch (msg)
                        {
                            case "not_supported":
                                await _repo.UpdateJobStatus(ass.JobId, msg);
                                break;
                            case "ffmpeg_fail":
                                await _repo.UpdateJobStatus(ass.JobId, msg);
                                break;
                            default:
                                await _repo.UpdateJobStatus(ass.JobId, "unknown");
                                break;
                        }
                    }
                    var od_results = _repo.GetODFileResultsByID((int)file_id, true);
                    var json = Newtonsoft.Json.JsonConvert.SerializeObject(od_results);  
                    
                    string path = $@"\\musa01\download\ITU\MUR\JSONResults\{file_id}.json";
                    // This text is added only once to the file.
                    if (!File.Exists(path))
                    {
                        File.WriteAllText(path, json);
                    }
                    break;
                case nameof(TaskType.CheckFiles):
                    var filePath = ass.Arguments;
                    var fileId = _repo.GetODFileByJobID(ass.JobId).Id;
                    Match regex = Regex.Match(ass.Arguments, @".*\\(.+)\\.+$", RegexOptions.IgnoreCase);
                    var folderName = regex.Groups[1];
                    // run audio match on file
                    am = new AudioMatcher(6);
                    am.Match(_repo, filePath, (int) ass.JobId, (int) fileId);
                    
                    // now get json file for CheckFiles and update it
                    var jsonStringResults = new WebClient().DownloadString($"http://itumur-api01:8080/api/files/{fileId}/results?filters=true");

                    //var checkFilesResult = Newtonsoft.Json.JsonConvert.DeserializeObject<CheckFilesResult>(jsonString);

                    // now parse results, and check if the file was found
                    var found = false;

                    JObject o = JObject.Parse(jsonStringResults);
                    JObject f = (JObject)o["file"];
                    JArray results = (JArray)o["results"];
                    string reference = null;
                    double duration = TimeSpan.Parse((string)f["file_duration"]).TotalSeconds;
                    foreach (var result in results)
                    {
                        var resultDuration = (double)result["duration"];
                        if (resultDuration > (duration * 0.8))
                        {
                            reference = (string) result["reference"];
                            found = true;
                            break;
                        }
                    }

                    /*
                    var directory = $@"\\musa01\download\ITU\MUR\CheckFilesResult\{folderName}";
                    if (!Directory.Exists(directory))
                        Directory.CreateDirectory(directory);
                        */

                    string path_to_result_file = $@"\\musa01\download\ITU\MUR\CheckFilesResult\{folderName}\CheckFileResults.json";
                    /*
                    JArray jArr = new JArray();
                    JValue jFilePath = new JValue($"{filePath}");
                    JValue jFound = new JValue($"{found}");
                    jArr.Add(jFilePath);
                    jArr.Add(jFound);
                    string json3 = jArr.ToString();
                    json3 = json3 + ",\n";
                    

                    if (!File.Exists(path2))
                    {
                        var file = File.Create(path2);
                        file.Close();
                    }
                    */

                    FileInfo fileInfo = new FileInfo(path_to_result_file);
                    while (IsFileLocked(fileInfo))
                    {
                        Thread.Sleep(10);
                    }

                    var checkFilesResultString = File.ReadAllText(path_to_result_file);

                    var checkFilesResult = Newtonsoft.Json.JsonConvert.DeserializeObject<CheckFilesResult>(checkFilesResultString);

                    checkFilesResult.file_completed_count++;

                    var file_result = new FileResult() { file_path = filePath, found = found, reference = reference };

                    checkFilesResult.file_results.Add(file_result);

                    var s = Newtonsoft.Json.JsonConvert.SerializeObject(checkFilesResult);

                    while (IsFileLocked(fileInfo))
                    {
                        Thread.Sleep(10);
                    }

                    File.WriteAllText(path_to_result_file, s);
                    break;
                case nameof(TaskType.RollingWindow):
                    am = new AudioMatcher(6);
                    arg_splits = ass.Arguments.Split(' ');

                    var format = "yyyy-MM-ddTHH:mm";
                    var provider = new CultureInfo("fr-FR");
                    DateTime start = DateTime.ParseExact(arg_splits[0], format, provider);
                    DateTime end = DateTime.ParseExact(arg_splits[1], format, provider);
                    string channel = arg_splits[2].Trim();
                    am.MatchRollingWindowAsync(_repo, sharedPathForRadioChannels, start, end, channel, (int) ass.JobId);
                    break;


                default:
                    break;
            }
            return true;
        }

        protected static bool IsFileLocked(FileInfo file)
        {
            try
            {
                using (FileStream stream = file.Open(FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                {
                    stream.Close();
                }
            }
            catch (IOException)
            {
                //the file is unavailable because it is:
                //still being written to
                //or being processed by another thread
                //or does not exist (has already been processed)
                return true;
            }
            //file is not locked
            return false;
        }
        private async Task<bool> CleanUp(TaskQueue ass)
        {
            switch (ass.TaskType)
            {
                case nameof(TaskType.AudioMatch):
                    await _repo.DeleteTask(ass.Id);
                    break;

                case nameof(TaskType.RollingWindow):
                    await _repo.DeleteTask(ass.Id);
                    break;

                case nameof(TaskType.CheckFiles):
                    await _repo.DeleteTask(ass.Id);
                    break;

                default:
                    await _repo.DeleteTask(ass.Id);
                    break;
            }
            return true;
        }
    }
}
