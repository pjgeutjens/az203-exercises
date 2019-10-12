using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace az203.durablefunctions.VideoProcessor
{
    public static class ProcessVideoActivities
    {
        [FunctionName("A_TranscodeVideo")]
        public static async Task<VideoFileInfo> TranscodeVideo(
            [ActivityTrigger] VideoFileInfo inputVideo, ILogger log)
        {
            log.LogInformation($"Transcoding {inputVideo.Location} to {inputVideo.BitRate}");
            // Simulate activity
            await Task.Delay(5000);

            return new VideoFileInfo()
            {
                Location = $"{Path.GetFileNameWithoutExtension(inputVideo.Location)}-{inputVideo.BitRate}kbps.mp4",
                BitRate = inputVideo.BitRate
            };
        }

        [FunctionName("A_ExtractThumbnail")]
        public static async Task<string> ExtractThumbnail(
            [ActivityTrigger] string inputVideo, ILogger log)
        {
            log.LogInformation($"Extracting thumbnail from {inputVideo}");

            if (inputVideo.Contains("error"))
            {
                throw new InvalidOperationException("error extracting thumbnail");
            }
            // Simulate activity
            await Task.Delay(2000);

            return "thumbnail.png";
        }

        [FunctionName("A_PrependIntro")]
        public static async Task<string> PrependIntro(
            [ActivityTrigger] string inputVideo, ILogger log)
        {
            log.LogInformation($"Prepending intro to {inputVideo}");
            var introLocation = "ShouldBeAConfigurationItem";
            // Simulate activity
            await Task.Delay(5000);

            return "withIntro.mp4";
        }

        [FunctionName("A_Cleanup")]
        public static async Task<string> Cleanup(
            [ActivityTrigger] string[] filesToCleanup, ILogger log)
        {
            foreach (var file in filesToCleanup.Where(f => f != null))
            {
                log.LogInformation($"Deleting {file}");
                await Task.Delay(500);
            }

            return "Cleanup successful";
        }
    }
}
