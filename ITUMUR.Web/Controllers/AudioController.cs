using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Framework;

namespace ITUMUR.Web.Controllers
{
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

        
        /*
        [Route("tracks")]
        [HttpGet]
        public IEnumerable<Songs> GetTracks()
        {
            return _repo.GetSongs().ToArray();
        }

    */
        [Route("tracks/{dr_nr}")]
        [HttpGet]
        public Songs GetTrackByDrNr(string dr_nr)
        {
            return _repo.GetTrackByDrNr(dr_nr);
        }

        [Route("channels")]
        [HttpGet]
        public IEnumerable<Stations> GetStations()
        {
            return _repo.GetStations().ToArray();
        }

        [Route("channels/{name}")]
        [HttpGet]
        public Stations GetStationByName(string name)
        {
            return _repo.GetStationsByName(name);
        }

        [Route("channels/{channel_name}/results")]
        [HttpGet]
        public IEnumerable<Result> GetChannelResults(string channel_name, [FromQuery(Name = "filters")] bool filters = true, [FromQuery(Name = "begin")] string begin = null, [FromQuery(Name = "end")] string end = null)
        {
            return _repo.GetChannelResults(channel_name, filters, begin, end);
        }

        [Route("jobs")]
        [HttpGet]
        public IEnumerable<Job> GetJobs()
        {
            return _repo.GetJobs();
        }

        [Route("jobs/{id}")]
        [HttpGet]
        public Job GetJobByID(int id)
        {
            return _repo.GetJobByID(id);
        }

        [Route("files/{id}")]
        [HttpGet]
        public Files GetODFile(int id)
        {
            return _repo.GetODFileByID(id);
        }

        [Route("files")]
        [HttpGet]
        public IEnumerable<Files> GetODFiles()
        {
            return _repo.GetODFiles();
        }

        [Route("files/{id}/results")]
        [HttpGet]
        public IEnumerable<Result> GetODFileResults(int file_id, [FromQuery(Name = "filters")] bool filters = true)
        {
            return _repo.GetODFileResultsByID(file_id, filters);
        }
    }
}
