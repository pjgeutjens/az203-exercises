using System;
using System.Threading;

namespace webjob
{
    // To learn more about Microsoft Azure WebJobs SDK, please see https://go.microsoft.com/fwlink/?LinkID=320976
    class Program
    {
        // Please set the following connection strings in app.config for this WebJob to run:
        // AzureWebJobsDashboard and AzureWebJobsStorage
        static void Main()
        {
            while (true)
            {
                Functions.NotifyOwner(Console.Out);
                Thread.Sleep(30000);
            }
        }
    }
}
