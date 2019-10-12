using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace az203.durablefunctions.VideoProcessor
{
    public static class ProcessVideoStarter
    {
        [FunctionName("ProcessVideo_Starter")]
        public static async Task<HttpResponseMessage> HttpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")]HttpRequestMessage req,
            [OrchestrationClient]DurableOrchestrationClient starter,
            ILogger log)
        {
            var qs = System.Web.HttpUtility.ParseQueryString(req.RequestUri.Query);
            string video = qs.Get("video");

            dynamic data = await req.Content.ReadAsStringAsync();

            video = video ?? data?.video;

            if (video == null)
            {
                return req.CreateResponse(HttpStatusCode.BadRequest,
                    "please pass a video location in the query string or data");
            }

            log.LogInformation($"About to start orchestrator for {video}");

            string instanceId = await starter.StartNewAsync("O_ProcessVideo", video);

            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");

            return starter.CreateCheckStatusResponse(req, instanceId);
        }
    }
}