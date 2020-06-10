using Moq;
using System;
using Xunit;
using AudioFingerprinting.Web.Controllers;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.Text;

namespace AudioFingerprinting.Web.Tests
{
    /*
    public class AudioControllerTests
    {
        AudioController _controller;

        public AudioControllerTests() {
            _controller = new AudioController();
        }

        [Fact]
        public void Post_Path_String_returns_Okay_respond()
        {

            var result = _controller.PostAudio(@"C\\superduperPath\");

            Console.WriteLine(result);

            Assert.True(true);

            //var repo = new Mock<IRepository>();
            //repo.Setup(s => s.AnalyzeAudioAsync("test")).ReturnsAsync(output);

        }

        [Theory]
        [InlineData(@"\\startæøå\file.mp3")]
        [InlineData(@"\\startæäå\file.flac")]
        [InlineData(@"\\startæøëå\file.wav")]
        public void PostAudio_given_correct_path_returns_status_code_200(string path) {
            var result = _controller.PostAudio(path);

            var expected = new OkResult();

            Assert.True(result.GetType() == expected.GetType());
        }

        [Theory]
        [InlineData("08/18/2018 07:00:00", "08/18/2018 09:00:00", null)]
        [InlineData("08/18/2018 07:00:00", null, "P3" )]
        [InlineData(null, "08/18/2018 09:00:00", "P3")]
        public void Post_Rolling_returns_badRequest_given_bad_RollingWindow(string start, string end, string radioID)
        {
            var result = _controller.RollingWindow(new RollingWindow(start, end, radioID));

            var expected = new BadRequestObjectResult("Some values where null");

            Assert.True(result.GetType() == expected.GetType());
        }

        [Theory]
        [InlineData("12,12,2018 07.00.00", "12,12,2018 08.08.08", "P3")]
        [InlineData("12:12:2018 07.00.00", "12:12:2018 08.08.08", "P3")]
        public void Post_Rolling_returns_badRequest_given_bad_TimeStrings(string start, string end, string radioID)
        {
            var result = _controller.RollingWindow(new RollingWindow(start, end, radioID));

            var expected = new BadRequestObjectResult("Some values where null");

            Assert.True(result.GetType() == expected.GetType());
        }

        [Theory]
        [InlineData("24/12/2018 07:00:00", "24/12/2018 08:00:00", "P3")]
        [InlineData("24/12/2018 07:00:00", "25/12/2018 08:00:00", "P3")]
        //[InlineData("12:12:2018 07.00.00", "12:12:2018 08.08.08", "P3")]
        public void Post_Rolling_returns_Ok_given_correct_time_and_radio(string start, string end, string radioID)
        {
            var result = _controller.RollingWindow(new RollingWindow(start, end, radioID));

            var expected = new OkResult();

            Assert.True(result.GetType() == expected.GetType());
        }

        [Theory]
        [InlineData(@"\\startæøå\file")]
        [InlineData(@"\\startæøëå\file.")]
        public void PostAudio_given_invalid_path_returns_status_code_400(string path)
        {
            var result = _controller.PostAudio(path);

            var expected = new BadRequestObjectResult("Invalid path to audio.");

            Assert.True(result.GetType() == expected.GetType());
        }

        [Theory]
        [InlineData(@"\\startæøå\file.jpg")]
        [InlineData(@"startæäå\fileæ.m3u")]
        [InlineData(@"\\startæøëå\file.mp4")]
        public void PostAudio_given_invalid_extension_returns_status_code_400(string path)
        {
            var result = _controller.PostAudio(path);

            var expected = new BadRequestObjectResult("Invalid path to audio.");

            Assert.True(result.GetType() == expected.GetType());
        }

        /*
        [Theory]
        [InlineData(@"\\startæøå\file.mp3")]
        [InlineData(@"\\startæäå\file.flac")]
        [InlineData(@"\\startæøëå\file.wav")]
        public void PostFingerprint_given_correct_path_returns_status_code_200(string path)
        {
            var result = _controller.PostFingerPrint(path);

            var expected = new OkResult();

            Assert.True(result.GetType() == expected.GetType());
        }

        
    
        [Theory]
        [InlineData(@"\\startæøå\file")]
        [InlineData(@"\\startæøëå\file.")]
        public void PostFingerprint_given_invalid_path_returns_status_code_400(string path)
        {
            var result = _controller.PostAudio(path);

            var expected = new BadRequestObjectResult("Invalid path to audio.");

            Assert.True(result.GetType() == expected.GetType());
        }

        [Theory]
        [InlineData("P69", null)]
        [InlineData(null, @"\\startæøëå\file.")]
        public void AddRadioChannel_returns_bad_request_when_either_param_is_null(string id, string url)
        {
            var result = _controller.AddRadioChannel(new Radio(id, url));

            var expected = new BadRequestObjectResult("Invalid JSON information.");

            Assert.True(result.GetType() == expected.GetType());
        }

        [Theory]
        [InlineData("P69", @"\\TEST\LOL\HAHA.txt")]
        [InlineData("P13", @"\\startæøëå\file.")]
        public void AddRadioChannel_returns_ok_when_both_param_not_null(string id, string url)
        {
            var result = _controller.AddRadioChannel(new Radio(id, url));

            var expected = new OkResult();

            Assert.True(result.GetType() == expected.GetType());
        }
    
    }*/
}
