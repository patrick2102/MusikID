#region License
// Copyright (c) 2015-2017 Stichting Centrale Discotheek Rotterdam.
// 
// website: https://www.muziekweb.nl
// e-mail:  info@muziekweb.nl
//
// This code is under MIT licence, you can find the complete file here: 
// LICENSE.MIT
#endregion#region License
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AudioFingerprint;
using AudioFingerprint.Audio;
using AudioFingerprint.WebService;
using System.Threading;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Collections.Concurrent;
using Elasticsearch.Net;
using Framework;

namespace MatchAudio
{
    public class AudioAnalyzer
    {
        //TODO comment
        private AudioEngine audioEngine;
        private Stopwatch totalTimeOfProgram;

        private Stopwatch queryTime;
        private int _segmentDuration;
        private int _overlapTime;
        private Nest.ElasticClient client;

        private string sharedPathOnDemand = $@"\\musa01\Download\ITU\MUR\OnDemand\";

        /// <summary>
        /// Setup the class, by intializing path's and the audio engine.
        /// Reads the Fingerprint.ini file for the settings
        /// </summary>
        public AudioAnalyzer(int segmentDuration, int overlapTime = 2)
        {
            //TODO avoid hardcoded magic strings
            var indexName = "dr";

            var uris = new List<Uri>(){
                new Uri("http://itumur-index01:9200/"),
                new Uri("http://itumur-index02:9200/"),
                new Uri("http://itumur-index03:9200/"),
                new Uri("http://itumur-index04:9200/"),
                new Uri("http://itumur-index05:9200/"),
                new Uri("http://itumur-index06:9200/")
            };
            var pool = new StaticConnectionPool(uris, true);
            var settings = new Nest.ConnectionSettings(pool).DefaultIndex(indexName).RequestTimeout(TimeSpan.FromMinutes(3)).MaximumRetries(10).ConnectionLimit(80);
            client = new Nest.ElasticClient(settings);
            _segmentDuration = segmentDuration;
            _overlapTime = overlapTime;
            AudioFingerprint.Math.SimilarityUtility.InitSimilarityUtility();
            audioEngine = new AudioEngine();
        }

        #region SubFingerprint routines

        /// <summary>
        /// Test the subfinger fingerprints, by trying to find a 15 second audio in 3 different bitrates.
        /// 
        /// Needed are:
        /// 1. MySQL database
        /// 2. CreateAudioFingerprint (Fills the MySQL database with fingerprints)
        /// 3. CreateInversedFingerprintIndex (Create a lucene reversed search index, needed to identity a fingerprint)
        /// 4. This program to find a 15 second fragment of audio
        /// </summary>
        public List<Result> RunSubFinger(string chunkPath, DateTime time)
        {
            totalTimeOfProgram = new Stopwatch();
            totalTimeOfProgram.Start();

            SubFingerprintQuery query = new SubFingerprintQuery(client);

            FileInfo file = new FileInfo(chunkPath);

            queryTime = new Stopwatch();
            queryTime.Start();

            string name = file.FullName;
            var fsQuery = CreateSubFingerprintFromAudio(name);

            if (fsQuery == null) return null;

            var answers = query.MatchAudioFingerprint(fsQuery);

            if (answers.ResultEntries.Count > 0)
            {
                var temp = new List<Result>();
                foreach (var answer in answers.ResultEntries)
                {
                    PrintAnswer(answers);
                    temp.Add(ConvertFromResultEntryToResult(answer, time));
                }
                return temp;
            }
            return new List<Result> { new Result("NaN", time, time.AddSeconds(_segmentDuration), -1) };
        }



        public void ChunkAudioFileAndRunSubFinger(DrRepository _repo, string filePath, long jobID, int file_id, int chunkSize = 6)
        {
            var dir_path = $@"{sharedPathOnDemand}\Job_{jobID}";

            if (Directory.Exists(dir_path))
            {
                Directory.Delete(dir_path, true);
                Directory.CreateDirectory(dir_path);
            }
            Directory.CreateDirectory(dir_path);

            var ffmpeg = new Ffmpeg();

            Console.WriteLine($"Handling file: {filePath}");
            Console.WriteLine($"Currently chunking with FFMPEG.");

            for (int i = 0; i < _segmentDuration; i += _overlapTime)
            {
                ffmpeg.StartFFmpegAudioMatch(filePath, i, dir_path, chunkSize);
            }
            Console.WriteLine($"FFMPEG done with chunking. Will now match.");

            int file_duration_in_seconds = _overlapTime * Directory.GetFiles(dir_path).Length;

            _repo.UpdateFile(jobID, file_duration_in_seconds);

            foreach (var file in Directory.GetFiles(dir_path))
            {
                var regex = @"(\d+)_(\d+)";

                var matches = Regex.Match(Path.GetFileNameWithoutExtension(file), regex);

                if ((!int.TryParse(matches.Groups[1].Value, out int n)) || (!int.TryParse(matches.Groups[2].Value, out int q)))
                    continue;

                var name = $@"{dir_path}" + @"\" + (int.Parse(matches.Groups[1].Value) + (int.Parse(matches.Groups[2].Value) * _segmentDuration)) + ".wav"; //First digit in match is the starting offset

                File.Move(file, name);
            }

            RunSubFingerOnMultipleChunks(_repo, dir_path, jobID, file_id);

            if (Directory.Exists(dir_path))
                Directory.Delete(dir_path, true);
        }
        

        public void RunSubFingerOnMultipleChunks(DrRepository _repo, string chunkFolderPath, long jobID, int file_id)
        {
            var results = new ConcurrentQueue<Result>();

            totalTimeOfProgram = new Stopwatch();
            totalTimeOfProgram.Start();

            SubFingerprintQuery query = new SubFingerprintQuery(client);

            DirectoryInfo d = new DirectoryInfo(chunkFolderPath);

            //TODO fix wav with generic stuff
            FileInfo[] files = d.GetFiles("*.wav").Where(i => int.TryParse(Path.GetFileNameWithoutExtension(i.Name), out int n)).OrderBy(i => int.Parse(Path.GetFileNameWithoutExtension(i.Name))).ToArray();

            var start_time = DateTime.Today;

            int filesAnalyzed = 0;

            int toProcess = files.Length;

            using (ManualResetEvent resetEvent = new ManualResetEvent(false))
            {
                foreach (FileInfo file in files)
                {
                    if (file.Length < 37000)
                    {
                        file.Delete();
                        Interlocked.Increment(ref filesAnalyzed);
                        continue;
                    }
                    ThreadPool.QueueUserWorkItem(
                       new WaitCallback(x => {

                           var time = (DateTime)x;
                           string name = file.FullName;
                           var fsQuery = CreateSubFingerprintFromAudio(name);
                           Result result;
                           //var repo = new DrRepository(new drfingerprintsContext());
                           if (fsQuery != null)
                           {
                               var answers = query.MatchAudioFingerprint(fsQuery);

                               foreach (var answer in answers.ResultEntries)
                               {
                                   result = ConvertFromResultEntryToResult(answer, time);
                                   _repo.InsertOnDemandResult(new List<Result> { result }, file_id);
                                   //results.Enqueue(result);
                                   //dict.UpdateSingleResult(_repo, result);
                               }

                               if (answers.ResultEntries.Count == 0)
                               {

                                   result = new Result(
                                                       "NaN",
                                                       time,
                                                       time.AddSeconds(_segmentDuration),
                                                       -1
                                                       );
                                   _repo.InsertOnDemandResult(new List<Result> { result } , file_id);
                                   //dict.UpdateSingleResult(_repo, result);
                                   //results.Enqueue(result);
                               }
                           }
                            Interlocked.Increment(ref filesAnalyzed);
                               
                            var percentage = ((float)(filesAnalyzed) / files.Length) * 100;
                           Console.Write($"\r{(int)percentage}% done.");
                           _repo.UpdateJob(jobID, percentage);
                            // Safely decrement the counter
                            if (percentage == 100)
                                resetEvent.Set();
                       }), start_time);

                    start_time = start_time.AddSeconds(_overlapTime);
                    //start_time = start_time.AddSeconds(_segmentDuration);
                }
                resetEvent.WaitOne();
            }
            Console.WriteLine($"Done.");

            //HACK
            //new DrRepository(new drfingerprintsContext()).InsertOnDemandResult(results.ToList(), file_id);
            //snew SQLCommunication().UpdateJob(jobID, percentage);


            _repo.UpdateJob(jobID, 100);
        }

        private Result ConvertFromResultEntryToResult(ResultEntry answer, DateTime start_time)
        {
            if (answer != null)
            {
                var regex = @"(\d+)-(\d+)-(\d+)$";
                var match = Regex.Match(answer.Reference.ToString(), regex);
                var diskotekNr = int.Parse(match.Groups[1].Value);
                var sideNr = int.Parse(match.Groups[2].Value);
                var sequenceNr = int.Parse(match.Groups[3].Value);
                var bits = 256f * 256f;
                var similarity = (answer == null) ? -1.0f : ((bits - answer.Similarity) / bits) * 100;
                var song_offset = answer.TimeIndex * 11.6f;
                return new Result(answer.Reference.ToString(), start_time, start_time.AddSeconds(_segmentDuration),
                    similarity, diskotekNr, sideNr, sequenceNr, song_offset);
            }
            return new Result("", start_time, start_time.AddSeconds(_segmentDuration)
                );
        }



        /// <summary>
        /// Read a audio file (remember for sub fingerprints no more than 15 seconds)
        /// Downsample it to mono and 5512Hz
        /// Use the samples to create a fingerprint
        /// 
        /// return a fingerprint signature.
        /// </summary>
        private FingerprintSignature CreateSubFingerprintFromAudio(string filename)
        {
            DateTime startTime = DateTime.Now;
            SpectrogramConfig spectrogramConfig = new DefaultSpectrogramConfig();

            // First read audio file and downsample it to mono 5512hz
            AudioSamples samples = audioEngine.ReadMonoFromFile(filename, spectrogramConfig.SampleRate, 0, -1);
            //Console.WriteLine(string.Format("Resample tot mono {0}hz : {1:##0.000} sec.", spectrogramConfig.SampleRate, (DateTime.Now - startTime).TotalMilliseconds / 1000));

            startTime = DateTime.Now;
            // Now slice the audio in chunks seperated by 11,6 ms (5512hz 11,6ms = 64 samples!)
            // An with length of 371ms (5512kHz 371ms = 2048 samples [rounded])
            FingerprintSignature fsQuery = audioEngine.CreateFingerprint(samples, spectrogramConfig);
            // Console.WriteLine(string.Format("Hashing audio to fingerprint : {0:##0.000} sec.", (DateTime.Now - startTime).TotalMilliseconds / 1000));

            return fsQuery;
        }

        /// <summary>
        /// Some radio channels do audio stretching. (Letting the audio run faster or slower, then it orginal was).
        /// The algoritme for the fingerprint, can't cope with it. So you have to slow ro speed to audio chunk back to it's 
        /// orginal speed before matching.
        /// 
        /// eg. Radio538 in the Netherlands speeds up it sound with a factor of 1.4
        /// To bring the audio back for matching you enter a stretchRate of -1.4
        /// 
        /// Finding a stretchrate is trying different values on a know audio fragment until you get the best 
        /// audio recognition.
        /// </summary>
        private FingerprintSignature CreateSubFingerprintFromAudioWithTimeStretch(string filename, float stretchRate)
        {
            DateTime startTime = DateTime.Now;

            AudioSamples samples = audioEngine.TimeStretch(filename, stretchRate);

            SpectrogramConfig spectrogramConfig = new DefaultSpectrogramConfig();
            // First read audio file and downsample it to mono 5512hz
            samples = audioEngine.Resample(samples.Samples, samples.SampleRate, 2, 5512);
            Console.WriteLine(string.Format("Resample tot mono {0}hz : {1:##0.000} sec.", spectrogramConfig.SampleRate, (DateTime.Now - startTime).TotalMilliseconds / 1000));

            startTime = DateTime.Now;
            // Now slice the audio in chunks seperated by 11,6 ms (5512hz 11,6ms = 64 samples!)
            // An with length of 371ms (5512kHz 371ms = 2048 samples [rounded])
            FingerprintSignature fsQuery = audioEngine.CreateFingerprint(samples, spectrogramConfig);
            Console.WriteLine(string.Format("Hashing audio to fingerprint : {0:##0.000} sec.", (DateTime.Now - startTime).TotalMilliseconds / 1000));

            return fsQuery;
        }

        #endregion

        #region Some info and test stuff

        private void PrintAnswer(Resultset result)
        {
            if (result != null)
            {
                Console.WriteLine("======================================================================");
                Console.WriteLine("Algorithm: " + result.Algorithm.ToString());
                Console.Write("Stats: ");
                Console.Write("Total=" + (result.QueryTime.TotalMilliseconds / 1000).ToString("#0.000") + "s");
                Console.Write(" | FingerQry=" + (result.FingerQueryTime.TotalMilliseconds / 1000).ToString("#0.000") + "s");
                Console.Write(" | FingerID=" + (result.FingerLoadTime.TotalMilliseconds / 1000).ToString("#0.000") + "s");
                Console.Write(" | Match=" + (result.MatchTime.TotalMilliseconds / 1000).ToString("#0.000") + "s");
                Console.Write(" | Match=" + (result.MatchTime.TotalMilliseconds / 1000).ToString("#0.000") + "s");
                Console.WriteLine();
                Console.WriteLine();

                foreach (ResultEntry item in result.ResultEntries)
                {
                    Song song = new Song();

                    Console.WriteLine("SearchPlan  : " + item.SearchStrategy.ToString());
                    Console.WriteLine("Reference   : " + item.Reference.ToString());
                    // AcoustID is for complete track so position in track is pointless
                    if (item.TimeIndex >= 0)
                    {
                        Console.WriteLine("Position    : " + (item.Time.TotalMilliseconds / 1000).ToString("#0.000") + " sec");
                    }
                    else
                    {
                        Console.WriteLine("Position    : Match on complete track");
                    }
                    if (result.Algorithm == FingerprintAlgorithm.AcoustIDFingerprint)
                    {
                        Console.WriteLine(string.Format("Match perc. : {0}%", item.Similarity));
                    }
                    else
                    {
                        Console.WriteLine("BER         : " + item.Similarity.ToString());
                    }

                    Console.WriteLine();
                } //foreach
                Console.WriteLine("======================================================================");
            }
        }
        #endregion
    }
}