//using System;
//using Xunit;
//using MatchAudio;
//using Moq;
//using System.IO;

//namespace MatchAudio.Tests
//{
//    public class MatcherPathHandlerTest
//    {

//        [Theory]
//        [InlineData("mp4")]
//        [InlineData("txt")]
//        public void Does_Not_Call_Matcher_When_Given_Wrong_file_Type(string pathEnd)
//        {
//            var mock = new Mock<IAudioMatcher>();
//            mock.Setup(x => x.Match("",0));
//            var testFileName = $"Test.{pathEnd}";

//            using (var tw = new StreamWriter(testFileName, true))
//            {
//                tw.WriteLine("The next line!");
//            }

//            var handler = new MatcherPathHandler(mock.Object);
//            handler.Handle(testFileName);
//            File.Delete(testFileName);

//            mock.Verify(x => x.Match(testFileName, ), Times.Never());

//        }

//        [Fact]
//        public void Calls_AudioMatcher_When_Given_csv_file()
//        {
//            var mock = new Mock<IAudioMatcher>();
//            mock.Setup(x => x.Match("/path/to/music"));
//            var testFileName = "Text.csv";

//            using (var tw = new StreamWriter(testFileName, true))
//            {
//                tw.WriteLine("first line, something");
//                tw.WriteLine("123-123, /path/to/music/");
//            }

//            var handler = new MatcherPathHandler(mock.Object);
//            handler.Handle(testFileName);
//            File.Delete(testFileName);

//            mock.Verify(x => x.Match("/path/to/music", false));
//        }


//        [Theory]
//        [InlineData("mp3")]
//        [InlineData("wav")]
//        public void Calls_AudioMatcher_When_Given_single_file(string testPath)
//        {
//            var mock = new Mock<IAudioMatcher>();
//            mock.Setup(x => x.Match("", false));
//            var testFileName = $"Test.{testPath}";

//            using (var tw = new StreamWriter(testFileName, true))
//            {
//                tw.WriteLine("first line, something");
//            }

//            var handler = new MatcherPathHandler(mock.Object);
//            handler.Handle(testFileName);
//            File.Delete(testFileName);

//            mock.Verify(x => x.Match(testFileName, false));
//        }
//    }
//}
