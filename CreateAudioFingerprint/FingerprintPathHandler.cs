using AudioFingerprint;
using CreateAudioFingerprint;
using CreateElasticIndex;
using Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MakeSubFinger
{
    public class FingerprintPathHandler
    {
        private readonly IFingerprintCreator _fingerprintCreator;
        private readonly IDrRepository _repo;

        public FingerprintPathHandler(IFingerprintCreator printCreator)
        {
            _fingerprintCreator = printCreator;
            _repo = new DrRepository(new drfingerprintsContext());
        }

        public void Handle(string audioPath, long jobID) {

            audioPath = audioPath.Substring(1, audioPath.Length - 2); //HACK remove " in front and back

            var pathExtension = Path.GetExtension(audioPath);

            //checks if the path is a csv, directory or a single file
            if (pathExtension.Equals(".csv"))
                GetPathArrayFromCSV(audioPath, jobID);
            else if (IsFileAllowedFormat(pathExtension))
                HandleFileAsSingleAudioFile(audioPath);
            else throw new Exception("File Format not Supported");
        }

        private bool IsFileAllowedFormat(string pathExtension)
        {
            var extension = pathExtension;
            if (extension[0] == '.')
                extension = extension.Remove(0, 1);
            foreach (Enum s in Enum.GetValues(typeof(SupportedAudioFormats)))
            {
                if (extension.Equals(s.ToString()))
                    return true;
            }
            return false;
        }

        // deprecated
        private void CreateFingerPrintsFromPathArray(string[] files, long jobID)
        {
            int toProcess = files.Length;

            using (ManualResetEvent resetEvent = new ManualResetEvent(false))
            {
                float percentage;
                foreach (var file in files)
                {
                    try
                    {
                        int id = _fingerprintCreator.Create(file);
                        //now add new fp to elastic index
                        if (id != 0)
                        {
                            new CreateElasticIndexSingle().IndexSingleElement("dr", id);
                        }

                    }
                    catch (Exception e)
                    {
                        using (System.IO.StreamWriter exWriter = new System.IO.StreamWriter(@"fingerprintCreationFailure.txt"))
                        {
                            exWriter.WriteLine("this file does not work " + file);
                            exWriter.WriteLine(e.ToString());
                        }
                    }
                    // Safely decrement the counter
                    if (Interlocked.Decrement(ref toProcess) == 0)
                        resetEvent.Set();

                    percentage = ((float)(files.Length - toProcess) / files.Length) * 100;

                    // Update job percentage. 
                    _repo.UpdateJob(jobID, percentage);


                }
                resetEvent.WaitOne();
                // HACK do we need this?
                // final update after loop, should give 100%

                percentage = ((files.Length - toProcess) / files.Length) * 100;
                _repo.UpdateJob(jobID, percentage);
            }
        }

        private void HandleFileAsSingleAudioFile(string audio_path)
        {
            var extension = Path.GetExtension(audio_path);

            if (extension[0] == '.')
                extension = extension.Remove(0, 1);
 
            var file_id = _repo.InsertFile(audio_path, extension);

            int jobID = _repo.InsertJob(JobType.Fingerprint, file_id, "");

            int id = _fingerprintCreator.Create(audio_path);
            
            // now add new fp to elastic index and update job.
            new CreateElasticIndexSingle().IndexSingleElement("dr",id);
            _repo.UpdateJob(jobID, 100);
        }

        private void GetPathArrayFromCSV(string file_name, long jobID)
        {
            var files = new List<string>();    

            try
            {
                using (var reader = new StreamReader($@"{file_name}"))
                {
                    reader.ReadLine(); //read first line which is description of the colums

                        while (!reader.EndOfStream) //read untill file is empty
                        {
                            var line = reader.ReadLine();
                            if (line.Equals(""))
                                continue;
                            var values = line.Split(','); //comma seperated

                            //HACK: The CSV-file contained some files with excessive quotation marks, which ruined the file path.Therefore we remove all occurences of " here.
                            //TODO: Talk to PO about CSV paths
                            var filePath = values[1].Replace("\"", "");
                            files.Add(filePath);
                        }
                }
            }
            catch (Exception e)
            {
                using (StreamWriter fileLog = new StreamWriter(@"HandlerErrorInCSV.txt"))
                {
                    fileLog.WriteLine($"exception: {e.StackTrace}");
                }
            }
            CreateFingerPrintsFromPathArray(files.ToArray(), jobID);
        }
    }
}
