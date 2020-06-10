using System;
using Xunit;

namespace MatchAudio.Tests
{
    public class ResultTest
    {
        [Fact]
        public void Given_Result_Test_Correct_ToString_Method_With_Full_Data()
        {
            //Arrange
            string reference = "ABCD1234";
            DateTime startTime = DateTime.MinValue;
            var endTime =  DateTime.MinValue.AddHours(1);
            var similarity = 99;
            var artist = "HonkaBonka";
            var title = "HukkaBukka";
            Result result = new Result(reference,startTime,endTime,similarity,artist,title);
            //Action
            var resultOfResult = result.ToString();
            //Assert 
            Assert.Equal(DateTime.Now.ToString("HH:mm:ss") + " ; "
                + DateTime.Now.AddHours(1).ToString("HH:mm:ss") + " ; ABCD1234 ; HonkaBonka ; HukkaBukka",resultOfResult);
        }

        [Fact]
        public void Given_Result_Test_Correct_ToString_Method_With_Null()
        {
            //Arrange
            string reference = "";
            var startTime = DateTime.Now;
            var endTime = DateTime.Now.AddHours(1);
            var similarity = 99;
            var artist = "HonkaBonka";
            var title = "HukkaBukka";
            Result result = new Result(reference, startTime, endTime, similarity, artist, title);
            //Action
            var resultOfResult = result.ToString();
            //Assert 
            Assert.Equal(DateTime.Now.ToString("HH:mm:ss") + " ; "
                + DateTime.Now.AddHours(1).ToString("HH:mm:ss") + " ; NaN ; HonkaBonka ; HukkaBukka", resultOfResult);
        }
        [Fact]
        public void Test_UpdateValues_That_It_Chooses_The_Maximum_EndTime()
        {
            //Arrange
            var endtimeCorrect = DateTime.Now;
            var endTime = DateTime.MinValue.AddHours(1);
            string reference = "";
            var startTime = DateTime.MinValue;
            var similarity = 99;
            var artist = "HonkaBonka";
            var title = "HukkaBukka";
            Result result = new Result(reference, startTime, endTime, similarity, artist, title);
            Result result1 = new Result(reference, startTime, endTime.AddHours(1), similarity, artist, title);
            result.UpdateValues(result1);

            Assert.Equal(result1._endTime,result._endTime);
        }
        [Fact]
        public void Test_GetDuration_Given_Start_And_EndTime()
        {
            var endtimeCorrect = DateTime.Now;
            string reference = "";
            var startTime = DateTime.MinValue;
            var endTime = DateTime.MinValue.AddHours(1);
            var similarity = 99;
            var artist = "HonkaBonka";
            var title = "HukkaBukka";
            Result result = new Result(reference, startTime, endTime, similarity, artist, title);

            var actualResult = result.GetDuration();

            Assert.Equal(3600, actualResult);
        }
    }
}
