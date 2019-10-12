using System;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Extensions.Configuration;

namespace az203.tables.generateSAS
{
    class Program
    {
        static void Main(string[] args)
        {
            var config = ConfiguratonBuilder
            var storageAccount = CloudStorageAccount.Parse(
                "DefaultEndpointsProtocol=https;AccountName=az203stor2019;AccountKey=3xl8YlFS2Lk6VRNXRJiYvuiIiR8QHb/rD8T1Sp3U1ZXHZlUqwYjTBakJlMZWutq+xkuJrduRYMxr4N272X7a+Q==;EndpointSuffix=core.windows.net");
            var tableClient = storageAccount.CreateCloudTableClient();
            var table = tableClient.GetTableReference("todo");

            var sas = table.GetSharedAccessSignature(new SharedAccessTablePolicy
            {
                Permissions = SharedAccessTablePermissions.Query,
                SharedAccessStartTime = DateTime.Now.AddMinutes(-1),
                SharedAccessExpiryTime = DateTime.Now.AddDays(1)
            });

            // or using access policy
            // var sas2 = table.GetSharedAccessSignature(null, "MyTableAccessPolicy");

            Console.WriteLine(sas);
        }
    }
}
