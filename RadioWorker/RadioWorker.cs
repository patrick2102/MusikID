using Framework;
using MatchAudio;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace RadioWorker
{
    public class RadioWorker
    {
        private int segmentDuration;
        private int overlapDuration;
        private AudioAnalyzer analyzer;

        private string rollingWindowPath;

        public RadioWorker(RadioTaskQueue tsk)
        {
            rollingWindowPath = @"\\musa01\download\ITU\MUR\RadioChannels\{0}\RollingWindow\";
            segmentDuration = 6;
            overlapDuration = 2; //FIXME Temporary make radiosampler handle the overlap and segmentduration.
            analyzer = new AudioAnalyzer(segmentDuration);
            Start(tsk);
        }

        public void Start(RadioTaskQueue tsk)
        {
            AnalyzeChunkAndSaveResultAsync(tsk);
        }

        public bool IsFileTooSmall(FileInfo fileInfo){
            if (fileInfo.Length < 37000) //Delete file if it's too small and therefore useless to analyze. This number has been calculated.
            {
                fileInfo.Delete();
                return true;
            }
            return false;
        }

        public bool GetTime(string filename, FileInfo fileInfo, out DateTime time)
        {
            //Find date from file
            var regex = @"(\d*)-(\d*)-(\d*)_(\d*)-(\d*)-(\d*)";
            var match = Regex.Match(filename, regex);

            //Check if file can be parsed:
            for (int i = 1; i < match.Groups.Count; i++)
            {
                if (!int.TryParse(match.Groups[i].Value, out int n))
                {
                    fileInfo.Delete();
                    time = new DateTime();
                    return false;
                }
            }

            time = new DateTime(int.Parse(match.Groups[1].Value),
                                int.Parse(match.Groups[2].Value),
                                int.Parse(match.Groups[3].Value),
                                int.Parse(match.Groups[4].Value),
                                int.Parse(match.Groups[5].Value),
                                int.Parse(match.Groups[6].Value));
            return true;
        }

        public async Task AnalyzeChunkAndSaveResultAsync(RadioTaskQueue ra)
        {
            Console.WriteLine($"Analyzing: {ra.ChannelId}. Task ID: {ra.Id}");

            if (!File.Exists(ra.ChunkPath))
                return;

            var sharedPath = string.Format(rollingWindowPath, ra.ChannelId);

            var fileName = Path.GetFileNameWithoutExtension(ra.ChunkPath);
            var subFolder = Path.Combine(Path.GetDirectoryName(ra.ChunkPath), Path.GetFileNameWithoutExtension(ra.ChunkPath));
            var fileInfo = new FileInfo(ra.ChunkPath);

            if (IsFileTooSmall(fileInfo))
                return;

            if (!Directory.Exists(subFolder))
                Directory.CreateDirectory(subFolder);
            else {
                Directory.Delete(subFolder, true);
                Directory.CreateDirectory(subFolder);
            }

            var ffmpeg = new Ffmpeg();

            for (int i = 0; i < segmentDuration; i += overlapDuration)
            {
                string segmentName = $@"{subFolder}\{i}_%d.wav";
                ffmpeg.StartFFmpegAudioMatch(ra.ChunkPath, i, subFolder, segmentDuration);
            }

            DirectoryInfo d = new DirectoryInfo(subFolder);

            ConcurrentQueue<FileInfo> subFiles = new ConcurrentQueue<FileInfo>(d.GetFiles());

            Parallel.ForEach(subFiles, subFile =>
            {
                if (IsFileTooSmall(fileInfo))
                    return;

                var regexSub = @"(\d+)_(\d+)";

                var matchesSub = Regex.Match(Path.GetFileNameWithoutExtension(subFile.Name), regexSub);

                if ((!int.TryParse(matchesSub.Groups[1].Value, out int n)) || (!int.TryParse(matchesSub.Groups[2].Value, out int q)))
                    return;

                var newName = subFolder + @"\" + (int.Parse(matchesSub.Groups[1].Value) + (int.Parse(matchesSub.Groups[2].Value) * segmentDuration)) + ".wav"; //First digit in match is the starting offset

                    File.Move(subFile.FullName, newName);
            });

            d.Refresh();

            subFiles = new ConcurrentQueue<FileInfo>(d.GetFiles().OrderBy(i => int.Parse(Path.GetFileNameWithoutExtension(i.Name))).ToList());

            if (!GetTime(fileName, fileInfo, out DateTime time))
                return;

            IDrRepository repo = null;
            try
            {
                repo = new DrRepository(new drfingerprintsContext());
            }
            catch (Exception e) {
                Console.WriteLine(e.ToString());
            }
            var results = new List<LivestreamResults>();

            foreach (var subfile in subFiles)
            {
                var timeSub = time.AddSeconds(int.Parse(Path.GetFileNameWithoutExtension(subfile.Name))); 
                
                try {
                    var res = analyzer.RunSubFinger(subfile.FullName, timeSub);
                    foreach (var r in res)
                    {
                        results.Add(new LivestreamResults
                        {
                            ChannelId = ra.ChannelId,
                            PlayDate = r._startTime,
                            Offset = new TimeSpan(),
                            Duration = (int)r._song_duration,
                            Accuracy = r._accuracy,
                            SongOffset = r._song_offset_seconds,
                            Song = new Songs { 
                                                            Reference = r._reference
                                                            }
                        });
                    }
                } catch (Exception e) {
                    Console.WriteLine(e.ToString());
                }

                foreach (var result in results)
                {
                    repo.InsertLivestreamResult(result);
                }

                var t = timeSub.ToString();
            }

            //Parallel.ForEach(subFiles, async (FileInfo subfile, ParallelLoopState state) =>
            //{
            //    var timeSub = time.AddSeconds(int.Parse(Path.GetFileNameWithoutExtension(subfile.Name)));

            //    try {
            //        results = analyzer.RunSubFinger(subfile.FullName, timeSub);
            //    } catch (Exception) {
            //        return;
            //    }

            //    foreach (var result in results)
            //    {
            //        await repo.InsertLivestreamResult(result, ra.ChannelId);
            //    }

            //    var t = timeSub.ToString();
            //});

            if (Directory.Exists(subFolder))
                Directory.Delete(subFolder, true);

            MoveFileToRollingWindowFolder(sharedPath, fileName, ra.ChunkPath);
        }

        public void MoveFileToRollingWindowFolder(string sharedPath, string fileName, string chunkPath) {
            var moveTo = $@"{sharedPath}{fileName}.wav";

            if (File.Exists(chunkPath))
            {
                File.Copy(chunkPath, moveTo);
                File.Delete(chunkPath);
            }
        } 

    }
}
