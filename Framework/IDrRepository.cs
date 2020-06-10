using Framework.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Framework
{
    public interface IDrRepository
    {
        IEnumerable<TrackDTO> GetSongs(int limit);
        TrackDTO GetTrackByDrNr(string dr_nr);
        IEnumerable<Stations> GetStations();

        Stations GetStationsByName(string name);
        LivestreamResultsDTO GetLivestreamResults(string channel_name, bool filters, string begin, string end);
        IEnumerable<Job> GetJobs();
        void CheckForFailedTasks();
        FileDTO GetJobByID(int id);
        FileDTO GetODFileByID(int id);
        IEnumerable<FileDTO> GetODFiles(int limit);
        OnDemandResultDTO GetODFileResultsByID(int file_id, bool filters);
        Job PostTrack(string songPath, bool force);

       // IEnumerable<Files> GetODFiles();
        FingerTaskQueue GetFingerTask();
        bool DeleteTrack(int id);
        Task<bool> UpdateTrack(int id, string file_path);

        //Task<bool> CreateTask(TaskQueue task);
        Task<bool> DeleteTask(long id);
        Task<bool> DeleteFingerTask(long id);
        TaskQueue GetOldestTask();
        bool UpdateFile(long jobID, int file_duration_in_seconds);
        Task<bool> UpdateJob(long jobID, float percentage);
        Task<int> InsertLiveStreamResult(Result result, string channel_id);
        int InsertOnDemandResult(IEnumerable<Result> result, int file_id);
        Task<bool> RemoveIntervalLivestreamResults(string channel_id, DateTime start, DateTime end);
        Stations PostStation(Stations s);
        Task<bool> DeleteStation(string id);

       FileDTO PostFile(string file_path, string user);
        bool StartStationByName(string name);
        bool StopStationByName(string name);
        bool PostRematchTask(string name, string startTime, string endTime);
        CheckFilesResult PostCheckFile(string folder_name);
        string GetCheckFile(string folder_name);
        Songs CheckIfReferenceInSongs(int diskoteks_nr, int side_nr, int skærings_nr);
        int InsertLivestreamResult(LivestreamResults result);
        int InsertFingerprint(int diskotekNr, int sideNr, int sequenceNr, string subFingerPrintRef, long duration, byte[] signature);
        int InsertFile(string audio_path, string extension);
        int InsertJob(JobType fingerprint, int file_id, string args);
        IEnumerable<FileDTO> GetFiles(int limit);
        FileDTO GetFileByID(int id);
    }
}
