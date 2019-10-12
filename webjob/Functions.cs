using System;
using System.IO;

namespace webjob
{
    public class Functions
    {
        // This function will get triggered/executed when a new message is written 
        // on an Azure Queue called queue.
        public static void NotifyOwner(TextWriter log)
        {
            log.WriteLine($"Notification at {DateTime.Now.ToString()}");
        }
    }
}
