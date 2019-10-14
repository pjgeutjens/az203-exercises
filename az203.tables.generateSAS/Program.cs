using System;
using System.IO;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Extensions.Configuration;

namespace az203.tables.generateSAS
{
    class Program
    {
        static void Main(string[] args)
        {
            var config = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json").Build();
            var storageAccount = Microsoft.Azure.Cosmos.Table.CloudStorageAccount.Parse(
               config.GetConnectionString("StorageConnection"));
            var tableClient = storageAccount.CreateCloudTableClient();
            var table = tableClient.GetTableReference("todo");

            var sas = table.GetSharedAccessSignature(new SharedAccessTablePolicy
            {
                Permissions = SharedAccessTablePermissions.Query,
                SharedAccessStartTime = DateTime.Now.AddMinutes(-1),
                SharedAccessExpiryTime = DateTime.Now.AddDays(1)
            });
            
            Microsoft.Azure.Storage.CloudStorageAccount st;
            Microsoft.Azure.Storage.CloudStorageAccount.TryParse(config.GetConnectionString("StorageConnection"), out st);
            CloudBlobClient blobClient = st.CreateCloudBlobClient();
            var container = blobClient.GetContainerReference("images");

            var containerSas = container.GetSharedAccessSignature(new SharedAccessBlobPolicy{
                Permissions = SharedAccessBlobPermissions.List,
                SharedAccessStartTime = DateTime.Now.AddMinutes(-1),
                SharedAccessExpiryTime = DateTime.Now.AddHours(1)
            });

            var blob = container.GetBlockBlobReference("headshot.png");

            var blobSas = blob.GetSharedAccessSignature(new SharedAccessBlobPolicy{
                Permissions = SharedAccessBlobPermissions.Read,
                SharedAccessStartTime = DateTime.Now.AddMinutes(-1),
                SharedAccessExpiryTime = DateTime.Now.AddHours(1)
            });

            // or using access policy
            // var sas2 = table.GetSharedAccessSignature(null, "MyTableAccessPolicy");

            Console.WriteLine(sas);
            Console.WriteLine(containerSas);
            Console.WriteLine(blobSas);
        }
    }
}
