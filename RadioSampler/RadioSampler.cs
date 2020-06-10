using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RadioWorker;
using DatabaseCommunication;
using Framework;

namespace RadioSampler
{
    public class RadioSampler
    {

        public static void Main(string[] args)
        {
            new RadioSampler();
        }

        public RadioSampler()
        {
            var radios = new ConcurrentDictionary<string, RadioStationHandler>();

            var repo = new DrRepository(new drfingerprintsContext());
            //new SQLCommunication().GetRunningStations(out List<string[]> listOfRadios);

            var listOfRadios = repo.GetRunningStations();

            foreach (var ra in listOfRadios)
            {
                CancellationTokenSource cts = new CancellationTokenSource();
                radios.TryAdd(ra.DrId, new RadioStationHandler(Task.Run(() => new RadioStationHandler.RadioStation(ra.DrId, ra.StreamingUrl, cts.Token), cts.Token), cts));
            }

            while (true)
            {
                //new SQLCommunication().GetRunningStations(out List<string[]> runningRadios);

                var runningRadios = repo.GetRunningStations();

                foreach (var ra in runningRadios)
                {
                    if (!radios.ContainsKey(ra.DrId))
                    {
                        CancellationTokenSource cts = new CancellationTokenSource();
                        radios.TryAdd(ra.DrId, new RadioStationHandler(Task.Run(() => new RadioStationHandler.RadioStation(ra.DrId, ra.StreamingUrl, cts.Token), cts.Token), cts));
                    }
                }

                foreach (var ra in radios)
                {
                    if (!runningRadios.Any(m => m.DrId == ra.Key))
                    {
                        var cts = ra.Value.GetCancellationTokenSource();
                        cts.Cancel();
                        cts.Dispose();
                        radios.TryRemove(ra.Key, out RadioStationHandler rsh);
                    }
                }
                Thread.Sleep(200);
            }
        }
    }
}
