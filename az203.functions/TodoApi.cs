using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using Microsoft.WindowsAzure.Storage.Table;
using Microsoft.WindowsAzure.Storage;

namespace az203.functions
{
    public static class TodoApi
    {

        [FunctionName("CreateTodo")]
        public static async Task<IActionResult> CreateTodo(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "todo")]HttpRequest req,
            [Table("todos", Connection = "AzureWebJobsStorage")] IAsyncCollector<TodoTableEntity> todoTable,
            ILogger log)
        {
            log.LogInformation("Creating new todo item");
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var input = JsonConvert.DeserializeObject<TodoCreateModel>(requestBody);

            var todo = new Todo() { TaskDescription = input.TaskDescription};
            await todoTable.AddAsync(todo.ToTableEntry());
            return new OkObjectResult(todo);
        }

        [FunctionName("GetTodos")]
        public static async Task<IActionResult> GetTodos(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "todo")]HttpRequest req,
            [Table("todos", Connection = "AzureWebJobsStorage")] CloudTable todoTable, 
            ILogger log)
        {
            log.LogInformation("Getting all todos");
            var query = new TableQuery<TodoTableEntity>();
            var segment = await todoTable.ExecuteQuerySegmentedAsync(query, null);
            return new OkObjectResult(segment.Select(Mappings.ToTodo));
        }

        [FunctionName("GetTodoById")]
        public static async Task<IActionResult> GetTodoById(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "todo/{id}")]HttpRequest req,
            [Table("todos", "TODO", "{id}", Connection = "AzureWebJobsStorage")] TodoTableEntity todo,
            ILogger log, string id)
        {
            if (todo == null)
            {
                return new NotFoundResult();
            }

            return new OkObjectResult(todo.ToTodo());
        }

        [FunctionName("UpdateTodo")]
        public static async Task<IActionResult> UpdateTodo(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "todo/{id}")]HttpRequest req,
            [Table("todos", Connection = "AzureWebJobsStorage")] CloudTable todoTable,
            ILogger log, string id)
        {
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var updated = JsonConvert.DeserializeObject<TodoUpdateModel>(requestBody);
            var findOperation = TableOperation.Retrieve<TodoTableEntity>("TODO", id);
            var findResult = await todoTable.ExecuteAsync(findOperation);
            if (findResult.Result == null)
            {
                return new NotFoundResult();
            }

            var existingRow = (TodoTableEntity)findResult.Result;
            existingRow.IsCompleted = updated.IsCompleted;
            if (!String.IsNullOrEmpty(updated.TaskDescription))
            {
                existingRow.TaskDescription = updated.TaskDescription;
            }

            var replaceOperation = TableOperation.Replace(existingRow);
            await todoTable.ExecuteAsync(replaceOperation);

            return new OkObjectResult(existingRow.ToTodo());

        }

        [FunctionName("DeleteTodo")]
        public static async Task<IActionResult> DeleteTodo(
           [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "todo/{id}")]HttpRequest req,
            [Table("todos", Connection = "AzureWebJobsStorage")] CloudTable todoTable,
            ILogger log, string id)
        {
            var deleteOperation = TableOperation.Delete(new TableEntity() { 
            PartitionKey = "TODO", RowKey = id, ETag = "*"});

            try
            {
                var deleteResult = await todoTable.ExecuteAsync(deleteOperation);

            }
            catch (StorageException e) when (e.RequestInformation.HttpStatusCode == 404)
            {
                return new NotFoundResult();
            }

            return new OkResult();
        }

    }
}
