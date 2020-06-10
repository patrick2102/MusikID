
using Framework;
using System;

namespace MatchAudio
{
    public interface IAudioMatcher
    {
        void Match(DrRepository _repo, string _inputPath, long jobID, int fileId);
        void MatchRollingWindowAsync(DrRepository _repo, string sharedPathForRadioChannels, DateTime start, DateTime end, string channel_id, long jobID);
    }
}