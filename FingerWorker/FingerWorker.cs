using CreateAudioFingerprint;
using CreateElasticIndex;
using MakeSubFinger;
using MatchAudio;
using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using Framework;
using System.Threading.Tasks;

namespace FingerWorkerProcess
{
    public class FingerWorker
    { 

        IDrRepository _repo;

        public FingerWorker()
        {
            _repo = new DrRepository(new drfingerprintsContext());
        }
        public async void Start()
        {
            while (true)
            {
                //var task = _repo.GetFingerTask();
                new SQLCommunication().GetFingerTask(out FingerTaskQueue task);
                if (task != null)
                {
                    await ExecuteAssignment(task);

                    await CleanUp(task);

                }
                else // if no new assignments where found then sleep before trying again.  Done in order to avoid too many queries to the DB.
                {
                    Thread.Sleep(200);
                }
            }
        }

        private async Task<bool> ExecuteAssignment(FingerTaskQueue ass)
        {
            string audio_path;
                switch (ass.TaskType)
                {
                    case "Fingerprint": //HACK fingerprint is old task type.
                    case nameof(TaskType.CreateFingerprint):
                        var parts = Regex.Matches(ass.Arguments, @"[\""].+?[\""]|[^ ]+")
                            .Cast<Match>()
                            .Select(m => m.Value)
                            .ToList();
                        audio_path = parts[0];
                        var force = bool.Parse(parts[1]);

                        var handler = new FingerprintPathHandler(new FingerprintCreator(force));
                        handler.Handle(audio_path, ass.JobId);
                        await _repo.UpdateJob(ass.JobId, 100);
                        break;

                    case nameof(TaskType.IndexSingle):
                        var songID = int.Parse(ass.Arguments);
                        new CreateElasticIndexSingle().IndexSingleElement("dr", songID);
                        break;

                    //the assignment did not match a case, should not happen
                    default:
                        return false;
                }
                return true;
        }


        private async Task<bool> CleanUp(FingerTaskQueue ass)
        {
            switch (ass.TaskType)
            {
                case nameof(TaskType.CreateFingerprint):
                    await _repo.DeleteFingerTask(ass.Id);
                    //new SQLCommunication().DeleteFingerTask(ass.ID);
                    Console.WriteLine("done with Fingerprint Task");
                    break;


                case nameof(TaskType.IndexSingle):
                    await _repo.DeleteFingerTask(ass.Id);
                    Console.WriteLine("done with IndexSingle Task");
                    break;
                default:
                    return false;
            }
            return true;
        }
    }
}
