using Framework;
using MatchAudio;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace RadioWorker
{
    public class RadioStationHandler
    {
        private Task _rsi;
        public CancellationTokenSource _cts;

        public RadioStationHandler(Task rsi, CancellationTokenSource cts) {
            _rsi = rsi;
            _cts = cts;
        }

        public CancellationTokenSource GetCancellationTokenSource() {
            return _cts;
        }

        public class RadioStation
        {
            readonly private string sharedPathForRadioChannels = @"\\musa01\download\ITU\MUR\RadioChannels\{0}\";
            private string _channelId;
            private string _url;
            private string _path;
            private CancellationToken _ct;
            private DrRepository repo;

            public RadioStation(string channelId, string url, CancellationToken ct)
            {
                _channelId = channelId;
                _url = url;
                _path = string.Format(sharedPathForRadioChannels, channelId);
                _ct = ct;
                var segmentDuration = 6;
                var streamCutDuration = 29.92 + segmentDuration;
                Start(streamCutDuration, segmentDuration);
            }

            public async void Start(double streamCutDuration, int segmentDuration)
            {
                    while (!_ct.IsCancellationRequested)
                    {
                        if (_ct.IsCancellationRequested)
                            throw new TaskCanceledException();

                        Ffmpeg ffmpeg = new Ffmpeg();

                        //HACK: quick fix for creating folders if _path dont exist, then create a new folder for that channel and with a RollingWindow folder
                        if (!System.IO.Directory.Exists(_path)){
                            System.IO.Directory.CreateDirectory($"{_channelId}\\RollingWindow");
                        }

                        //TODO move reop to sampler
                        await Task.Run(() => ffmpeg.StartFFmpegSampler(_url, streamCutDuration, _path, _channelId));

                        repo = new DrRepository(new drfingerprintsContext());

                        repo.InsertRadioTask(_channelId, ffmpeg._chunkName);

                        Thread.Sleep((int)(streamCutDuration - segmentDuration) * 1000);
                    }
            }
        }
    }
}
