using MakeSubFinger;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using Xunit;

namespace CreateAudioFingerprint.tests
{
    public class PathHandlerTest
    {
        [Fact]
        public void Does_Not_Call_CreateFingerPrint_When_Given_Wrong_file_Type() {
            var mock = new Mock<IFingerprintCreator>();
            mock.Setup(x => x.Create(""));
            var testFileName = "Text.txt";

            using (var tw = new StreamWriter(testFileName, true))
            {
                tw.WriteLine("The next line!");
            }

            var handler = new FingerprintPathHandler(mock.Object);
            handler.Handle(testFileName, -1);
            File.Delete(testFileName);

            mock.Verify(x => x.Create(testFileName), Times.Never());

        }
        [Fact]
        public void Calls_CreateFingerPrint_When_Given_csv_file()
        {
            var mock = new Mock<IFingerprintCreator>();
            mock.Setup(x => x.Create("/path/to/music"));
            var testFileName = "Text.csv";

            using (var tw = new StreamWriter(testFileName, true))
            {
                tw.WriteLine("first line, something");
                tw.WriteLine("123-123, /path/to/music/");
            }

            var handler = new FingerprintPathHandler(mock.Object);
            handler.Handle(testFileName, -1);
            File.Delete(testFileName);

            mock.Verify(x => x.Create("/path/to/music"));
        }
        [Fact]
        public void Calls_CreateFingerPrint_When_Given_single_mp3_file()
        {
            var mock = new Mock<IFingerprintCreator>();
            mock.Setup(x => x.Create(""));
            var testFileName = "Text.mp3";

            using (var tw = new StreamWriter(testFileName, true))
            { 
            }

            var handler = new FingerprintPathHandler(mock.Object);
            handler.Handle(testFileName, -1);
            File.Delete(testFileName);

            mock.Verify(x => x.Create(testFileName), Times.Once);

        }
    }
}
