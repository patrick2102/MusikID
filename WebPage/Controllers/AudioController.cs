using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Framework;
using Microsoft.AspNetCore.Cors;
using Framework.DTO;
using Nancy.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Globalization;

namespace ITUMUR.Web.Controllers
{
    [EnableCors("MyPolicy")]
    [ApiController]
    [Route("api/")]
    public class AudioController : ControllerBase
    {


        private readonly ILogger<AudioController> _logger;
        private readonly IDrRepository _repo;

        public AudioController(ILogger<AudioController> logger)
        {
            _logger = logger;
            _repo = new DrRepository(new drfingerprintsContext());
        }

        [Route("tracks")]
        [HttpGet]
        public ActionResult<IEnumerable<TrackDTO>> GetTracks([FromQuery(Name = "limit")] int limit = 100)
        {
            return Ok(_repo.GetSongs(limit).ToArray());
        }

        [Route("tracks/{dr_nr}")]
        [HttpGet]
        public ActionResult<TrackDTO> GetTrackByDrNr(string dr_nr)
        {
            var res = _repo.GetTrackByDrNr(dr_nr);

            if (res == null) return NoContent();
            else return Ok(res); 
        }

        public class PostTrackReq {
            public string path { get; set; }
        }

        [Route("tracks")]
        [HttpPost]
        public  ActionResult<Job> PostTrack([FromBody] PostTrackReq req, [FromQuery(Name = "force")] bool force = false)
        {
            if (!System.IO.File.Exists(req.path)) return BadRequest("File not found");
            var res = _repo.PostTrack(req.path,force);
            return Created($"api/tracks/{res.Id}", res);
        }

        [Route("channels")]
        [HttpGet]
        public IEnumerable<Stations> GetStations()
        {
            //todo create channel DTO and use.
            return _repo.GetStations().ToArray();
        }

        [Route("channels/{name}")]
        [HttpGet]
        public Stations GetStationByName(string name)
        {
            //TODO channelDTO
            return _repo.GetStationsByName(name);
        }

        public class PostStationsReq
        {
            public string url { get; set; }
            public string name { get; set; }
            public string type { get; set; }
            public string drId { get; set; }
        }

        [Route("channels")]
        [HttpPost]
        public Stations PostStation([FromBody] PostStationsReq req )
        {
            //todo not valid url.
            return _repo.PostStation(new Stations{ StreamingUrl = req.url, ChannelName = req.name, ChannelType = req.type, DrId = req.drId, Running = false }  );
        }

        [Route("channels/{name}/start")]
        [HttpPost]
        public IEnumerable<Stations> StartStationByName(string name)
        {
            var res = _repo.StartStationByName(name);
            //TODO error handling hvis res == false , wrong name, 
            return _repo.GetStations().ToArray();
        }

        [Route("channels/{name}/stop")]
        [HttpPost]
        public IEnumerable<Stations> StopStationByName(string name) 
        {
            var res = _repo.StopStationByName(name);
            //TODO error handling hvis res == false
            return _repo.GetStations().ToArray();
        }

        public class ChannelRematchReq {

            public string endTime { get; set; }
            public string startTime { get; set; }

        }

        [Route("channels/{name}/rematch")]
        [HttpPost]
        public ActionResult<bool> RematchOnChannel(string name, [FromBody] ChannelRematchReq req )
        {
            //return NotSupportedException();
            //maybe return fileDTO
            var res = _repo.PostRematchTask(name, req.startTime, req.endTime);
            return res;
        }


        public class postFileReq
        {
            public string audioPath { get; set; }
            public string user { get; set; }
        }

        [EnableCors("MyPolicy")] //good to remember Cors
        [Route("files")]
        [HttpPost]
        public ActionResult<FileDTO> PostFile([FromBody] postFileReq req)
        {
            if (!System.IO.File.Exists(req.audioPath)) return BadRequest("Can't find file.");

            var res = _repo.PostFile(req.audioPath, req.user);

            return Created($"api/files/{res.id}", res);

        }

        private string initialResultJSON;
        private DateTime beginTime;
        private DateTime endTime;

        [Route("channels/{channel_name}/results")]
        [HttpGet]
        public ContentResult GetChannelResults(string channel_name, [FromQuery(Name = "filters")] bool filters = true, [FromQuery(Name = "begin")] string begin = null, [FromQuery(Name = "end")] string end = null)
        {
            /**
             * 
             *  TODO We need to move all this combining interval files out of the controller and it needs to be refactored a lot, way to hard to understand.
             * 
             */

            Directory.CreateDirectory($@"\\musa01\Download\ITU\MUR\JSONResults_Livestream\{channel_name}\results\");
            string outputPath = $@"\\musa01\Download\ITU\MUR\JSONResults_Livestream\{channel_name}\results\results.json";
            LivestreamResultsDTO tempResults;
            string newFormat = "yyyy-MM-ddTHH:mm";
            try
            {
                beginTime = DateTime.ParseExact(begin, newFormat, CultureInfo.InvariantCulture);
                endTime = DateTime.ParseExact(end, newFormat, CultureInfo.InvariantCulture);
            }
            catch
            {
                beginTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.AddHours(-1).Hour, DateTime.Now.Minute, DateTime.Now.Second);
                endTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, 0, 0);
            }

            if (begin == null || end == null)
            {
                //Is called when you want the latest hour of results
                begin = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, 0, 0).ToString(newFormat);
                end = DateTime.Now.ToString(newFormat);
                var liveStreamDTO = createEmptyJSONFile(channel_name, begin, end, outputPath);
                liveStreamDTO = callDBToFindResults(channel_name, filters, begin, end, liveStreamDTO);
                tempResults = liveStreamDTO;
                initialResultJSON = Newtonsoft.Json.JsonConvert.SerializeObject(liveStreamDTO);
                System.IO.File.WriteAllText(outputPath, initialResultJSON);
            }
            else if (DateTime.Now < endTime)
            {
                var dt = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, 0, 0);
                var liveStreamDTO = createEmptyJSONFile(channel_name, begin, end, outputPath);
                var dtString = dt.ToString(newFormat);
                liveStreamDTO = callDBToFindResults(channel_name, filters, dtString, end, liveStreamDTO);
                tempResults = liveStreamDTO;
                initialResultJSON = Newtonsoft.Json.JsonConvert.SerializeObject(liveStreamDTO);
                System.IO.File.WriteAllText(outputPath, initialResultJSON);
            }
            else
            {
                //Creates the initial JSON file
                var initialResultDTO = createEmptyJSONFile(channel_name, begin, end, outputPath);
                tempResults = initialResultDTO;
                initialResultJSON = Newtonsoft.Json.JsonConvert.SerializeObject(initialResultDTO);
                System.IO.File.WriteAllText(outputPath, initialResultJSON);
            }

            //Searches for all files with correct creation time inbetween begin and end times 
            var intervalFiles = new List<string>();
            foreach (var file in Directory.GetFiles($@"\\musa01\Download\ITU\MUR\JSONResults_Livestream\{channel_name}\"))
            {
                var exactTime = System.IO.File.GetCreationTime(file);
                var dt = new DateTime(exactTime.Year, exactTime.Month, exactTime.Day, exactTime.Hour, 0, 0, 0);

                if (dt > beginTime && dt <= endTime.AddHours(1))
                {
                    intervalFiles.Add(file);
                }
            }
            intervalFiles.OrderBy(f => System.IO.File.GetCreationTime(f));

            //Searches for end and start files
            var first = intervalFiles.FirstOrDefault();
            var last = intervalFiles.LastOrDefault();

            intervalFiles.Remove(first);
            intervalFiles.Remove(last);

            if (intervalFiles.Count == 0 && tempResults.results.Count == 0)
            {
                //find results in db if no json files in the interval.
                var liveStreamDTO = createEmptyJSONFile(channel_name, begin, end, outputPath);
                liveStreamDTO = callDBToFindResults(channel_name, filters, begin, end, liveStreamDTO);
                var returnRes = Newtonsoft.Json.JsonConvert.SerializeObject(liveStreamDTO);
                return Content(returnRes);
            }
            //Adds results to result json
            appendStartEndFiles(outputPath, new List<string> { first, last }, beginTime, endTime);
            appendFilesInInterval(outputPath, intervalFiles);

            //Orders all results according to timedate and returns the finalized result json
            var jsonFromFile = System.IO.File.ReadAllText(outputPath);

            try
            {
                var resultJSON = Newtonsoft.Json.JsonConvert.DeserializeObject<LivestreamResultsDTO>(jsonFromFile);
                var livestreamDTO = new LivestreamResultsDTO()
                {
                    channelName = resultJSON.channelName,
                    startTime = resultJSON.startTime,
                    endTime = resultJSON.endTime,
                    results = resultJSON.results.OrderBy(x => x.start_time).ToList()
                };
                int j = 0;
                for (int i = 1; i < livestreamDTO.results.Count; i++)
                {
                    var lastResult = livestreamDTO.results[j];
                    var firstResult = livestreamDTO.results[i];
                    if (firstResult != null && lastResult != null)
                    {
                        if (firstResult.reference == lastResult.reference)
                        {
                            livestreamDTO.results.Remove(lastResult);
                            firstResult.start_time = lastResult.start_time;
                            firstResult.duration = lastResult.duration + firstResult.duration;
                        }
                    }
                    j++;
                }

                var finalAndOrderedResult = Newtonsoft.Json.JsonConvert.SerializeObject(livestreamDTO);
                System.IO.File.WriteAllText(outputPath, finalAndOrderedResult);
                var finalAndOrderedResultJSON = System.IO.File.ReadAllText(outputPath);
                return Content(finalAndOrderedResult, "application/json");
            }
            catch
            {
                return Content(initialResultJSON, "application/json");
            }
        }

        private LivestreamResultsDTO createEmptyJSONFile(string channel_name, string begin, string end, string outputPath)
        {
            var emptyLiveStreamResultDTO = new LivestreamResultsDTO
            {
                channelName = channel_name,
                startTime = begin,
                endTime = end,
                results = new List<ResultDTO>()
            };
            return emptyLiveStreamResultDTO;
        }

        private LivestreamResultsDTO callDBToFindResults(string channel_name, bool filters, string begin, string end, LivestreamResultsDTO liveStreamResultDTO)
        {
            var results = _repo.GetLivestreamResults(channel_name, filters, begin, end);
            liveStreamResultDTO.results.AddRange(results.results);
            return liveStreamResultDTO;
        }

        private bool appendStartEndFiles(string outputPath, List<string> startFile, DateTime beginTime, DateTime endTime)
        {
            var file = System.IO.File.ReadAllText(outputPath);
            try
            {
                var resultFile = Newtonsoft.Json.JsonConvert.DeserializeObject<LivestreamResultsDTO>(file);
                var count = 0;
                foreach (string filePath in startFile)
                {
                    if (!System.IO.File.Exists(filePath))
                    {
                        break;
                    }
                    var liveStreamResultString = System.IO.File.ReadAllText(filePath);

                    //Specific path (typically an hour of result)

                    var liveStreamResult = Newtonsoft.Json.JsonConvert.DeserializeObject<LivestreamResultsDTO>(liveStreamResultString);

                    var lastResult = liveStreamResult.results.FirstOrDefault();
                    var firstResult = resultFile.results.LastOrDefault();

                    if (firstResult != null && lastResult != null)
                    {
                        if (firstResult.reference == lastResult.reference)
                        {
                            resultFile.results.Remove(firstResult);
                            liveStreamResult.results.Remove(lastResult);
                            lastResult.duration = lastResult.duration + firstResult.duration;
                            resultFile.results.Add(firstResult);
                        }
                    }
                    foreach (var result in liveStreamResult.results)
                    {
                        //string newFormat = "yyyy/MM/dd HH:mm:ss";
                        string newFormat = "HH:mm:ss";
                        var startOrEndTime = DateTime.ParseExact(result.start_time, newFormat, CultureInfo.InvariantCulture);
                        if (count == 0)
                        {
                            if (startOrEndTime.Minute > beginTime.Minute)
                            {
                                resultFile.results.Add(result);
                            }
                        }
                        else if (count == 1)
                        {
                            if (startOrEndTime.Minute < endTime.Minute)
                            {
                                resultFile.results.Add(result);
                            }
                        }
                        else
                        {
                            Console.WriteLine("It got more than one end and start file");
                        }

                        var resultJSON = Newtonsoft.Json.JsonConvert.SerializeObject(resultFile);

                        System.IO.File.WriteAllText(outputPath, resultJSON);

                    }
                    count++;
                }
            }
            catch
            {
                return false;
            }
            return true;
        }

        private static void appendFilesInInterval(string outputPath, List<string> filesInInterval)
        {
            foreach (var path in filesInInterval)
            {
                var liveStreamResultString = System.IO.File.ReadAllText(path);

                //Specific path (typically an hour of result)
                try
                {
                    var liveStreamResult = Newtonsoft.Json.JsonConvert.DeserializeObject<LivestreamResultsDTO>(liveStreamResultString);

                    //Full result for return
                    var file = System.IO.File.ReadAllText(outputPath);
                    var resultFile = Newtonsoft.Json.JsonConvert.DeserializeObject<LivestreamResultsDTO>(file);
                    var lastResult = liveStreamResult.results.FirstOrDefault();
                    var firstResult = resultFile.results.LastOrDefault();
                    if (firstResult != null && lastResult != null)
                    {
                        if (firstResult.reference == lastResult.reference)
                        {
                            resultFile.results.Remove(firstResult);
                            liveStreamResult.results.Remove(lastResult);
                            firstResult.duration = lastResult.duration + firstResult.duration;
                            firstResult.start_time = lastResult.start_time;
                            resultFile.results.Add(firstResult);
                        }
                    }

                    resultFile.results.AddRange(liveStreamResult.results);

                    var livestreamResultJSON = Newtonsoft.Json.JsonConvert.SerializeObject(resultFile);

                    System.IO.File.WriteAllText(outputPath, livestreamResultJSON);
                }
                catch
                {
                    Console.WriteLine("Probably a corrupted JSON file");
                }
            }
        }

        [Route("jobs")]
        [HttpGet]
        public ActionResult<IEnumerable<Job>> GetJobs([FromQuery(Name = "limit")] int limit = 100)
        {
            return Ok(_repo.GetJobs());
        }

        [Route("jobs/{id}")]
        [HttpGet]
        public ActionResult<FileDTO> GetJobByID(int id)
        {
            var res = _repo.GetJobByID(id);
            
            if (res == null) return NoContent();
            else return Ok(res);
        }
        /*
        [Route("OnDemandFiles/{id}")]
        [HttpGet]
        public ActionResult<FileDTO> GetODFile(int id)
        {
            //use file_dto, if empty code 204.

            var res = _repo.GetODFileByID(id);
            if (res == null) return NoContent();
            else return Ok(res);
        }
        */

        [Route("ondemandfiles")]
        [HttpGet]
        public ActionResult<IEnumerable<FileDTO>> GetODFiles([FromQuery(Name = "limit")] int limit = 200)
        {
            return Ok(_repo.GetODFiles(limit).Reverse());
        }

        [Route("files/{id}")]
        [HttpGet]
        public ActionResult<FileDTO> GetFile(int id)
        {

            var res = _repo.GetFileByID(id);
            if (res == null) return NoContent();
            else return Ok(res);
        }

        [Route("files")]
        [HttpGet]
        public ActionResult<IEnumerable<FileDTO>> GetFiles([FromQuery(Name = "limit")] int limit = 200)
        {
            return Ok(_repo.GetFiles(limit).Reverse());
        }


        [Route("ondemandfiles/{id}")]
        [HttpGet]
        public ContentResult GetODFileResults(int id, [FromQuery(Name = "filters")] bool filters = true)
        {
            
            var path = $@"\\musa01\download\ITU\MUR\JSONResults\{id}.json";

            //if json does not exist for the given ondemand file, create one.
            if (!System.IO.File.Exists(path))
            {
                var od_results = _repo.GetODFileResultsByID(id, true);

                var json = Newtonsoft.Json.JsonConvert.SerializeObject(od_results);
                System.IO.File.WriteAllText(path, json);
            }

            if (!filters)
            {

                var results = _repo.GetODFileResultsByID(id, filters);

                var temp = new JavaScriptSerializer().Serialize(results);
                return Content(temp, "application/json");
            }
            else
            {
                var jsonFromFile = System.IO.File.ReadAllText(path);
                var resultJSON = Newtonsoft.Json.JsonConvert.DeserializeObject<OnDemandResultDTO>(jsonFromFile);
                if (resultJSON.results.Length == 0)
                {
                    var results = _repo.GetODFileResultsByID(id, filters);

                    var json = new JavaScriptSerializer().Serialize(results);
                    System.IO.File.Delete(path);
                    System.IO.File.WriteAllText(path, json);
                    return Content(json, "application/json");
                }
                return Content(jsonFromFile, "application/json");
            }
        }

        public class CheckFilesReq {
            public string folder_name { get; set; }
        }

        [Route("TrackFolderCheck")]
        [HttpPost]
        public ActionResult<CheckFilesResult> PostCheckFile([FromBody] CheckFilesReq req)
        {
            var full_path = $@"\\musa01\download\ITU\MUR\TrackFolderCheck\{req.folder_name}";
            if (!Directory.Exists(full_path)) return BadRequest("Can't find folder. Hint: use the relative path of the folder instead of the absolute.");

            var res = _repo.PostCheckFile(req.folder_name);

            if (res == null) return BadRequest("Failed to create check file job.");

            return Created("not_implemented_in_API", res);
        }


        [Route("TrackFolderCheck/{folder_name}")]
        [HttpGet]
        public ContentResult GetCheckFilesResult(string folder_name)
        {
            var full_path = $@"\\musa01\download\ITU\MUR\TrackFolderCheckResult\{folder_name}\CheckFileResults.json";

            //if (!System.IO.File.Exists(full_path)) return BadRequest("Can't find folder. Hint: use the relative path of the folder instead of the absolute.");

            var res = _repo.GetCheckFile(folder_name);

         //   if (res == null) return NoContent();

            return Content(res, "application/json"); 
        }

        //[Route("files/{id}/results")]
        //[HttpPost]
        //public IActionResult (int id, [FromQuery(Name = "filters")] bool filters = true)
        //{
        //    //TODO no content return status code. 
        //    return Ok(false);
        //}

    }
}
