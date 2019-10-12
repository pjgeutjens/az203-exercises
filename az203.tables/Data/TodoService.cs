using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos.Table;

namespace az203.tables.Data
{
    public class TodoService
    {
        private CloudTable _todoTable;

        public TodoService()
        {
            var storageAccount = CloudStorageAccount.Parse(
                "DefaultEndpointsProtocol=https;AccountName=az203stor2019;AccountKey=3xl8YlFS2Lk6VRNXRJiYvuiIiR8QHb/rD8T1Sp3U1ZXHZlUqwYjTBakJlMZWutq+xkuJrduRYMxr4N272X7a+Q==;EndpointSuffix=core.windows.net");
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