using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;

namespace MatchAudio
{
    public class Ffmpeg
    {
        public string _timeArgument;
        public string _chunkName;

        public (string, string) BuildArgument(string filePath, int i, string chunkSize, string segmentName) {
            var fileExtension = Path.GetExtension(filePath);
            var arguments = $"-i \"{filePath}\" -ss {i} -segment_time {chunkSize} -c copy -f segment \"{segmentName}\"";
            switch (fileExtension) {
                case ".mp3":
                    return (arguments, filePath);
                case ".wav":
                    return (arguments, filePath);
                case ".m4a":
                    return (arguments, filePath);
                case ".mp4":
                    var output = $@"analysis\{Path.GetFileNameWithoutExtension(filePath)}_output.mp3";
                    arguments = $"-y -i \"{filePath}\" -crf 0 \"{output}\"";
                    Start(arguments);
                    return BuildArgument(output, i, chunkSize, segmentName);
                default:
                    Console.WriteLine("Format not supported.");
                    throw new NotSupportedException(JobStatus.not_supported.ToString());
            }
        }

        public string MakeTimeArgument() {
            _timeArgument = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss").Replace("/", "-").Replace(":", "-").Replace(" ", "_");
            return _timeArgument;
        }

        public string MakeDurArgument(double streamDur) {
            return streamDur.ToString().Replace(",", ".");
        }

        public string MakeChunkNameArgument(string path, string channelId){
            var time = MakeTimeArgument();
            _chunkName = $@"{path}{channelId}-{time}.wav";
            return _chunkName;
        }

        public void StartFFmpegSampler(string url, double streamDur, string path, string channelID) {
            var dur = MakeDurArgument(streamDur);
            var chunkName = MakeChunkNameArgument(path, channelID);
            var arguments = $"-y -i \"{ url}\" -t {dur} \"{chunkName}\"";
            Console.WriteLine($"Analyzing: {channelID}");
            try
            {
                Start(arguments);
                Console.WriteLine($"Success: {channelID}");
            } catch (Exception e) {
                Console.WriteLine(e.Message);
                Console.WriteLine($"Failure: {channelID}");
            }
            //TODO upload to repo 
        }

        public void StartFFmpegAudioMatch(string filePath, int i, string chunkFolder, int chunkSize) {
            string segmentName = $@"{chunkFolder}\{i}_%d.wav";
            var time = DateTime.Today.AddSeconds(chunkSize).ToString("T", DateTimeFormatInfo.InvariantInfo);
            var (arguments, file) = BuildArgument(filePath, i, time, segmentName);
            Start(arguments);
            if (Path.GetExtension(filePath).Equals(".mp4")) {
                if (File.Exists(filePath))
                    File.Delete(file);
            }
        }

        public void Start(string arguments) {
            if (arguments == null)
            {
                Console.WriteLine("No arguments.");
                throw new ArgumentException("");
            }

            Process proc = new Process();
            proc.StartInfo.FileName = @"ffmpeg.exe";
            proc.StartInfo.Arguments = arguments;
            proc.StartInfo.UseShellExecute = false;
            proc.StartInfo.CreateNoWindow = true;
            proc.StartInfo.RedirectStandardInput = true;
            proc.StartInfo.RedirectStandardOutput = true;
            proc.Start();
            proc.StandardInput.Flush();
            proc.StandardInput.Close();
            proc.WaitForExit();
            if (proc.ExitCode != 0)
            {
                Console.WriteLine($"FFMPEG failed with {arguments}");
                throw new Exception(JobStatus.ffmpeg_fail.ToString());
            }
        }
    }
}
