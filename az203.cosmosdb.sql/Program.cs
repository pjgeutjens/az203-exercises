using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using System;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using ContainerProperties = Microsoft.Azure.Cosmos.ContainerProperties;

namespace az203.cosmosdb.sql
{
    class Program
    {
        static void Main(string[] args)
        {
            RunDatabases().Wait();
            RunContainers().Wait();
            //RunDocuments().Wait();
            QueryDocuments().Wait();
            LinqQueryDocuments().Wait();
        }

        private static async Task LinqQueryDocuments()
        {
            var container = Shared.Client.GetContainer("Volcanoes", "Volcanoes");
            var q = from c in container.GetItemLinqQueryable<Volcano>(allowSynchronousQueryExecution: true)
                where c.Country == "Guatemala"
                select new
                {
                    c.Id,
                    c.Name
                };
            var documents = q.ToList();
            Console.WriteLine($"Found {documents.Count} volcanoes in Guatemala");
            foreach (var document in documents)
            {
                Console.WriteLine(document.Name);
            }
        }

        private static async Task QueryDocuments()
        {
            string sql = "SELECT * FROM v WHERE v.Country = 'Samoa'";
            var iterator = Shared.Client.GetContainer("Volcanoes", "Volcanoes").GetItemQueryIterator<dynamic>(sql);
            var documents = await iterator.ReadNextAsync();

            int count = 0;
            foreach (var document in documents)
            {
                Console.WriteLine($"{count++} Id: {document.id} Name {document["Volcano Name"]}");
            }
            Console.WriteLine($"retrieved {count} documents as dynamic");
        }

        private static async Task RunDocuments()
        {
            // document from object
            Console.WriteLine();
            Console.WriteLine(">>> Document from object");
            dynamic document = new
            {
                id = Guid.NewGuid(),
                Name = "MadeUp",
                Country = "Belgium"
            };
            var result = await Shared.Client.GetContainer("Volcanoes", "Volcanoes")
                .CreateItemAsync(document, new PartitionKey("Belgium"));

            Console.WriteLine($"created: {document.id} from object");

            //var documentJson = $@"
            //{{
            //    ""id"" : ""{Guid.NewGuid()}"",
            //    ""Name"": ""JsonMomoa"",
            //    ""Country"": ""Samoa"",
            //    ""Eruptions"": [[""Last week"", ""Two years ago""]]
            //}}
            //";

            //var JsonObject = JsonConvert.DeserializeObject<JObject>(documentJson);
            //result = await Shared.Client.GetContainer("Volcanoes", "Volcanoes")
            //    .CreateItemAsync(document, new PartitionKey("Samoa"));

            //Console.WriteLine($"created: {JsonObject["id"].Value<string>()} from json string");

            var volcano = new Volcano()
            {
                Id = Guid.NewGuid(),
                Name = "ProperClassVolcano",
                Country = "Netherlands"
            };

            result = Shared.Client.GetContainer("Volcanoes", "Volcanoes").CreateItemAsync(volcano, new PartitionKey("Netherlands"),
                new ItemRequestOptions()
                {
                    ConsistencyLevel = ConsistencyLevel.Strong
                });
            Console.WriteLine($"Created document {volcano.Id.ToString()}");

        }

        private static async Task RunContainers()
        {
            //Debugger.Break();
            await ListContainers("Volcanoes");
            await CreateContainer("Volcanoes", "Hills", 400, "/Country");
            await ListContainers("Volcanoes");
            await DeleteContainer("Volcanoes", "Hills");
            await ListContainers("Volcanoes");

        }

        private static async Task DeleteContainer(string databaseId, string containerId)
        {
            Console.WriteLine();
            Console.WriteLine($">>> Deleting Container {databaseId}/{containerId} <<<");
            var result = await Shared.Client.GetContainer(databaseId, containerId).DeleteContainerAsync();

            Console.WriteLine($"Deleted {result.Container.Id}");
        }

        private static async Task CreateContainer(string databaseId, string containerId, int throughput = 400, string partitionKey = "/pk")
        {
            Console.WriteLine();
            Console.WriteLine($">>> Create Container {databaseId}/{containerId}: {throughput} DTU");
            var result = await Shared.Client.GetDatabase(databaseId)
                .CreateContainerAsync(containerId, partitionKey, throughput);
            Console.WriteLine($">>> Container {result.Container.Id} created <<<");
        }

        private static async Task ListContainers(string databaseId)
        {
            Console.WriteLine();
            Console.WriteLine($">>> List Containers in Database {databaseId} <<<");
            var iterator = Shared.Client.GetDatabase(databaseId).GetContainerQueryIterator<ContainerProperties>();
            var containers = await iterator.ReadNextAsync();
            foreach (var c in containers)
            {
                var throughput = await Shared.Client.GetDatabase(databaseId).GetContainer(c.Id).ReadThroughputAsync();
                Console.WriteLine($"Container Id: {c.Id}; PartitionKey: {c.PartitionKeyPath}: Throughput: {throughput}");
            }
        }

        private static async Task RunDatabases()
        {
            await ViewDatabases();
            await CreateDatabase("Lakes", 400);
            await CreateDatabase("Rivers");
            await ViewDatabases();
            await DeleteDatabase("Lakes");
            await DeleteDatabase("Rivers");
            await ViewDatabases();
        }

        // Containers 

        private static async Task DeleteDatabase(string id)
        {
            Console.WriteLine();
            Console.WriteLine($">>> Deleting Database {id} <<<");

            var result = await Shared.Client.GetDatabase(id).DeleteAsync();
            Console.WriteLine($">>> Deleted {id} <<<");
        }

        private static async Task CreateDatabase(string id, int? throughput = null)
        {
            Console.WriteLine();
            Console.WriteLine($">>> Create Database {id} <<<");

            var result = await Shared.Client.CreateDatabaseAsync(id, throughput);

            Console.WriteLine($"Created Database {result.Database.Id}");
        }

        private static async Task ViewDatabases()
        {
            Console.WriteLine();
            Console.WriteLine(">>> View Databases <<<");

            var iterator = Shared.Client.GetDatabaseQueryIterator<DatabaseProperties>();
            var databases = await iterator.ReadNextAsync();

            foreach (var db in databases)
            {
                var throughput = await Shared.Client.GetDatabase(db.Id).ReadThroughputAsync();
                Console.WriteLine($"Database Id: {db.Id}; Modified: {db.LastModified}; throughput: {throughput}");
            }
        }
    }

    internal class Volcano  
    {
        [JsonProperty(PropertyName = "id")]
        public Guid Id { get; set; }
        [JsonProperty(PropertyName = "Volcano Name")] 
        public string Name { get; set; }
        [JsonProperty(PropertyName = "Country")]
        public string Country { get; set; }

    }

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
