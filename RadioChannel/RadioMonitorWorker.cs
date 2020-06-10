using DatabaseCommunication;
using Lucene.Net.Search;
using MatchAudio;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace RadioChannel
{
    public class RadioMonitorWorker
    {
        private IndexSearcher _indexSubFingerLookup;
        private RadioAssignment _rass;

        private int segmentDuration;
        private int overlapDuration;

        private AudioAnalysisDictionary aad;
        private AudioAnalyzer analyzer;

        private string rollingWindowPath;

        public RadioMonitorWorker(RadioAssignment rass)
        {
            rollingWindowPath = @"\\musa01\download\ITU\MUR\RadioChannels\{0}\RollingWindow\";
            segmentDuration = 6;
            overlapDuration = 2;
            aad = new AudioAnalysisDictionary(rass.Channel_id);
            analyzer = new AudioAnalyzer(segmentDuration);
            _rass = rass;
        }

        public void Start()
        {
            var startListenTime = DateTime.Now;
            AnalyzeChunkAndSaveResult(_rass);
        }

        public void AnalyzeChunkAndSaveResult(RadioAssignment ra)
        {
            //try
            //{
            var sharedPath = string.Format(rollingWindowPath, ra.Channel_id);

            Console.WriteLine("Analyzing: " + ra.Chunk_path);

            var filename = Path.GetFileNameWithoutExtension(ra.Chunk_path);

            var subfolder = Path.Combine(Path.GetDirectoryName(ra.Chunk_path), Path.GetFileNameWithoutExtension(ra.Chunk_path));

            var file_info = new FileInfo(ra.Chunk_path);

            if (file_info.Length < 37000)
            {
                file_info.Delete();
                return;
            }

            if (!System.IO.Directory.Exists(subfolder))
            {
                System.IO.Directory.CreateDirectory(subfolder);
            }
            else
            {
                System.IO.Directory.Delete(subfolder, true);
                System.IO.Directory.CreateDirectory(subfolder);
            }

            for (int i = 0; i < segmentDuration; i += overlapDuration)
            {
                string segmentName = $@"{subfolder}\{i}_%d.wav";
                ExecuteCmd($@"ffmpeg -ss {i} -i {ra.Chunk_path} -f segment -segment_time {segmentDuration} -loglevel quiet -y -c copy {segmentName}");
            }

            //Find date from file
            var regex = @"(\d*)-(\d*)-(\d*)_(\d*)-(\d*)-(\d*)";
            var match = Regex.Match(filename, regex);

            //Check if file can be parsed:
            for (int i = 1; i < match.Groups.Count; i++)
            {
                if (!int.TryParse(match.Groups[i].Value, out int n))
                {
                    file_info.Delete();
                    return;
                }
            }

            var time = new DateTime(int.Parse(match.Groups[1].Value),
                                    int.Parse(match.Groups[2].Value),
                                    int.Parse(match.Groups[3].Value),
                                    int.Parse(match.Groups[4].Value),
                                    int.Parse(match.Groups[5].Value),
                                    int.Parse(match.Groups[6].Value));

            ConcurrentQueue<FileInfo> subFiles;

            DirectoryInfo d = new DirectoryInfo(subfolder);

            subFiles = new ConcurrentQueue<FileInfo>(d.GetFiles());

            Parallel.ForEach(subFiles, subFile =>
            {
                if (subFile.Length < 37000)//Checks if the file is less than around 4.5 seconds long.
                    {
                    File.Delete(subFile.FullName);
                    return;
                }

                var regexSub = @"(\d+)_(\d+)";

                var matchesSub = Regex.Match(Path.GetFileNameWithoutExtension(subFile.Name), regexSub);

                if ((!int.TryParse(matchesSub.Groups[1].Value, out int n)) || (!int.TryParse(matchesSub.Groups[2].Value, out int q)))
                    return;

                var newName = subfolder + @"\" + (int.Parse(matchesSub.Groups[1].Value) + (int.Parse(matchesSub.Groups[2].Value) * segmentDuration)) + ".wav"; //First digit in match is the starting offset

                    File.Move(subFile.FullName, newName);
            });

            d.Refresh();
            subFiles = new ConcurrentQueue<FileInfo>(d.GetFiles().OrderBy(i => int.Parse(Path.GetFileNameWithoutExtension(i.Name))).ToList());

            Parallel.ForEach(subFiles, (FileInfo subfile, ParallelLoopState state) =>
            {
                var timeSub = time.AddSeconds(int.Parse(Path.GetFileNameWithoutExtension(subfile.Name)));

                List<Result> results = null;
                try
                {
                    results = analyzer.RunSubFinger(subfile.FullName, timeSub);
                }
                catch (Exception e)
                {
                    new SQLCommunication().InsertError(e.StackTrace, ra.JobID);
                    return;
                }

                if (results == null) return;

                aad.Update(results);

                var t = timeSub.ToString();

                var moveTo = $@"{sharedPath}{t.Replace(":", "-").Replace(" ", "_")}.wav";

                try
                {
                    File.Copy(subfile.FullName, moveTo);
                }
                catch (IOException)
                {
                    state.Break();
                }

                Console.WriteLine($"Query for: {subfile} completed.");
            }
            );

            if (System.IO.Directory.Exists(subfolder))
            {
                System.IO.Directory.Delete(subfolder, true);
            }

            if (File.Exists(ra.Chunk_path))
            {
                File.Delete(ra.Chunk_path);
            }
            //} 
            //catch (System.IO.FileNotFoundException e) {
            //        //Remove task from queue in db.
            //    }
        }

        private void ExecuteCmd(string cmd)
        {
            string currentDirectory = Path.GetDirectoryName(Path.GetDirectoryName(System.IO.Directory.GetCurrentDirectory()));
            Process command = new Process();
            command.StartInfo.FileName = "cmd.exe";
            command.StartInfo.RedirectStandardInput = true;
            command.StartInfo.RedirectStandardOutput = true;
            command.StartInfo.UseShellExecute = false;
            command.Start();

            command.StandardInput.WriteLine(cmd);

            command.StandardInput.Flush();
            command.StandardInput.Close();
            Console.WriteLine(command.StandardOutput.ReadToEnd());
        }

    }
}
