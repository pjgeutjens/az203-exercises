using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace az203.functions
{
    public static class QueueTrigger
    {
        [FunctionName("QueueTrigger")]
        public static void Run([QueueTrigger("myqueue-items", Connection = "AzureWebJobsStorage")]string myQueueItem,
            [Queue("myqueue-items-destination", Connection="AzureWebJobsStorage")] out string myQueueItemCopy,
            ILogger log)
        {
            log.LogInformation($"C# Queue trigger function processed: {myQueueItem}");
            myQueueItemCopy = myQueueItem;
        }
    }
}
