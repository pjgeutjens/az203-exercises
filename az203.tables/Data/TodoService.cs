using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos.Table;
using System.IO;

namespace az203.tables.Data
{
    public class TodoService
    {
        private CloudTable _todoTable;

        public TodoService()
        {
            var config = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json").Build();
            var storageAccount = CloudStorageAccount.Parse(
                config.GetConnectionString("TableStorage"));
            var tableClient = storageAccount.CreateCloudTableClient();
            
            _todoTable = tableClient.GetTableReference("todo");
        }

        public async Task<List<Todo>> GetTodosAsync()
        {
            var results = new List<Todo>();
            var query = new TableQuery<Todo>();
            TableContinuationToken token = null;
            do
            {
                TableQuerySegment<Todo> queryResults = await _todoTable.ExecuteQuerySegmentedAsync(query, token);
                token = queryResults.ContinuationToken;
                results.AddRange(queryResults.Results);
            } while (token != null);

            return results;
        }
    }
}