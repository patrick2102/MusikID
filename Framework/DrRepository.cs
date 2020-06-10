
using Framework.DTO;
using Framework.Rules;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Framework
{
    public class DrRepository : IDrRepository
    {
        private readonly drfingerprintsContext _cont;
        public DrRepository(drfingerprintsContext cont)
        {
            _cont = cont;
        }

        public IEnumerable<TrackDTO> GetSongs(int limit)
        {
            List<Songs> songs;
            using (var cont = new drfingerprintsContext())
            {
                songs = cont.Songs.OrderByDescending(s => s.Id).Take(limit).ToList();
            }
            return songs.Select(s => new TrackDTO()
            {
                Id = s.Id,
                DrDiskoteksnr = s.DrDiskoteksnr,
                Sidenummer = s.Sidenummer,
                Sekvensnummer = s.Sekvensnummer,
                DateChanged = s.DateChanged,
                Reference = s.Reference,
                Duration = s.Duration
            });
        }

        public void CheckForFailedTasks()
        {
            var reset_limit = 5;  //5 min
            using (var cont = new drfingerprintsContext())
            {

                // join job and task where started = 0 and LastUpdated.AddMinutes(reset_limit) < DateTime.Now

                var tasks = (from t in cont.TaskQueue
                             join j in cont.Job on t.JobId equals j.Id
                             where (j.LastUpdated.AddMinutes(reset_limit) < DateTime.Now)
                             select t
                             ).ToList();

                foreach (var t in tasks)
                {
                    t.Started = false;
                }
                cont.SaveChanges();
            }
        }

        public TrackDTO GetTrackByDrNr(string dr_nr)
        {
            Songs s;
            using (var cont = new drfingerprintsContext())
            {
                s = cont.Songs.Where(song => song.Reference == dr_nr).SingleOrDefault();
            }
            if (s == null) return null;
            return new TrackDTO()
            {
                Id = s.Id,
                DrDiskoteksnr = s.DrDiskoteksnr,
                Sidenummer = s.Sidenummer,
                Sekvensnummer = s.Sekvensnummer,
                DateChanged = s.DateChanged,
                Reference = s.Reference,
                Duration = s.Duration
            };
        }
        public IEnumerable<Stations> GetStations()
        {
            IEnumerable<Stations> res;
            using (var cont = new drfingerprintsContext())
            {
                res = cont.Stations.ToList();
            }
            return res;
        }

        public void DeleteLiveStreamResultsOlderThan(int days)
        {
            using (var cont = new drfingerprintsContext())
            {
                var results = cont.LivestreamResults.Where(s => s.PlayDate < DateTime.Now.AddDays(-days)).ToList();
                cont.LivestreamResults.RemoveRange(results);
                cont.SaveChanges();
            }
        }

        public void DeleteODResultsOlderThan(int days)
        {
            using (var cont = new drfingerprintsContext())
            {
                var results = cont.OnDemandResults.Where(s => s.LastUpdated < DateTime.Now.AddDays(-days)).ToList();
                cont.OnDemandResults.RemoveRange(results);
                cont.SaveChanges();
            }
        }

        public Stations GetStationsByName(string name)
        {
            return _cont.Stations.Where(s => s.ChannelName == name).SingleOrDefault();
        }

        public List<Stations> GetRunningStations()
        {
            using (var cont = new drfingerprintsContext())
            {
                var stations = cont.Stations.Where(s => s.Running == true).ToList();
                return stations;
            }
        }



        public LivestreamResultsDTO GetLivestreamResults(string channel_name, bool filters, string begin, string end)
        {
            CultureInfo provider = CultureInfo.InvariantCulture;
            DateTime begin_date, end_date;
            if (begin == null)
            {
                begin_date = DateTime.Now.AddHours(-1);
            }
            else
            {
                begin_date = DateTime.ParseExact(begin, "yyyy-MM-ddTHH:mm", provider);
            }
            if (end == null)
            {
                end_date = DateTime.Now;
            }
            else
            {
                end_date = DateTime.ParseExact(end, "yyyy-MM-ddTHH:mm", provider);
            }
            IOrderedEnumerable<Result> converted;
            using (var cont = new drfingerprintsContext())
            {
                //if (begin_date > end_date) return BadRequest("Begin Date cannot be before end.");
                var results = cont.LivestreamResults.Where(res => res.ChannelId == channel_name).Where(res => res.PlayDate >= begin_date && res.PlayDate <= end_date);

                converted = (from r in results
                                 join s in cont.Songs on r.SongId equals s.Id
                                 select new Result()
                                 {
                                     resultID = (int)r.Id,
                                     _startTime = r.PlayDate,
                                     _endTime = r.PlayDate.AddSeconds(r.Duration),
                                     _reference = s.Reference,
                                     _song_duration = s.Duration,
                                     _diskotekNr = s.DrDiskoteksnr,
                                     _sideNr = s.Sidenummer,
                                     _sequenceNr = s.Sekvensnummer,
                                     _accuracy = r.Accuracy,
                                     _song_offset_seconds = r.SongOffset.Value,
                                     _timeIndex = r.SongOffset.Value
                                 }).ToList().OrderBy(r => r._startTime);
            }
            IEnumerable<ResultDTO> temp;
            if (filters)
            {
                var filtered_results = new RuleApplier(new RuleParser().Parse("all_filters=true")).ApplyRules(converted);

                temp = filtered_results.Select(r => new ResultDTO(r.GetStartTime(), r.GetEndTime(), r._reference, r.title, r.artists, r.GetAccuracy()));
            }
            else
            {
                temp = converted.Select(r => new ResultDTO(r.GetStartTime(), r.GetEndTime(), r._reference, r.title, r.artists, r.GetAccuracy()));
            }
            return new LivestreamResultsDTO()
            {
                results = temp.Reverse().ToList(),
                channelName = channel_name,
                endTime = end,
                startTime = begin
            };
        }

        public Songs CheckIfReferenceInSongs(int diskoteks_nr, int side_nr, int skærings_nr)
        {
            Songs result;
            using (var cont = new drfingerprintsContext())
            {
                result = cont.Songs.Where(s => s.DrDiskoteksnr == diskoteks_nr).Where(s => s.Sidenummer == side_nr).Where(s => s.Sekvensnummer == skærings_nr).ToList().FirstOrDefault();
                return result;
            }
        }

        public int InsertLivestreamResult(LivestreamResults result)
        {
            LivestreamResults newRes;
            using (var cont = new drfingerprintsContext())
            {
                var song = cont.Songs.Where(s => s.Reference == result.Song.Reference).FirstOrDefault();

                if (song == null) return 0;

                newRes = result;
                newRes.SongId = song.Id;
                cont.LivestreamResults.Add(newRes);

                cont.SaveChanges();
            }
            return (int)newRes.Id;
        }

        public int InsertJob(JobType type, int file_id, string args)
        {
            var job = new Job()
            {
                Arguments = args,
                FileId = file_id,
                JobType = type.ToString()
            };
            using (var cont = new drfingerprintsContext())
            {
                cont.Job.Add(job);
                cont.SaveChanges();
            }
            return (int)job.Id;
        }

        public void InsertRadioTask(string channelId, string ChunkPath)
        {

            var radioTask = new RadioTaskQueue()
            {
                ChannelId = channelId,
                ChunkPath = ChunkPath
            };

            using (var cont = new drfingerprintsContext())
            {
                cont.RadioTaskQueue.Add(radioTask);
                cont.SaveChanges();
            }
        }

        public int InsertFile(string audio_path, string extension)
        {
            Files file = new Files() { FilePath = audio_path, FileType = extension };
            using (var cont = new drfingerprintsContext())
            {
                cont.Files.Add(file);

                cont.SaveChanges();
            }
            return (int)file.Id;
        }

        public async Task<bool> DeleteTask(long id)
        {
            using (var cont = new drfingerprintsContext())
            {
                var task = cont.TaskQueue.Where(t => t.Id == id).FirstOrDefault();

                if (task == null) return false;

                cont.TaskQueue.Remove(task);

                cont.SaveChanges();
            }
            return false;
        }

        public async Task<bool> DeleteRadioTask(long id)
        {
            using (var cont = new drfingerprintsContext())
            {
                var task = cont.RadioTaskQueue.Where(t => t.Id == id).FirstOrDefault();

                if (task == null) return false;

                cont.RadioTaskQueue.Remove(task);

                cont.SaveChanges();
            }
            return false;
        }

        public async Task<bool> DeleteFingerTask(long id)
        {
            using (var cont = new drfingerprintsContext())
            {
                var task = cont.FingerTaskQueue.Where(t => t.Id == id).FirstOrDefault();

                if (task == null) return false;

                cont.FingerTaskQueue.Remove(task);

                cont.SaveChanges();
            }
            return false;
        }

        public async Task<bool> DeleteResults(long file_id)
        {
            using (var cont = new drfingerprintsContext())
            {
                var res = cont.OnDemandResults.Where(r => r.FileId == file_id).ToList();

                cont.OnDemandResults.RemoveRange(res);

                cont.SaveChanges();
            }
            return true;
        }

        public IEnumerable<Job> GetJobs()
        {
            using (var cont = new drfingerprintsContext())
            {
                return cont.Job;
            }
        }

        public FileDTO GetJobByID(int id)
        {
            //TODO: This doesn't work atm, but probably works when merged with paco and jacos
            Job job;
            using (var cont = new drfingerprintsContext())
            {
                job = cont.Job.Where(j => j.Id == id).FirstOrDefault();
            }
            if (job == null) return null;

            var f = new FileDTO()
            {
                created = job.StartDate.ToString("yyyy-MM-dd HH:mm:ss"),
                percentage = job.Percentage,
                user = job.User,
                last_updated = job.LastUpdated.ToString("yyyy-MM-dd HH:mm:ss"),
                jobId = job.Id
            };

            return f;
        }

        public FileDTO GetODFileByID(int id)
        {
            FileDTO file_dto;
            using (var cont = new drfingerprintsContext())
            {
                file_dto =
                            (from f in cont.Files
                             join j in cont.Job on f.Id equals j.FileId
                             where f.Id == id && j.JobType == JobType.AudioMatch.ToString()
                             select new FileDTO()
                             {
                                 id = f.Id,
                                 created = j.StartDate.ToString("yyyy-MM-dd HH:mm:ss"),
                                 percentage = j.Percentage,
                                 job_finished = j.Percentage == 100 ? true : false,
                                 time_used = j.LastUpdated.Subtract(j.StartDate).ToString(),
                                 user = j.User,
                                 file_path = f.FilePath,
                                 file_duration = new TimeSpan((long)f.Duration.Value).ToString(),
                                 file_ext = f.FileType,
                                 estimated_time_of_completion = (j.Percentage == 0 ? DateTime.MinValue : DateTime.Now.AddTicks((long)(j.LastUpdated.Subtract(j.StartDate).Ticks * (100 / j.Percentage))).AddTicks(-j.LastUpdated.Subtract(j.StartDate).Ticks)).ToString("yyyy-MM-dd HH:mm:ss")

                             }).FirstOrDefault();
            }


            return file_dto;
        }

        public FileDTO GetFileByID(int id)
        {
            FileDTO file_dto;
            using (var cont = new drfingerprintsContext())
            {
                file_dto =
                            (from f in cont.Files
                             join j in cont.Job on f.Id equals j.FileId
                             where f.Id == id
                             select new FileDTO()
                             {
                                 id = f.Id,
                                 created = j.StartDate.ToString("yyyy-MM-dd HH:mm:ss"),
                                 percentage = j.Percentage,
                                 job_finished = j.Percentage == 100 ? true : false,
                                 time_used = j.LastUpdated.Subtract(j.StartDate).ToString(),
                                 user = j.User,
                                 file_path = f.FilePath,
                                 file_duration = new TimeSpan((long)f.Duration.Value).ToString(),
                                 file_ext = f.FileType,
                                 estimated_time_of_completion = (j.Percentage == 0 ? DateTime.MinValue : DateTime.Now.AddTicks((long)(j.LastUpdated.Subtract(j.StartDate).Ticks * (100 / j.Percentage))).AddTicks(-j.LastUpdated.Subtract(j.StartDate).Ticks)).ToString("yyyy-MM-dd HH:mm:ss")

                             }).FirstOrDefault();
            }


            return file_dto;
        }
        public Files GetODFileByJobID(long job_id)
        {
            using (var cont = new drfingerprintsContext())
            {
                var job = cont.Job.Where(j => j.Id == job_id).FirstOrDefault();

                return cont.Files.Where(f => f.Id == job.FileId).FirstOrDefault();
            }
        }

        public IEnumerable<FileDTO> GetODFiles(int limit)
        {
            List<FileDTO> file_dtos;
            using (var cont = new drfingerprintsContext())
            {
                file_dtos =
                            (from f in cont.Files
                             join j in cont.Job on f.Id equals j.FileId
                             where j.JobType == JobType.AudioMatch.ToString()
                             select new FileDTO()
                             {
                                 id = f.Id,
                                 created = j.StartDate.ToString("yyyy-MM-dd HH:mm:ss"),
                                 percentage = j.Percentage,
                                 job_finished = j.Percentage == 100 ? true : false,
                                 time_used = j.LastUpdated.Subtract(j.StartDate).ToString(),
                                 user = j.User,
                                 file_path = f.FilePath,
                                 file_duration = TimeSpan.FromSeconds((long)f.Duration.Value).ToString(),
                                 file_ext = f.FileType,
                                 job_type = j.JobType,
                                 last_updated = j.LastUpdated.ToString(),
                                 jobId = j.Id,
                                 estimated_time_of_completion = (j.Percentage == 0 ? DateTime.MinValue : DateTime.Now.AddTicks((long)(j.LastUpdated.Subtract(j.StartDate).Ticks * (100 / j.Percentage))).AddTicks(-j.LastUpdated.Subtract(j.StartDate).Ticks)).ToString("yyyy-MM-dd HH:mm:ss") //TODO

                             }).ToList();
            }
            return file_dtos;
        }

        public IEnumerable<FileDTO> GetFiles(int limit)
        {
            List<FileDTO> file_dtos;
            using (var cont = new drfingerprintsContext())
            {
                file_dtos =
                            (from f in cont.Files
                             join j in cont.Job on f.Id equals j.FileId
                             select new FileDTO()
                             {
                                 id = f.Id,
                                 created = j.StartDate.ToString("yyyy-MM-dd HH:mm:ss"),
                                 percentage = j.Percentage,
                                 job_finished = j.Percentage == 100 ? true : false,
                                 time_used = j.LastUpdated.Subtract(j.StartDate).ToString(),
                                 user = j.User,
                                 file_path = f.FilePath,
                                 file_duration = TimeSpan.FromSeconds((long)f.Duration.Value).ToString(),
                                 file_ext = f.FileType,
                                 job_type = j.JobType,
                                 last_updated = j.LastUpdated.ToString(),
                                 jobId = j.Id,
                                 estimated_time_of_completion = (j.Percentage == 0 ? DateTime.MinValue : DateTime.Now.AddTicks((long)(j.LastUpdated.Subtract(j.StartDate).Ticks * (100 / j.Percentage))).AddTicks(-j.LastUpdated.Subtract(j.StartDate).Ticks)).ToString("yyyy-MM-dd HH:mm:ss") //TODO

                             }).ToList();
            }
            return file_dtos;
        }

        public OnDemandResultDTO GetODFileResultsByID(int file_id, bool filters)
        {
            List<Result> results;
            using (var cont = new drfingerprintsContext())
            {
                results =
                              (from f in cont.Files.Where(f => f.Id == file_id)
                               join r in cont.OnDemandResults on f.Id equals r.FileId
                               join s in cont.Songs on r.SongId equals s.Id
                               select new Result()
                               {
                                   resultID = (int)r.Id,
                                   _startTime = new DateTime(r.Offset.Ticks),
                                   _endTime = new DateTime(r.Offset.Ticks).AddSeconds(r.Duration),
                                   _reference = s.Reference,
                                   _song_duration = s.Duration,
                                   _diskotekNr = s.DrDiskoteksnr,
                                   _sideNr = s.Sidenummer,
                                   _sequenceNr = s.Sekvensnummer,
                                   _accuracy = r.Accuracy,
                                   _song_offset_seconds = r.SongOffset.Value,
                                   _timeIndex = r.SongOffset.Value
                               }).ToList().OrderBy(r => r._startTime).ToList();

            }
            FileDTO file_dto;


            using (var cont = new drfingerprintsContext())
            {
                var file = cont.Files.Where(f => f.Id == file_id).FirstOrDefault();
                var job = cont.Job.Where(j => j.FileId == file_id).FirstOrDefault();

                if (job == null || file == null)
                {
                    file_dto = new FileDTO() { };

                }
                else
                {
                    file_dto = new FileDTO() { id = file.Id, file_path = file.FilePath, created = job.StartDate.ToString("yyyy-MM-dd HH:mm:ss"), percentage = job.Percentage, job_type = job.JobType, job_finished = job.Percentage == 100 ? true : false, time_used = job.LastUpdated.Subtract(job.StartDate).ToString(), user = job.User };


                    if (job.Percentage == 0) file_dto.estimated_time_of_completion = DateTime.MinValue.ToString("yyyy-MM-dd HH:mm:ss");
                    else file_dto.estimated_time_of_completion = DateTime.Now.AddTicks((long)(job.LastUpdated.Subtract(job.StartDate).Ticks * (100 / job.Percentage))).AddTicks(-job.LastUpdated.Subtract(job.StartDate).Ticks).ToString("yyyy-MM-dd HH:mm:ss");
                }
            }

            if (filters)
            {
                var filtered_results = new RuleApplier(new RuleParser().Parse("all_filters=true")).ApplyRules(results);

                var tmp = new OnDemandResultDTO()
                {
                    file = file_dto,
                    results = filtered_results.Select(r => new ResultDTO(r.GetStartTime(), r.GetEndTime(), r._reference, r.title, r.artists, r.GetAccuracy())).ToArray()
                };
                return tmp;
            }
            else
            {
                var tmp = new OnDemandResultDTO()
                {
                    file = file_dto,
                    results = results.Select(r => new ResultDTO(r.GetStartTime(), r.GetEndTime(), r._reference, r.title, r.artists, r.GetAccuracy())).ToArray()
                };
                return tmp;
            }

        }

        public Job PostTrack(string file_path, bool force)
        {
            Job job;
            using (var cont = new drfingerprintsContext())
            {
                var ext = Path.GetExtension(file_path);

                var entity = new Files { FilePath = file_path, FileType = ext };
                cont.Files.Add(entity);
                cont.SaveChanges();

                job = new Job { FileId = entity.Id, JobType = TaskType.CreateFingerprint.ToString() };
                cont.Job.Add(job);
                cont.SaveChanges();
                //TODO just use reference to files instead of saving path in arguments on the task

                // | 64 |       1 | 2020-01-08 16:12:52.582038 | Fingerprint | "\\musa01\download\ITU\MUR\csv\musa-lydfiler_no-fingerprint_2020-01-08.csv" true false |     82 | ITUMUR-STREAM02
                cont.FingerTaskQueue.Add( new FingerTaskQueue() { 
                    TaskType = TaskType.CreateFingerprint.ToString(),
                    Arguments = "\"" + file_path + "\" " + force, 
                    JobId = job.Id
                }
                );
                cont.SaveChanges();
            }
            return job;
        }

        public bool DeleteTrack(int id)
        {
            Songs track;

            using (var cont = new drfingerprintsContext())
            {
                track = cont.Songs.Where(s => s.Id == id).FirstOrDefault();
                if (track == null) return false;
                cont.Songs.Remove(track);
                cont.SaveChanges();

                var res = cont.Subfingerid.Where(s => s.Id == id).FirstOrDefault();
                if (res == null) return false;
                cont.Subfingerid.Remove(res);
                cont.SaveChanges();
            }
            return true;
        }

        //TODO: this doesnt work at all, change to using and where instead of find
        public async Task<bool> UpdateTrack(int id, string file_path)
        {
            using (var cont = new drfingerprintsContext())
            {
                var entity = await cont.Songs.FindAsync(id);

                if (entity == null) return false;

                var ext = Path.GetExtension(file_path);

                var file = new Files { FilePath = file_path, FileType = ext };
                cont.Files.Add(file);
                await cont.SaveChangesAsync();

                var job = new Job { FileId = file.Id, Arguments = entity.Id.ToString(), JobType = TaskType.CreateFingerprint.ToString() };
                cont.Job.Add(job);
                await cont.SaveChangesAsync();

                cont.TaskQueue.Add(CreateTask(TaskType.UpdateFingerprint, "", job.Id));
                await cont.SaveChangesAsync();
            }
            return true;
        }


        public TaskQueue CreateTask(TaskType type, string args, long job_id)
        {
            var task = new TaskQueue()
            {
                TaskType = type.ToString(),
                Arguments = args,
                JobId = job_id
            };
            return task;
        }
        public TaskQueue GetOldestTask()
        {
            //HACK: To ensure that the same tasks are not started by multiple workers, we use a SQL-statement for getting the oldest task.
            var sql = new SQLCommunication();
            sql.GetTask(out TaskQueue task);
            return task; //_cont.TaskQueue.Where(t => !t.Started).OrderBy(t => t.LastUpdated).FirstOrDefault();
        }

        public bool UpdateFile(long jobID, int file_duration_in_seconds)
        {
            using (var cont = new drfingerprintsContext())
            {
                var fileID = cont.Job.Where(j => j.Id == jobID).FirstOrDefault().FileId;
                var file = cont.Files.Where(f => f.Id == fileID).FirstOrDefault();
                file.Duration = file_duration_in_seconds;
                cont.SaveChanges();
            }

            return true;
        }

        public async Task<bool> UpdateJob(long jobID, float percentage)
        {
            using (var cont = new drfingerprintsContext())
            {
                var job = cont.Job.Where(j => j.Id == jobID).FirstOrDefault();

                job.Percentage = percentage;

                cont.SaveChanges();
            }
            return true;
        }

        public async Task<bool> UpdateJobStatus(long jobID, string status)
        {

            using (var cont = new drfingerprintsContext())
            {
                var job = cont.Job.Where(j => j.Id == jobID).FirstOrDefault();

                job.StatusMessage = status;

                cont.SaveChanges();
            }
            return true;
        }

        public async Task<int> InsertLiveStreamResult(Result result, string channel_id)
        { 
            LivestreamResults ls_res;
            using (var cont = new drfingerprintsContext())
               
            {
                var song = cont.Songs.Where(s => s.DrDiskoteksnr == result._diskotekNr && s.Sekvensnummer == result._sequenceNr && result._sideNr == s.Sidenummer).FirstOrDefault();
                ls_res = new LivestreamResults()
                {
                    SongId = song.Id,
                    ChannelId = channel_id,
                    PlayDate = result._startTime,
                    Offset = result._startTime.TimeOfDay,
                    Duration = (int)song.Duration,
                    Accuracy = result.GetAccuracy(),
                    SongOffset = result._song_offset_seconds
                };

                await cont.SaveChangesAsync();
            }
            return (int)ls_res.Id;
        }

        public int InsertOnDemandResult(IEnumerable<Result> results, int file_id)
        {

            using (var cont = new drfingerprintsContext())
            {
                foreach (var result in results)
                {
                    var song = cont.Songs.Where(s => s.DrDiskoteksnr == result._diskotekNr && s.Sekvensnummer == result._sequenceNr && result._sideNr == s.Sidenummer).FirstOrDefault();
                    var od_res = new OnDemandResults() { SongId = song.Id, Offset = result._startTime.TimeOfDay, Duration = (int)result._endTime.TimeOfDay.TotalSeconds - (int)result._startTime.TimeOfDay.TotalSeconds, FileId = file_id, Accuracy = result._accuracy, SongOffset = result._song_offset_seconds };

                    cont.OnDemandResults.Add(od_res);
                }

                cont.SaveChanges();
            }

            return 1;
        }

        public async Task<int> InsertLivestreamResult(Result result, string channel_id)
        {
            LivestreamResults ls_res;
            using (var cont = new drfingerprintsContext())
            {
                var song = cont.Songs.Where(s => s.DrDiskoteksnr == result._diskotekNr && s.Sekvensnummer == result._sequenceNr && result._sideNr == s.Sidenummer).FirstOrDefault();
                ls_res = new LivestreamResults() { Id = result.resultID, SongId = song.Id, Duration = (int)song.Duration, ChannelId = channel_id, Accuracy = result._accuracy, Offset = new TimeSpan((long)result._song_offset_seconds), };

                cont.LivestreamResults.Add(ls_res);
                cont.SaveChanges();
               
            } 
            return (int)ls_res.Id;
        }

        public async Task<bool> RemoveIntervalLivestreamResults(string channel_id, DateTime start, DateTime end)
        {
            var results = _cont.LivestreamResults.Where(r => r.ChannelId == channel_id).Where(r => start <= r.PlayDate && r.PlayDate <= end);
            _cont.LivestreamResults.RemoveRange(results);

            await _cont.SaveChangesAsync();

            return true;
        }

        public Stations PostStation(Stations s)
        {
            using (var cont = new drfingerprintsContext())
            {
                cont.Stations.Add(s);
                cont.SaveChanges();
            }
            return s;
        }

        public async Task<bool> DeleteStation(string id)
        {
            var station = _cont.Stations.Where(s => s.DrId == id).FirstOrDefault();

            if (station == null) return false;

            _cont.Stations.Remove(station);

            await _cont.SaveChangesAsync();

            return false;
        }

        public FileDTO PostFile(string file_path, string user)
        {
            var ext = Path.GetExtension(file_path);

            Files file;
            Job job;
            using (var cont = new drfingerprintsContext())
            {
                file = new Files { FilePath = file_path, FileType = ext };
                cont.Files.Add(file);
                cont.SaveChanges();

                job = new Job { FileId = file.Id, Arguments = "", JobType = TaskType.AudioMatch.ToString() };
                cont.Job.Add(job);
                cont.SaveChanges();

                cont.TaskQueue.Add(CreateTask(TaskType.AudioMatch, file_path, job.Id));
                cont.SaveChanges();
            }
            return new FileDTO()
            {
                id = file.Id,
                file_path = file.FilePath,
                jobId = job.Id,
                created = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };
        }

        public bool StopStationByName(string name)
        {
            using (var cont = new drfingerprintsContext())
            {
                var result = cont.Stations.Where(s => s.DrId == name).FirstOrDefault();

                if (result == null) return false;

                result.Running = false;
                cont.SaveChanges();
            }
            return true;
        }

        public bool StartStationByName(string name)
        {
            using (var cont = new drfingerprintsContext())
            {
                var result = cont.Stations.Where(s => s.DrId == name).FirstOrDefault();

                if (result == null) return false;

                result.Running = true;
                cont.SaveChanges();
            }
            return true;
        }

        public bool PostRematchTask(string name, string startTime, string endTime)
        {
            CultureInfo provider = CultureInfo.InvariantCulture;
            DateTime begin_date, end_date;
            if (startTime == null)
            {
                begin_date = DateTime.Now.AddHours(-1);
            }
            else
            {
                begin_date = DateTime.ParseExact(startTime, "yyyy-MM-ddTHH:mm", provider);
            }
            if (endTime == null)
            {
                end_date = DateTime.Now;
            }
            else
            {
                end_date = DateTime.ParseExact(endTime, "yyyy-MM-ddTHH:mm", provider);
            }

            using (var cont = new drfingerprintsContext())
            {
                var job = new Job { FileId = null, JobType = TaskType.RollingWindow.ToString(), StartDate = DateTime.Now };
                cont.Job.Add(job);
                cont.SaveChanges();
                //TODO just use reference to files instead of saving path in arguments on the task
                var taskString = startTime + " " + endTime + " " + name;
                cont.TaskQueue.Add(CreateTask(TaskType.RollingWindow, taskString, job.Id));
                cont.SaveChanges();
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
        public string GetCheckFile(string folder_name)
        {
            var result_path = $@"\\musa01\download\ITU\MUR\TrackFolderCheckResult\{folder_name}\CheckFileResults.json";
            FileInfo fileInfo = new FileInfo(result_path);

            while (IsFileLocked(fileInfo))
            {
                Thread.Sleep(10);
            }

            var checkFilesResultString = File.ReadAllText(result_path);

            return checkFilesResultString;
        }

        public CheckFilesResult PostCheckFile(string folder_name)
        {
            var result_folder = $@"\\musa01\download\ITU\MUR\TrackFolderCheckResult\{folder_name}";
            if (!Directory.Exists(result_folder))
            {
                Directory.CreateDirectory(result_folder);
            }
            var data_folder = $@"\\musa01\download\ITU\MUR\TrackFolderCheck\{folder_name}";

            var audio_files = Directory.GetFiles(data_folder);

            var checkFilesResult = new CheckFilesResult() { file_count = audio_files.Count(), file_completed_count = 0, file_results = new List<FileResult>() };

            var s = Newtonsoft.Json.JsonConvert.SerializeObject(checkFilesResult);

            string path_to_result_file = $@"\\musa01\download\ITU\MUR\TrackFolderCheckResult\{folder_name}\CheckFileResults.json";

            File.WriteAllText(path_to_result_file, s);

            using (var cont = new drfingerprintsContext())
            {
                foreach (var audio_file in audio_files)
                {
                    var file_converted = UnicodeToUTF8(audio_file);
                    var file = new Files { FilePath = file_converted, FileType = Path.GetExtension(audio_file) };
                    cont.Files.Add(file);
                    cont.SaveChanges();

                    var job = new Job { FileId = file.Id, Arguments = "", JobType = JobType.CheckFile.ToString() };
                    cont.Job.Add(job);
                    cont.SaveChanges();

                    cont.TaskQueue.Add(CreateTask(TaskType.CheckFiles, file_converted, job.Id));
                    cont.SaveChanges();
                }
            }

            return checkFilesResult;
        }

        private string UnicodeToUTF8(string strFrom)
        {
            byte[] bytSrc;
            byte[] bytDestination;
            string strTo = String.Empty;

            bytSrc = Encoding.Unicode.GetBytes(strFrom);
            bytDestination = Encoding.Convert(Encoding.Unicode, Encoding.ASCII, bytSrc);
            strTo = Encoding.ASCII.GetString(bytDestination);

            return strTo;
        }

        public List<string> GetStationIds()
        {
            var res = new List<string>();

            using (var cont = new drfingerprintsContext())
            {
                var stations = GetStations();
                foreach (var station in stations)
                {
                    res.Add(station.DrId);
                }
            }
            return res;
        }

        public int InsertFingerprint(int diskotekNr, int sideNr, int sequenceNr, string subFingerPrintRef, long duration, byte[] signature)
        {
            Songs song;
            Subfingerid fingerprint;
            using (var cont = new drfingerprintsContext())
            {
                song = new Songs { DrDiskoteksnr = diskotekNr, Sidenummer = sideNr, Sekvensnummer = sequenceNr, Reference = subFingerPrintRef, Duration = duration };
                cont.Songs.Add(song);
                cont.SaveChanges();

                fingerprint = new Subfingerid { Signature = signature, Id = song.Id };
                cont.Subfingerid.Add(fingerprint);
                cont.SaveChanges();
            }
            if (song.Id == 0) return 0;
            return song.Id;

        }

        public FingerTaskQueue GetFingerTask()
        {
            FingerTaskQueue task;
            using (var cont = new drfingerprintsContext())
            {
                using (var transaction = cont.Database.BeginTransaction())
                {
                    task = cont.FingerTaskQueue.Where(t => t.Started == false).OrderBy(t => t.LastUpdated).FirstOrDefault();

                    if (task != null)
                    {
                        task.Started = true;

                        cont.SaveChanges();
                    }

                    cont.SaveChanges();
                    transaction.Commit();
                }
            }
            return task;
        }
    }

}
