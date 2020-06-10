using AudioFingerprint.Audio;
using System;
using Xunit;

namespace CreateAudioFingerprint.tests
{
    public class FingerprintCreatorTests
    {
        FingerprintCreator fingerprintCreator;
        public FingerprintCreatorTests() {
            fingerprintCreator = new FingerprintCreator();
        }

        [Fact]
        public void SupportedFileFormat_given_valid_format_returns_true()
        {
            var formats = new string[3];
            formats[0] = "mp3";
            formats[1] = "wav";
            formats[2] = "flac";

            var result = true;

            foreach (var s in formats) {
                if (!fingerprintCreator.FormatSupported(s))
                    result = false;
            }
            Assert.True(result);
        }

        [Fact]
        public void SupportedFileFormat_given_invalid_format_returns_false()
        {
            var formats = new string[3];
            formats[0] = "jpg";
            formats[1] = "png";
            formats[2] = "mp4";

            var result = true;

            foreach (var s in formats)
            {
                if (!fingerprintCreator.FormatSupported(s))
                    result = false;
            }
            Assert.False(result);
        }

        [Fact]
        public void ExtractInfoUsingRegex_given_path_returns_info() {
            string path = @"C:\\files\\123456-1-1_artist_song.mp3";

            var id = "123456";
            var side = "1";
            var num = "1";

            var matches = fingerprintCreator.ExtractInfoUsingRegex(path);

            Assert.Equal(id, matches[0]);
            Assert.Equal(side, matches[1]);
            Assert.Equal(num, matches[2]);
        }
        
        [Theory]
        [InlineData("111222", "12", "21", "flac")]
        [InlineData("111222", "1", "1", "mp3")]
        [InlineData("111222", "12", "21", "wav")]
        public void Validation_given_correct_string_array_returns_true(string diskotekNr, string sideNr, string sequenceNr, string format)
        {
            var matches = new string[] {diskotekNr, sideNr, sequenceNr, format };

            var answer = fingerprintCreator.Validation(matches);

            Assert.True(answer);
        }

        [Fact]
        public void Validation_given_short_string_array_returns_false()
        {
            var matches = new string[3];
            matches[0] = "123456";
            matches[1] = "1";
            matches[2] = "1";

            Assert.False(fingerprintCreator.Validation(matches));
        }

        [Theory]
        [InlineData(null, "1", "1", "mp3")]
        [InlineData("1", null, "1", "mp3")]
        [InlineData("1", "1", null, "mp3")]
        [InlineData("111222", "1", "1", null)]
        [InlineData("a", "b", "c", "mp3")]
        [InlineData("111222", "1", "1", "jpg")]
        public void Validation_given_incorrect_string_arrays_returns_false(string diskotekNr, string sideNr, string sequenceNr, string format)
        {
            var matches = new string[] {diskotekNr, sideNr, sequenceNr, format};

            var answer = fingerprintCreator.Validation(matches);
            if (answer)
                Console.WriteLine("");

            Assert.False(answer);
        }

    }
}
