using MakeSubFinger;
using System;
using System.Diagnostics;

namespace CreateAudioFingerprint
{
    public class CreateFingerprintMain
    {

        public static void Main(string[] args)
        {
            bool isPriority = false;
            string audio_path = "";
            bool force = false;
            if (args.Length > 0)
            {
                audio_path = args[0];
                if (args.Length > 1)
                {
                    if (args[1].ToLower() == "true")
                    {
                        force = true;
                    }
                }
                if (args.Length > 2)
                {
                    if (args[2].ToLower() == "true")
                    {
                        isPriority = true;
                    }
                }
            }
            else
            {
                Console.WriteLine("No path was given as argument");
                return;
            }

            Stopwatch sw = new Stopwatch();
            sw.Start();

            var handler = new FingerprintPathHandler(new FingerprintCreator(force));
            handler.Handle(audio_path, -1);

            sw.Stop();
            Console.WriteLine($"Time to fingerprint: {sw.Elapsed.TotalSeconds}");
            Console.WriteLine("Finished fingerprinting");

            if (isPriority)
            {
                Console.WriteLine("Commencing LuceneUpdate because of priority");
            }
        }
    }
}
