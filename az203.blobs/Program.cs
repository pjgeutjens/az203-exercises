using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;

namespace az203.blobs
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Azure Blob Storage - Quickstart");

            ProcessAsync().GetAwaiter().GetResult();
        }

        private static async Task ProcessAsync()
        {
            var blobClient = Shared.StorageAccount.CreateCloudBlobClient();
            var container = blobClient.GetContainerReference("myContainer");
            await container.CreateIfNotExistsAsync(BlobContainerPublicAccessType.Container, null, null);

            BlobContainerPermissions permissions = new BlobContainerPermissions()
            {
                PublicAccess = BlobContainerPublicAccessType.Blob
            };

            await container.SetPermissionsAsync(permissions);

            // upload a blob

            string localPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var localFilename = "captainamerica.jpg";
            string sourceFile = Path.Combine(localPath, localFilename);

            CloudBlockBlob cloudBlockBlob = container.GetBlockBlobReference(localFilename);
            await cloudBlockBlob.UploadFromFileAsync(sourceFile);

            // list the blobs in a container
            BlobContinuationToken token = null;

            do
            {
                var results = await container.ListBlobsSegmentedAsync(token);
                token = results.ContinuationToken;
                foreach (IListBlobItem result in results.Results)
                {
                    Console.WriteLine(result.Uri);
                }
            } while (token == null);

            // download a blob
            string destinationFile = sourceFile.Replace("america", "copy");
            await cloudBlockBlob.DownloadToFileAsync(destinationFile, FileMode.Create);
        }
    }
}
