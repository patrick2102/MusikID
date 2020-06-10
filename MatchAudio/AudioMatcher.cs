using Framework;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace MatchAudio
{
    public class AudioMatcher
    {
        private readonly int _segmentDuration;

        public AudioMatcher(int dur)
        {
            _segmentDuration = dur;
        }

        public void Match(DrRepository _repo, string _inputPath, long jobID, int file_id)
        {
            var dict = new AudioAnalysisDictionary();
            var analyzer = new AudioAnalyzer(_segmentDuration);
            analyzer.ChunkAudioFileAndRunSubFinger(_repo, dict, _inputPath, jobID, file_id, _segmentDuration);
        }

        

        public async System.Threading.Tasks.Task MatchRollingWindowAsync(DrRepository _repo, string sharedPathForRadioChannels, DateTime start, DateTime end, string channel_id, long jobID)
        {
            FileInfo[] files = new DirectoryInfo($@"{sharedPathForRadioChannels}\{channel_id}\RollingWindow\").GetFiles();
            var format = "dd-MM-yyyy_HH-mm-ss";
            var provider = new CultureInfo("fr-FR");

            var filtered = new List<FileInfo>();
            
            foreach (var file in files )
            {
                var splits = file.Name.Split('_');
                
                var date = DateTime.ParseExact($"{splits[0]}_{splits[1].Split('.')[0]}", format, provider);
                if (WithinRange(start, end, date))
                {
                    filtered.Add(file);
                }
            }
            filtered = filtered.OrderBy(f => f.Name).ToList();
            

            var dict = new AudioAnalysisDictionary(channel_id);

            var analyzer = new AudioAnalyzer(_segmentDuration);

            var results = new List<Result>();

            var i = 0f;
            foreach (var chunk in filtered) {
                // This should be threaded??
                var splits = chunk.Name.Split('_');
                var date = DateTime.ParseExact($"{splits[0]}_{splits[1].Split('.')[0]}", format, provider);

                foreach (var res in analyzer.RunSubFinger(chunk.FullName, date)) results.Add(res);

                i++;

                float percentage = (i / filtered.Count) * 100;

                _repo.UpdateJob(jobID, percentage);
            }

            foreach (var result in results)
            {
                await _repo.InsertLivestreamResult(result, channel_id);
            }
        }

        

        private bool WithinRange(DateTime startDate, DateTime endDate, DateTime dateToCheck)
        {
            return dateToCheck >= startDate && dateToCheck < endDate;
        }
    }
}
