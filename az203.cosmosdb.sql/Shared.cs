using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;

namespace az203.cosmosdb.sql
{
public static class Shared
    {
        public static CosmosClient Client { get; set; }

        static Shared()
        {
            var config = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
            var uri = config["CosmosDbUri"];
            var key = config["CosmosDbKey"];
            Client = new CosmosClient(uri, key);
        }
    }
}