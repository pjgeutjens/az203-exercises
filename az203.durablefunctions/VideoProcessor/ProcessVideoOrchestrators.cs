using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace az203.durablefunctions.VideoProcessor
{
    public static class ProcessVideoOrchestrators
    {
        [FunctionName("O_ProcessVideo")]
        public static async Task<object> ProcessVideo(
            [OrchestrationTrigger] DurableOrchestrationContext ctx, ILogger log)
        {
            var videoLocation = ctx.GetInput<string>();

            string transcodedLocation = null;
            string thumbnailLocation = null;
            string withIntroLocation = null;

            try
            {
                var bitrates = new[] {1000, 2000, 3000, 4000};
                var transcodeTasks = new List<Task<VideoFileInfo>>();

                foreach (int bitrate in bitrates)
                {
                    var info = new VideoFileInfo() { Location = videoLocation, BitRate = bitrate};
                    var task = ctx.CallActivityAsync<VideoFileInfo>("A_TranscodeVideo", info);
                    transcodeTasks.Add(task);
                }

                var transcodeResults = await Task.WhenAll(transcodeTasks);

                transcodedLocation = transcodeResults
                    .OrderByDescending(r => r.BitRate)
                    .Select(r => r.Location)
                    .First();

                if (!ctx.IsReplaying)
                {
                    log.LogInformation("starting thumbnail extraction");
                }

                thumbnailLocation = await
                    ctx.CallActivityAsync<string>("A_ExtractThumbnail", transcodedLocation);


                if (!ctx.IsReplaying)
                {
                    log.LogInformation("prepending intro video");
                }

                withIntroLocation = await
                    ctx.CallActivityAsync<string>("A_PrependIntro", transcodedLocation);

            }
            catch (Exception e)
            {
                if (!ctx.IsReplaying)
                {
                    log.LogInformation($"Caught an error from an activity: {e.Message}");
                }

                await
                    ctx.CallActivityAsync<string>("A_Cleanup",
                        new[] {transcodedLocation, thumbnailLocation, withIntroLocation});

                return new
                {
                    Error = "Video processing failed",
                    Message = e.Message
                };

            }   
                

            
            return new
            {
                Transcoded = transcodedLocation,
                Thumbnail = thumbnailLocation,
                WithIntro = withIntroLocation
            };
        }
    }
}
