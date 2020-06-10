using AudioFingerprint;
using AudioFingerprint.Audio;
using Framework;
using MakeSubFinger;
using MySql.Data.MySqlClient;
using Nest;
using System;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;

namespace CreateAudioFingerprint
{
    public class FingerprintCreator : IFingerprintCreator
    {
        string[] formatsSupported;
        public bool _force;
        IDrRepository _rep;

        public FingerprintCreator(bool force = false)
        {
            _force = force;
            formatsSupported = Enum.GetNames(typeof(SupportedAudioFormats));
            _rep = new DrRepository(new drfingerprintsContext());
        }

        public int Create(string _audioFilePath)
        {
            var file = new FileInfo(_audioFilePath);

            string filename = Regex.Match(_audioFilePath, @"[^\\]*$").Value;

            var matches = new string[6];
            string regex;
            Match match;

            //regex = @"(\d+)-(\d+)-(\d+)_?(.+)?_(.*)\.(.*)";
            regex = @"(\d+)-(\d+)-(\d+).*\.(.*)$";
            match = Regex.Match(filename, regex);

            //HACK; this below should call extractInfoFromRegex instead and not just regex, but calling the method resulted in some wierd error on the server
            //Starts with match.Groups[1] because match.Groups[0] is the first part of the file path, which does not contain any information we need.
            matches[0] = match.Groups[1].Value; //Finds DiskotekNr.
            matches[1] = match.Groups[2].Value; //Finds SideNr.
            matches[2] = match.Groups[3].Value; //Finds SequenceNr.
            matches[3] = match.Groups[4].Value; //Finds file extension.

            matches = new[] { matches[0], matches[1], matches[2], matches[3] };

            var _repo = new DrRepository(new drfingerprintsContext());
            var song = _repo.CheckIfReferenceInSongs(int.Parse(matches[0]), int.Parse(matches[1]), int.Parse(matches[2])); //comm.SongAlreadyFingerprinted(int.Parse(matches[0]), int.Parse(matches[1]), int.Parse(matches[2]));
            var songAlreadyFingerprinted = song != null;
            if (!_force && songAlreadyFingerprinted)
            {
                Console.WriteLine("Song already fingerprinted");
                return 0; //song already in database do nothing
            }

            //If force and already fingerprinted, delete old entry from both elasticsearch and 
            if (_force && songAlreadyFingerprinted)
            {
                _repo.DeleteTrack(song.Id);
                //delete from elastic
                var settings = new ConnectionSettings(new Uri("http://itumur-search01:9200/")).DefaultIndex("dr");

                var client = new ElasticClient(settings);
                var tmp = new DeleteRequest("dr", song.Id);
                client.Delete(tmp);
            }
                

            //TODO, use ffmpeg project instead
            ExecuteCmd($@"ffmpeg -i " + "\"" + _audioFilePath + "\" -ar 44100 -ac 1 -loglevel quiet \"" + file.Name + "\"");

            //var info = ExtractInfoUsingRegex(_audioFilePath);

            string reference = matches[0] + "-" + matches[1] + "-" + matches[2];

            FingerprintSignature subFingerprint = null;
            try
            {
                subFingerprint = MakeSubFingerID(reference, filename);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }


            if (subFingerprint != null)
            {
                foreach (var match1 in matches)
                {
                    if (match1 == null)
                    {
                        using (StreamWriter fileLog = new StreamWriter(@"RegexError.txt"))
                        {
                            fileLog.WriteLine($"{filename}, {matches[0]}, {matches[1]}");
                        }
                    }
                }

                int diskotekNr = int.Parse(matches[0]);
                int sideNr = int.Parse(matches[1]);
                int sequenceNr = int.Parse(matches[2]);

                // Subfinger
                subFingerprint.DiskotekNr = diskotekNr;
                subFingerprint.SideNr = sideNr;
                subFingerprint.SequenceNr = sequenceNr;

            }

            string subFingerPrintRef = subFingerprint.Reference.ToString();

            Console.WriteLine("Fingerprinted a file.");

            // Delete the file after use:
            File.Delete(file.Name);
            // Store the fingerprint in the database

            int trackID;

            trackID = _rep.InsertFingerprint(subFingerprint.DiskotekNr, subFingerprint.SideNr, subFingerprint.SequenceNr, subFingerPrintRef, subFingerprint.Duration, subFingerprint.Signature);

            return trackID;
            
        }


        public bool Validation(string[] matches)
        {
            if (matches.Length != 4)
                return false;

            //Check if any of the matches are null.
            for (int i = 0; i < 4; i++)
            {
                if (matches[i] == null)
                {
                    Console.WriteLine("Invalid file. Some matches are null.");
                    return false;
                }
                //Check if the first 3 indexes of the string array can be parsed to integers.
                if (i < 3)
                {
                    if (!int.TryParse(matches[i], out int n))
                    {
                        Console.WriteLine("Invalid file. Couldn't parse the id into an integer.");
                        return false;
                    }
                }

            }

            //Check if the format is supported.
            if (!FormatSupported(matches[3]))
            {
                Console.WriteLine("Invalid file. File format not supported.");
                return false;
            }

            return true;
        }

        private static void ExecuteCmd(string cmd)
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
            command.WaitForExit();
            Console.WriteLine(command.StandardOutput.ReadToEnd());
        }

        /// <summary>
        /// Checks if the format is supported.
        /// </summary>
        public bool FormatSupported(string format)
        {
            //TODO could possibly be improved:
            foreach (var s in formatsSupported)
            {
                if (s.Equals(format))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Extracts info from the file using Regex.
        /// </summary>

        public string[] ExtractInfoUsingRegex(string input)
        {
            string filename = Regex.Match(input, @"[^\\]*$").Value;

            var matches = new string[6];
            string regex;
            Match match;

            //regex = @"(\d+)-(\d+)-(\d+)_?(.+)?_(.*)\.(.*)";
            regex = @"(\d+)-(\d+)-(\d+).*\.(.*)$";
            match = Regex.Match(filename, regex);

            //Starts with match.Groups[2] because match.Groups[1] is the first part of the file path, and match.Groups[0] is the entire file name.
            matches[0] = match.Groups[1].Value; //Finds DiskotekNr.
            matches[1] = match.Groups[2].Value; //Finds SideNr.
            matches[2] = match.Groups[3].Value; //Finds SequenceNr.
            matches[3] = match.Groups[4].Value; //Finds file extension.

            matches = new[] { matches[0], matches[1], matches[2], matches[3]};

            return matches;
        }


        #region Fingerprint creation

        private FingerprintSignature MakeSubFingerID(string key, string filename)
        {
            FingerprintSignature fingerprint = null;

            AudioEngine audioEngine = new AudioEngine();
            try
            {
                SpectrogramConfig spectrogramConfig = new DefaultSpectrogramConfig();

                AudioSamples samples = null;
                try
                {
                    // First read audio file and downsample it to mono 5512hz
                    samples = audioEngine.ReadMonoFromFile(filename, spectrogramConfig.SampleRate, 0, -1);
                }
                catch (Exception e)
                {
                    throw e;
                }

                // No slice the audio is chunks seperated by 11,6 ms (5512hz 11,6ms = 64 samples!)

                // A width length of 371ms (5512kHz 371ms = 2048 samples [rounded])

                fingerprint = audioEngine.CreateFingerprint(samples, spectrogramConfig);
                if (fingerprint != null)
                {
                    fingerprint.Reference = key;
                }
            }
            finally
            {
                if (audioEngine != null)
                {
                    audioEngine.Close();
                    audioEngine = null;
                }
            }

            return fingerprint;
        }

        public string GetForce()
        {
            return (_force) ? "force" : "";
        }
        #endregion
    }
}
