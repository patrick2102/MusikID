using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace MatchAudio.test
{
    public class AudioAnalysisDictionaryTest
    {
        private List<Result> list;
        public AudioAnalysisDictionary audioAnalysisDictionary;
        public AudioAnalysisDictionaryTest()
        {
            //Result 1
            string reference = "ABCD1234";
            DateTime startTime = DateTime.Now;
            var endTime = DateTime.Now.AddHours(1);
            var similarity = 99;
            var artist = "HonkaBonka";
            var title = "HukkaBukka";
            Result result1 = new Result(reference, startTime, endTime, similarity, artist, title);

            //Result 2
            string reference1 = "1234ABCD";
            DateTime startTime1 = DateTime.MinValue;
            DateTime endTime1 = DateTime.MinValue.AddHours(1);
            var similarity1 = 98;
            var artist1 = "HelloBaby";
            var title1 = "FlyvendeFarmor";
            Result result2 = new Result(reference1, startTime1, endTime1, similarity1, artist1, title1);

           list = new List<Result>();
            list.Add(result1);
            list.Add(result2);
            audioAnalysisDictionary = new AudioAnalysisDictionary();
        }
        [Fact]
        public void Given_List_Of_Results_Of_Size_2_On_Update_Return_A_Dictionary_Of_Size_2()
        {
            audioAnalysisDictionary.Update(list, true);

            Assert.Equal(2,audioAnalysisDictionary.dictionary.Count);
        }

        [Fact]
        public void Given_FilePath_To_No_File_Returns_Null()
        {
            var logpath = @"C://Hello.csv";

            Assert.False(File.Exists(logpath));
        }

        [Fact]
        public void Given_Two_Results_In_Dictionary_Prints_Two_Results()
        {
            var currentConsoleOut = Console.Out;

            string text = DateTime.Now.ToString("HH: mm: ss") + "; "
                 + DateTime.Now.AddHours(1).ToString("HH:mm:ss") + " ; ABCD1234 ; HonkaBonka ; HukkaBukka";

            using (var consoleOutput = new ConsoleOutput())
            {
                audioAnalysisDictionary.Print();
                
                Assert.Equal(text, consoleOutput.GetOuput());
            }

            //sAssert.Equal(currentConsoleOut, Console.Out);
        }
    }
}
