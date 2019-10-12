using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Azure.Storage;
using Microsoft.Extensions.Configuration;

namespace az203.blobs
{
    public class Shared
    {
        public static CloudStorageAccount StorageAccount { get; private set; }

        public Shared()
        {
            var config = new ConfigurationBuilder().AddJsonFile("appsetting.json").Build();
            CloudStorageAccount storageAccount =
                CloudStorageAccount.Parse(config["StorageConnectionString"]);
            StorageAccount = storageAccount;
        }
    }
}
