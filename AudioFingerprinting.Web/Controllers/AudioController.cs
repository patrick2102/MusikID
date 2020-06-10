using AudioFingerprint;
using BusinessRules;
using DatabaseCommunication;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;


namespace AudioFingerprinting.Web.Controllers
{
    public class Radio
    {
        public string ID { get; set; }
        public string URL { get; set; }

        public Radio(string id, string url) { ID = id; URL = url; }

        public bool isInvalid()
        {
            return ID == null || URL == null;
        }
    }

    public class StartRadioRequest {
        public string Id { get; set; }

        public int SegmentDuration { get; set; }

        public int OverlapDuration { get; set; }

        public StartRadioRequest(string _id, int _segmentDuration = 6, int _overlapDuration = 2)
        {
            Id = _id;
            SegmentDuration = _segmentDuration;
            OverlapDuration = _overlapDuration;
        }
    }

    public class LivestreamRequest
    {
        public string StartTime { get; set; }

        public string EndTime { get; set; }

        public string ChannelID { get; set; }

        public string Arguments { get; set; }

        public LivestreamRequest(string start, string end, string radioID, string arguments) { StartTime = start; EndTime = end; ChannelID = radioID; Arguments = arguments; }
    }

    public class OnDemandRequest
    {
        public int FileID { get; set; }

        public string Arguments { get; set; }

        public OnDemandRequest(int fileID, string arguments) { FileID = fileID; Arguments = arguments; }
    }

    public class RollingWindow
    {
        public string StartTime { get; set; }

        public string EndTime { get; set; }
        

        public RollingWindow(string start, string end, string radioID) { StartTime = start; EndTime = end;  }

        public bool isInvalid()
        {
            return EndTime == null || StartTime == null;
        }

    }

    // [Route("api/[controller]")]
    [Route("api/")]
    [ApiController]
    public class AudioController : ControllerBase
    {
        static Dictionary<string, Process> radioMonitors = new Dictionary<string, Process>();

        private List<string> formatsSupported = Enum.GetNames(typeof(SupportedAudioFormats)).ToList(); //TODO maybe bad style.

        public class FingerprintRequest
        {
            public string path;

            public bool force = false;
            
        }

        [Route("tracks")]
        [HttpPost]
        public ActionResult PostTrack([FromBody] FingerprintRequest req)
        {

            var songPath = req.path;

            var (audioName, code) = ExtractAudioNameFromPath(songPath);

            // make more test on the request... check if extension is correct for example.
            if (!System.IO.File.Exists(songPath)) return BadRequest("Can't find file.");

            var comm = new SQLCommunication();

            var extension = Path.GetExtension(songPath);
            extension = extension.Substring(1, extension.Length-1);
            var file_id = new SQLCommunication().InsertFile(audioName, extension);

            comm.InsertJob(JobType.Fingerprint.ToString(), DateTime.Now, 0, file_id, $"", out int jobID);

            // add task to queue.

            var type = TaskType.Fingerprint;

            var arguments = $"\"{songPath}\" {(req.force ? "true" : "false")} false";

            var res = comm.InsertFingerTask(type, arguments, jobID, out int task_id);

            if (res)
            {
                return Created($"api/Audio/jobs/{jobID}", jobID);
            }
            else
            {
                return StatusCode(500);
            }
        }
        [Route("tracks")]
        [HttpGet]
        public ActionResult GetTracks([FromQuery(Name = "limit")] int limit = 100)
        {
            new SQLCommunication().GetTracks(limit, out List<SQLTrack> files);
            

            var json = JsonConvert.SerializeObject(files.ToArray());

            return Ok(json);
        }

        [Route("tracks/{diskoNummer}")]
        [HttpGet]
        public ActionResult GetTrack(string diskoNummer)
        {
            new SQLCommunication().GetTrackWithDiskoNumber(diskoNummer, out List<SQLTrack> tracks);


            var json = JsonConvert.SerializeObject(tracks.FirstOrDefault());

            return Ok(json);
        }

        public class FilesPostRequest
        {
            public string audioPath { get; set; }

            public string user { get; set; }
        }

        //HER api/tracks/{diskoteksnummer}/indexTrack
        [Route("tracks/{diskoNummer}/indexTrack")]
        [HttpPost]
        public ActionResult IndexTrack(string diskoNummer)
        {
            new SQLCommunication().GetTrackWithDiskoNumber(diskoNummer, out List<SQLTrack> tracks);
            if (tracks.Count == 0)
                return NotFound("did not find the track by diskoNummer");
            var song = tracks.FirstOrDefault();

            var sql = new SQLCommunication();
            sql.InsertJob("index",DateTime.Now, 0,-1,"", out int jobid);
            var res = sql.InsertFingerTask(TaskType.IndexSingle, song.id + "", jobid, out int taskId);

            if (res)
                return Ok("Job created under id:" + jobid);
            else
                return NotFound("Something went wrong, database error");
        }

        // POST api/file/
        [Route("files")]
        [HttpPost]
        public ActionResult PostAudio([FromBody] FilesPostRequest req)
        {
            if (!System.IO.File.Exists(req.audioPath)) return BadRequest("Can't find file.");

            var extension = Path.GetExtension(req.audioPath);

            var comm = new SQLCommunication();

            var file_id = comm.InsertFile(req.audioPath, extension);

            comm.InsertJob(JobType.AudioMatch.ToString(), DateTime.Now, 0, file_id, $"", out int jobID, req.user);

            // add task to queue.

            var type = TaskType.AudioMatch;

            var arguments = req.audioPath;

            var res = comm.InsertTask(type, arguments, jobID, out int task_id);

            if (res)
            {
                return Created($"http://ITUMUR02:8080/api/Audio/results/on_demand/{file_id}", file_id);
            }
            else
            {
                return StatusCode(500);
            }
        }


        [Route("files")]
        [HttpGet]
        public ActionResult<IEnumerable<Result>> GetODFiles([FromQuery(Name = "limit")] int limit = 100)
        {
            
            new SQLCommunication().GetOnDemandFile(limit, out List<OnDemandFile> files);

            files = files.ToList();

            

            var json = JsonConvert.SerializeObject(files.ToArray());

            return Ok(json);
        }

        [Route("files/{id}")]
        [HttpGet]
        public ActionResult<IEnumerable<Result>> GetODFile(int id)
        {
            var file_model = new FileReturnModel();
            file_model.result_type = result_types.OnDemand.ToString();
            new SQLCommunication().GetOnDemandFile(id, out List<OnDemandFile> files);
            var file = files.FirstOrDefault();

            if (file == null) return Ok(file);

            new SQLCommunication().GetJob(file.file_id, out Job job);
            new SQLCommunication().GetFile(file.file_id, out SQLFile SQLfile);

            if (job != null)
            {
                file_model.percentage = job.percentage;
                file_model.job_finished = (job.percentage == 100 ? true : false);
                file_model.created = job.start_time;
                file_model.time_used = job.last_updated.Subtract(job.start_time);
                file_model.user = job.user;
            }

            file_model.file_path = SQLfile?.path;
            file_model.file_duration = SQLfile == null ? new TimeSpan(0) : SQLfile.file_duration;

            
            var json = JsonConvert.SerializeObject(file_model);

            return Ok(json);
        }

        // GET api/file/{file_id}
        [Route("files/{id}/results")]
        [HttpGet]
        public ActionResult<IEnumerable<Result>> GetFile(int id, [FromQuery(Name = "filters")] bool filters = true)
        {
            var model = new ResultsReturnModel();
            model.file = new FileReturnModel();
            model.file.result_type = result_types.OnDemand.ToString();
            new SQLCommunication().GetJob(id, out Job job);
            new SQLCommunication().GetFile(id, out SQLFile SQLfile);

            var path = $@"\\musa01\download\ITU\MUR\JSONResults\{id}.txt";

            if (!System.IO.File.Exists(path))
            {
                if (job != null)
                {
                    model.file.percentage = job.percentage;
                    model.file.job_finished = (job.percentage == 100 ? true : false);
                    model.file.created = job.start_time;
                    model.file.time_used = job.last_updated.Subtract(job.start_time);
                    model.file.user = job.user;
                    if (job.percentage == 0) model.file.estimated_time_of_completion = DateTime.MinValue;
                    else model.file.estimated_time_of_completion = DateTime.Now.AddTicks((long)(model.file.time_used.Ticks * (100 / job.percentage))).AddTicks(-model.file.time_used.Ticks);

                }
                model.file.file_path = SQLfile?.path;
                model.file.file_duration = SQLfile == null ? new TimeSpan(0) : SQLfile.file_duration;


                var rules = new RuleParser().Parse("all_filters=true");

                new SQLCommunication().GetOnDemandResults(id, out List<Result> results);

                IEnumerable<Result> lst;

                if (filters == true)
                {
                    lst = new RuleApplier(rules).ApplyRules(results);
                }
                else if (filters == false)
                {
                    lst = new RuleApplier(new List<IBusinessRule> { new DRInfoRule() }).ApplyRules(results);
                }
                else
                {
                    lst = new RuleApplier(rules).ApplyRules(results);
                }

                var result_dtos = lst.Select(r => new ResultDTO(r.GetStartTime(), r.GetEndTime(), r._reference, r.title, r.artists, r.GetAccuracy()));

                if (result_dtos == null)
                {
                    result_dtos = new List<ResultDTO> { };
                }

                model.results = result_dtos.ToArray();

                var json = JsonConvert.SerializeObject(model);
                return Ok(json);
            }
            var jsonFromFile = System.IO.File.ReadAllText(path);
            return Ok(jsonFromFile);

        }

        [Route("channels")]
        [HttpPost]
        public ActionResult<IEnumerable<Result>> AddRadio(string id, string URL)
        {
            var res = new SQLCommunication().InsertStation(id, URL, out Station station);


            var json = JsonConvert.SerializeObject(station);

            return Created($"http:ITUMUR02:8080/api/channels/{station.DR_ID}/", json);
        }



        [Route("channels/{id}/start")]
        [HttpPost]
        public ActionResult<IEnumerable<Result>> StartMonitoringChannel(string id)
        {
            new SQLCommunication().UpdateStation(id, true, out List<Station> lst);


            var json = JsonConvert.SerializeObject(lst);

            return Ok(json);
        }

        [Route("channels/{id}/stop")]
        [HttpPost]
        public ActionResult<IEnumerable<Result>> StopMonitoringChannel(string id)
        {
            new SQLCommunication().UpdateStation(id, false, out List<Station> lst);


            var json = JsonConvert.SerializeObject(lst);

            return Ok(json);
        }

        [Route("channels/{id}/rematch")]
        [HttpPost]
        public ActionResult<IEnumerable<Result>> RollingWindowAnalyze(string id, RollingWindow window)
        {
            DateTime startTime, endTime;
            if (window.isInvalid())
                return BadRequest("Some values where null");
            try
            {
                startTime = DateTime.Parse(window.StartTime);
                endTime = DateTime.Parse(window.EndTime);
            }
            catch (FormatException)
            {
                return BadRequest("could not convert strings to dateTimes");
            }


            var comm = new SQLCommunication();


            comm.InsertJob(JobType.RollingWindow.ToString(), DateTime.Now, 0, -1, $"", out int jobID);

            // add task to queue.

            var type = TaskType.RollingWindow;

            var arguments = $"{window.StartTime.ToString().Replace(" ", "-")} {window.EndTime.ToString().Replace(" ", "-")} {id}";

            var res = comm.InsertTask(type, arguments, jobID, out int task_id);

            var startTimeStr = startTime.ToString("yyyy-MM-ddTHH:mm:ss.fff") + "Z";
            var endTimeStr = endTime.ToString("yyyy-MM-ddTHH:mm:ss.fff") + "Z";

            if (res)
            {
                return Created($"http://ITUMUR02:8080/api/Audio/results/live_stream/{id}/{startTimeStr}/{endTimeStr}", jobID);
            }
            else
            {
                return StatusCode(500);
            }
        }


        [Route("channels")]
        [HttpGet]
        public ActionResult<IEnumerable<Result>> GetAllChannels()
        {

            new SQLCommunication().GetStations(out List<Station> stations);

            var json = JsonConvert.SerializeObject(stations);

            return Ok(json);
        }

        // GET api/Audio/results/live_stream
        [Route("channels/{id}")]
        [HttpGet]
        public ActionResult<IEnumerable<Result>> GetChannel(string id, [FromQuery(Name = "filters")] bool filters = true)
        {
            new SQLCommunication().GetStations(out List<Station> stations);

            var json = JsonConvert.SerializeObject(stations.Where(s => s.DR_ID == id).FirstOrDefault());

            return Ok(json);
        }


        // GET api/channels/{id}/results
        [Route("channels/{id}/results")]
        [HttpGet]
        public ActionResult<IEnumerable<Result>> GetLSResultsFiltered(string id, [FromQuery(Name = "filters")] bool filters = true, [FromQuery(Name = "begin")] string begin = null, [FromQuery(Name = "end")] string end = null)
        {
            var path = $@"\\musa01\Download\ITU\MUR\JSONResults_Livestream\{id}_{begin}-{id}_{end}.txt";
            if (!System.IO.File.Exists(path))
            {


                var model = new ResultsReturnModelLiveStream();
                model.result_type = result_types.Livestream.ToString();
                model.channel = id;

                CultureInfo provider = CultureInfo.InvariantCulture;
                DateTime begin_date, end_date;


                if (begin == null)
                {
                    begin_date = DateTime.Now.AddHours(-1);
                }
                else
                {
                    begin_date = DateTime.ParseExact(begin, "yyyy-MM-ddTHH", provider);
                }
                if (end == null)
                {
                    end_date = DateTime.Now;
                }
                else
                {
                    end_date = DateTime.ParseExact(end, "yyyy-MM-ddTHH", provider);
                }

                if (begin_date > end_date) return BadRequest("Begin Date cannot be before end.");


                var rules = new RuleParser().Parse("all_filters=true");

                new SQLCommunication().GetLivestreamResults(begin_date, end_date, id, out List<Result> results);

                IEnumerable<Result> lst;

                if (filters == true)
                {
                    lst = new RuleApplier(rules).ApplyRules(results);
                }
                else if (filters == false)
                {
                    lst = new RuleApplier(new List<IBusinessRule> { new DRInfoRule() }).ApplyRules(results);
                }
                else
                {
                    lst = new RuleApplier(rules).ApplyRules(results);
                }

                var result_dtos = lst.Select(r => new ResultDTO(r.GetStartTime(), r.GetEndTime(), r._reference, r.title, r.artists, r.GetAccuracy()));

                model.results = result_dtos.ToArray();

                var json = JsonConvert.SerializeObject(model);
                return Ok(json);
            }
            else
            {
                var jsonFromFile = System.IO.File.ReadAllText(path);
                return Ok(jsonFromFile);
            }
            
        }



        /*
        // POST api/file/{file_id}
        [Route("file/{file_id}/no_filters")]
        [HttpGet]
        public ActionResult<IEnumerable<Result>> GetFilesNoFilters(int file_id)
        {
            var model = new ResultsReturnModel();
            model.result_type = result_types.OnDemand.ToString();
            new SQLCommunication().GetJob(file_id, out Job job);
            var file = new SQLCommunication().GetFile(file_id, out SQLFile SQLfile);

            model.percentage = job.percentage;
            model.file_path = SQLfile.path;
            model.date = job.start_time;
            model.job_finished = (job.percentage == 100 ? true : false);


            new SQLCommunication().GetOnDemandResults(file_id, out List<Result> results);

            var lst = new RuleApplier(new List<IBusinessRule> { new DRInfoRule() }).ApplyRules(results);


            var result_dtos = lst.Select(r => new ResultDTO(r.GetStartTime(), r.GetEndTime(), r._reference, r.title, r.artists, r.GetAccuracy()));

            if (result_dtos == null)
            {
                result_dtos = new List<ResultDTO> { };
            }

            model.results = result_dtos.ToArray();

            var json = JsonConvert.SerializeObject(model);

            return Ok(json);
        }
        */

        /*

    // POST api/Audio/rolling
    [Route("rolling")]
    [HttpPost]
    public ActionResult RollingWindow([FromBody] RollingWindow window)
    {
        DateTime startTime, endTime;
        if (window.isInvalid())
            return BadRequest("Some values where null");
        try
        {
            startTime = DateTime.Parse(window.StartTime);
            endTime = DateTime.Parse(window.EndTime);
        }
        catch (FormatException)
        {
            return BadRequest("could not convert strings to dateTimes");
        }


        var comm = new SQLCommunication();


        comm.InsertJob(JobType.RollingWindow.ToString(), DateTime.Now, 0, -1, $"", out int jobID);

        // add task to queue.

        var type = TaskType.RollingWindow;

        var arguments = $"{window.StartTime.ToString().Replace(" ", "-")} {window.EndTime.ToString().Replace(" ", "-")} {window.RadioID}";

        var res = comm.InsertTask(type, arguments, jobID, out int task_id);

        var startTimeStr = startTime.ToString("yyyy-MM-ddTHH:mm:ss.fff") + "Z";
        var endTimeStr = endTime.ToString("yyyy-MM-ddTHH:mm:ss.fff") + "Z";

        if (res)
        {
            return Created($"http://ITUMUR02:8080/api/Audio/results/live_stream/{window.RadioID}/{startTimeStr}/{endTimeStr}", jobID);
        }
        else
        {
            return StatusCode(500);
        }
    }

    // POST api/Audio/stopallradios
    [Route("stopallradios")]
    [HttpGet]
    public ActionResult StopAllRadios()
    {
        List<Process> radio_processes = Process.GetProcessesByName("RadioChannel").ToList();
        foreach (Process p in radio_processes)
        {
            p.Kill();
        }
        radioMonitors.Clear();


        List<Process> proc = Process.GetProcessesByName("ffmpeg").ToList();
        foreach (Process p in proc)
        {
            p.Kill();
        }

        return Ok("All radios has been stopped");
    }

    // POST api/Audio/startradio
    [Route("startradio")]
    [HttpPost]
    public ActionResult StartRadioMonitoring([FromBody] StartRadioRequest radio)
    {
        Task.Run(() =>
        {
            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.CreateNoWindow = false;
                startInfo.UseShellExecute = false;
                startInfo.FileName = "RadioChannel.exe";
                startInfo.WindowStyle = ProcessWindowStyle.Normal;
                startInfo.Arguments = $@"{radio.Id} {radio.SegmentDuration} {radio.OverlapDuration}";

                try
                {
                    // Start the process with the info we specified.
                    // Call WaitForExit and then the using statement will close.
                    using (Process exeProcess = Process.Start(startInfo))
                    {
                        radioMonitors.Add(radio.Id, exeProcess);
                        exeProcess.WaitForExit();
                        radioMonitors.Remove(radio.Id);
                    }
                }
                catch (Exception x)
                {
                    using (System.IO.StreamWriter file = new System.IO.StreamWriter(@"failure.txt"))
                    {
                        file.WriteLine(x.ToString());
                    }
                }
            }
            catch (Exception x)
            {
                using (System.IO.StreamWriter file = new System.IO.StreamWriter(@"failure.txt"))
                {
                    file.WriteLine(x.ToString());
                }
            }
        });

        return Ok();
    }
    */

        public class FileReturnModel
        {
            public DateTime created { get; set; }


            public bool job_finished { get; set; }
            public float percentage { get; set; }
            public TimeSpan time_used { get; set; }
            public DateTime estimated_time_of_completion { get; set; }
            
            public TimeSpan file_duration { get; set; }
            public string file_path { get; set; }
            public string user { get; set; }

            public string result_type { get; set; }
        }

        public enum result_types { OnDemand, Livestream }
        public class ResultsReturnModel
        {
            public FileReturnModel file = new FileReturnModel();

            public ResultDTO[] results { get; set; }
        }

        public class ResultsReturnModelLiveStream
        {
            public string channel { get; set; }
            public string result_type { get; set; }

            public ResultDTO[] results { get; set; }
        }

        //  return $"{GetStartTime().Add(new TimeSpan(offset.Ticks)).ToString("HH:mm:ss")} ; {GetEndTime().Add(new TimeSpan(offset.Ticks)).ToString("HH:mm:ss")} ; {title} ; {artists} ; {GetAccuracy()}"; // ; {str_ti}";

        public class ResultDTO
        {
            public string start_time { get; set; }
            public string end_time { get; set; }
            public int duration;
            public string reference { get; set; }
            public string title { get; set; }
            public string artists { get; set; }
            public float accuracy { get; set; }

            public ResultDTO(DateTime StartTime, DateTime EndTime, string Reference, string Title, string Artists, float Accuracy)
            {
                start_time = StartTime.ToString("HH:mm:ss");
                end_time = EndTime.ToString("HH:mm:ss");
                duration = (int) EndTime.Subtract(StartTime).TotalSeconds;
                reference = Reference;
                title = Title;
                artists = Artists;
                accuracy = Accuracy;
            }
        }


        /*
        // GET api/Audio/results/on_demand
        [Route("results/on_demand")]
        [HttpPost]
        public ActionResult<IEnumerable<Result>> GetODResultsFiltered([FromBody] OnDemandRequest req)
        {
            var model = new ResultsReturnModel();
            model.result_type = result_types.OnDemand.ToString();
            new SQLCommunication().GetJobStatus(req.FileID, out float percentage);

            model.percentage = percentage;

            if (percentage != 100)
            {
                model.job_finished = false;
            }
            else { 
                model.job_finished = true;
            }

            try
            {
                var rules = new RuleParser().Parse(req.Arguments);

                new SQLCommunication().GetOnDemandResults(req.FileID, out List<Result> results);

                var lst = new RuleApplier(rules).ApplyRules(results);
                
                var result_dtos = lst.Select(r => new ResultDTO(r.GetStartTime(), r.GetEndTime(), r._reference, r.title, r.artists, r.GetAccuracy()));

                if (result_dtos == null)
                {
                    result_dtos = new List<ResultDTO>();
                }

                model.results = result_dtos.ToArray();

                var json = JsonConvert.SerializeObject(model);

                return Ok(json);

                //return Ok(msg);
            }
            catch (Exception e)
            {
                return BadRequest(e.ToString());
            }
        }
*/

        // GET api/Audio/results/live_stream
       


        /*
        [Route("results/on_demand/{file_id}")]
        [HttpGet]
        public ActionResult<IEnumerable<Result>> GetODResultsAllFilters(int file_id)
        {
            var model = new ResultsReturnModel();
            model.result_type = result_types.OnDemand.ToString();
            new SQLCommunication().GetJob(file_id, out Job job);
            var file = new SQLCommunication().GetFile(file_id, out SQLFile SQLfile);

            model.percentage = job.percentage;
            model.file_path = SQLfile.path;
            model.date = job.start_time;
            model.job_finished = (job.percentage == 100 ? true : false);
            model.time_to_execute = job.last_updated.Subtract(job.start_time);
            model.file_duration = SQLfile.file_duration;


            var rules = new RuleParser().Parse("all_filters=true");

            new SQLCommunication().GetOnDemandResults(file_id, out List<Result> results);

            var lst = new RuleApplier(rules).ApplyRules(results);


            var result_dtos = lst.Select(r => new ResultDTO(r.GetStartTime(), r.GetEndTime(), r._reference, r.title, r.artists, r.GetAccuracy()));

            if (result_dtos == null)
            {
                result_dtos = new List<ResultDTO> { };
            }

            model.results = result_dtos.ToArray();

            var json = JsonConvert.SerializeObject(model);

            return Ok(json);
        }

        [Route("results/on_demand/{file_id}/no_filters")]
        [HttpGet]
        public ActionResult<IEnumerable<Result>> GetODResultsNoFilters(int file_id)
        {
            var model = new ResultsReturnModel();
            model.result_type = result_types.OnDemand.ToString();
            new SQLCommunication().GetJob(file_id, out Job job);
            var file = new SQLCommunication().GetFile(file_id, out SQLFile SQLfile);

            model.percentage = job.percentage;
            model.file_path = SQLfile.path;
            model.date = job.start_time;
            model.job_finished = (job.percentage == 100 ? true : false);
            

            new SQLCommunication().GetOnDemandResults(file_id, out List<Result> results);

            var lst = new RuleApplier(new List<IBusinessRule> { new DRInfoRule() }).ApplyRules(results);


            var result_dtos = lst.Select(r => new ResultDTO(r.GetStartTime(), r.GetEndTime(), r._reference, r.title, r.artists, r.GetAccuracy()));

            if (result_dtos == null)
            {
                result_dtos = new List<ResultDTO> {};
            }

            model.results = result_dtos.ToArray();

            var json = JsonConvert.SerializeObject(model);

            return Ok(json);
        }
        */
    /*
        [Route("fileAFSF")]
        [HttpGet]
        public ActionResult<IEnumerable<Result>> GetODFiles2()
        {
            

            new SQLCommunication().GetOnDemandFiles(out List<OnDemandFile> files);

            files = files.OrderByDescending(f => f.file_id).Take(100).ToList();

            var json = JsonConvert.SerializeObject(files.ToArray());

            return Ok(json);
        }*/

        [Route("jobs/{file_id}")]
        [HttpGet]
        public ActionResult<IEnumerable<Result>> GetJob(int file_id)
        {


            new SQLCommunication().GetJob(file_id, out Job job);

          

            var json = JsonConvert.SerializeObject(job);

            return Ok(json);
        }

        [Route("jobs")]
        [HttpGet]
        public ActionResult<IEnumerable<Result>> GetJobs([FromQuery(Name = "limit")] int limit = 100)
        {
            new SQLCommunication().GetJobs(out List<Job> jobs);

            var json = JsonConvert.SerializeObject(jobs.Take(limit).ToArray());

            return Ok(json);
        }
        
        //TODO Does not work at the moment. Commented to avoid confusion.
        //// POST api/Audio/stopradio
        //[Route("stopradio")]
        //[HttpPost]
        //public ActionResult StopRadioMonitoring([FromBody] string radioID)
        //{
        //    using (System.IO.StreamWriter file = new System.IO.StreamWriter(@"failure.txt"))
        //    {
        //        foreach (var i in radioMonitors)
        //        {
        //            file.WriteLine(i.Key);
        //        }
        //    }
        //    var radioIDs = radioMonitors.Where(k => !k.Key.Equals(radioID));


        //    foreach (var p in radioMonitors)
        //    {
        //        if (!p.Value.HasExited)
        //            p.Value.Kill();
        //    }
        //    List<Process> proc = Process.GetProcessesByName("ffmpeg").ToList();
        //    foreach (Process p in proc)
        //    {
        //        p.Kill();
        //    }
        //    radioMonitors.Clear();

        //    foreach (var id in radioIDs)
        //    {

        //        Task.Run(() =>
        //        {
        //            ProcessStartInfo startInfo = new ProcessStartInfo();
        //            startInfo.CreateNoWindow = false;
        //            startInfo.UseShellExecute = false;
        //            startInfo.FileName = "RadioChannel.exe";
        //            startInfo.WindowStyle = ProcessWindowStyle.Normal;
        //            startInfo.Arguments = id.Key;

        //            using (Process exeProcess = Process.Start(startInfo))
        //            {
        //                radioMonitors.Add(id.Key, exeProcess);
        //                exeProcess.WaitForExit();
        //                radioMonitors.Remove(radioID);
        //            }
        //        });
        //    }

        //    return Ok($"Stopped monitoring {radioID}.");
        //}

            /*

        // POST api/Audio/movelucenetoserver
        [Route("movelucenetoserver")]
        [HttpPost]
        public ActionResult MoveLuceneToServer([FromBody] string serverLuceneCopyPath)
        {
            string sharedLucenePath = @"\\musa01\Download\ITU\MUR\Lucene\LuceneCopy";
            
            if (Directory.Exists(serverLuceneCopyPath))
            {
                ClearFolder(serverLuceneCopyPath);
            }
            DirectoryCopy(sharedLucenePath, serverLuceneCopyPath, true);

            return Ok("Lucene has been moved to the server");
        }

        [Route("deleteandrenamelucene")]
        [HttpPost]
        public ActionResult DeleteAndRenameLucene([FromBody] string serverLuceneCopyPath)
        {
            try
            {
                var serverLucenePath = @"C:\LuceneInUse";
                if (Directory.Exists(serverLucenePath))
                {
                   Directory.Delete(serverLucenePath,true);
                }
                Directory.Move(serverLuceneCopyPath, serverLucenePath);

                return Ok("Lucene has been moved to the server");
            }
            catch (Exception e)
            {
                using (System.IO.StreamWriter file = new System.IO.StreamWriter(@"DeleteAndRenameLucenefailure.txt"))
                {
                    file.WriteLine(e.ToString());
                }
                return BadRequest();
            }
        }

        [Route("RecreateLucene")]
        [HttpPost]
        public ActionResult RecreateLucene([FromBody] string serverLucenePath)
        {
            var comm = new SQLCommunication();


            comm.InsertJob(JobType.CreateLuceneIndex.ToString(), DateTime.Now, 0, -1, $"", out int jobID);

            // add task to queue.

            var type = TaskType.CreateLuceneIndex;

            var arguments = serverLucenePath;

            var res = comm.InsertTask(type, arguments, jobID, out int task_id);

            if (res)
            {
                return Ok();
            }
            else
            {
                return StatusCode(500);
            }
        }


        // GET api/Audio/radio
        [Route("radio")]
        [HttpGet]
        public ActionResult GetRadios()
        {
            var msg = "";
            foreach (var radio in radioMonitors)
            {
                msg += radio.Key + " ";
            }

            return Ok($"{msg}");
        }

        // POST api/Audio/addradio
        [Route("addradio")]
        [HttpPost]
        public ActionResult AddRadioChannel([FromBody] Radio radio)
        {
            if (radio.isInvalid()) return BadRequest("Both ID and URL needs to be a value.");

            Task.Run(() =>
            {
                try
                {
                    ProcessStartInfo startInfo = new ProcessStartInfo();
                    startInfo.CreateNoWindow = false;
                    startInfo.UseShellExecute = false;
                    startInfo.FileName = "RadioChannel.exe";
                    startInfo.WindowStyle = ProcessWindowStyle.Normal;
                    startInfo.Arguments = $"add {radio.ID} {radio.URL}";


                    try
                    {
                        // Start the process with the info we specified.
                        // Call WaitForExit and then the using statement will close.
                        using (Process exeProcess = Process.Start(startInfo))
                        {
                            exeProcess.WaitForExit();
                        }
                    }
                    catch (Exception x)
                    {
                        using (System.IO.StreamWriter file = new System.IO.StreamWriter(@"failure.txt"))
                        {
                            file.WriteLine(x.ToString());
                        }
                    }
                }
                catch (Exception x)
                {
                    using (System.IO.StreamWriter file = new System.IO.StreamWriter(@"failure.txt"))
                    {
                        file.WriteLine(x.ToString());
                    }
                }
            });

            return Ok();
        }
        */
        

        private (string, int) ExtractAudioNameFromPath(string songPath)
        {
            string match = @"\\(.+\\)*(.+)\.(.+)$";

            Match regex = Regex.Match(songPath, match,
            RegexOptions.IgnoreCase);
            if (!regex.Success || (regex.Groups.Count != 4))
                return (null, 400);
            // Checks if filepath has the necessary information and returns a 400 status code if incorrect.
            if (!formatsSupported.Contains(regex.Groups[3].Value))
                return (null, 400);
            // Checks if the file format is supported.

            return (regex.Groups[2].Value, 200); // Finally, we get the Group value and display it.
        }

        private static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
        {
            // Get the subdirectories for the specified directory.
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);

            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException(
                    "Source directory does not exist or could not be found: "
                    + sourceDirName);
            }

            DirectoryInfo[] dirs = dir.GetDirectories();
            // If the destination directory doesn't exist, create it.
            if (!Directory.Exists(destDirName))
            {
                Directory.CreateDirectory(destDirName);
            }

            // Get the files in the directory and copy them to the new location.
            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                string temppath = Path.Combine(destDirName, file.Name);
                file.CopyTo(temppath, false);
            }

            // If copying subdirectories, copy them and their contents to new location.
            if (copySubDirs)
            {
                foreach (DirectoryInfo subdir in dirs)
                {
                    string temppath = Path.Combine(destDirName, subdir.Name);
                    DirectoryCopy(subdir.FullName, temppath, copySubDirs);
                }
            }
        }

        private void ClearFolder(string folderName)
        {
            DirectoryInfo dir = new DirectoryInfo(folderName);

            foreach (FileInfo fi in dir.GetFiles())
            {
                fi.IsReadOnly = false;
                fi.Delete();
            } //foreach

            foreach (DirectoryInfo di in dir.GetDirectories())
            {
                ClearFolder(di.FullName);
                di.Delete();
            } //foreach
        }
    }
}
