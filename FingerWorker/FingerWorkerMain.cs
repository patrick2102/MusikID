using System;
using System.Threading;
using DatabaseCommunication;
using System.IO;
using MatchAudio;
using CreateAudioFingerprint;
using System.Globalization;

namespace FingerWorkerProcess
{
    public class FingerWorkerMain
    {
        

        static void Main(string[] args)
        {

            new FingerWorker().Start();
        }
    

    }
}
