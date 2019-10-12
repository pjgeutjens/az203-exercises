using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Azure.Batch;
using Microsoft.Azure.Batch.Auth;
using Microsoft.Azure.Batch.Common;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;

namespace cutifypets
{
    internal class Program
    {
        private const string envVarBatchURI = "BATCH_URL";
        private const string envVarBatchName = "BATCH_NAME";
        private const string envVarKey = "BATCH_KEY";
        private const string envVarStorage = "STORAGE_NAME";
        private const string envVarStorageKey = "STORAGE_KEY";

        private const string PoolId = "WinFFmpegPool";
        private const int DedicatedNodeCount = 0;
        private const int LowPriorityNodeCount = 3;
        private const string PoolVMSize = "STANDARD_D2_v2";
        private const string appPackageId = "ffmpeg";
        private const string appPackageVersion = "3.4";

        private const string JobId = "WinFFmpegJob";

        private static string batchAccountName;
        private static string batchAccountUrl;
        private static string batchAccountKey;
        private static string storageAccountName;
        private static string storageAccountKey;

        private static async Task<bool> MonitorTasksAsync(BatchClient batchClient, string jobId, TimeSpan timeout)
        {
            var allTasksSuccessful = true;
            const string completeMessage = "All tasks reached state Completed.";
            const string incompleteMessage =
                "One or more tasks failed to reach the Completed state within the timeout period.";
            const string successMessage =
                "Success! All tasks completed successfully. Output files uploaded to output container.";
            const string failureMessage = "One or more tasks failed.";

            Console.WriteLine("Monitoring all tasks for 'Completed' state, timeout in {0}...", timeout.ToString());

            // We use a TaskStateMonitor to monitor the state of our tasks. In this case, we will wait for all tasks to
            // reach the Completed state.
            IEnumerable<CloudTask> addedTasks = batchClient.JobOperations.ListTasks(JobId);

            var taskStateMonitor = batchClient.Utilities.CreateTaskStateMonitor();
            try
            {
                await taskStateMonitor.WhenAll(addedTasks, TaskState.Completed, timeout);
            }
            catch (TimeoutException)
            {
                await batchClient.JobOperations.TerminateJobAsync(jobId);
                Console.WriteLine(incompleteMessage);
                return false;
            }

            await batchClient.JobOperations.TerminateJobAsync(jobId);
            Console.WriteLine(completeMessage);

            // All tasks have reached the "Completed" state, however, this does not guarantee all tasks completed successfully.
            // Here we further check for any tasks with an execution result of "Failure".

            // Obtain the collection of tasks currently managed by the job. 
            // Use a detail level to specify that only the "id" property of each task should be populated. 
            // See https://docs.microsoft.com/azure/batch/batch-efficient-list-queries
            var detail = new ODATADetailLevel(selectClause: "executionInfo");

            // Filter for tasks with 'Failure' result.
            detail.FilterClause = "executionInfo/result eq 'Failure'";

            var failedTasks = await batchClient.JobOperations.ListTasks(jobId, detail).ToListAsync();

            if (failedTasks.Count > 0)
            {
                allTasksSuccessful = false;
                Console.WriteLine(failureMessage);
            }
            else
            {
                Console.WriteLine(successMessage);
            }

            return allTasksSuccessful;
        }

        private static async Task CreateBatchPoolAsync(BatchClient batchClient, string poolId)
        {
            CloudPool pool = null;
            Console.WriteLine("Creating Pool [{0}]", poolId);

            // create image reference object for nodes
            var imageReference = new ImageReference(
                publisher: "MicrosoftWindowsServer",
                offer: "WindowsServer",
                sku: "2012-R2-Datacenter-smalldisk",
                version: "latest");

            // create VirtualMachineConfiguration object from image reference
            var virtualMachineConfiguration = new VirtualMachineConfiguration(
                imageReference,
                "batch.node.windows amd64");

            try
            {
                // create unbound pool, only created when we call CommitAsync()
                pool = batchClient.PoolOperations.CreatePool(
                    poolId,
                    targetDedicatedComputeNodes: DedicatedNodeCount,
                    targetLowPriorityComputeNodes: LowPriorityNodeCount,
                    virtualMachineSize: PoolVMSize,
                    virtualMachineConfiguration: virtualMachineConfiguration
                );

                pool.ApplicationPackageReferences = new List<ApplicationPackageReference>
                {
                    new ApplicationPackageReference
                    {
                        ApplicationId = appPackageId,
                        Version = appPackageVersion
                    }
                };

                await pool.CommitAsync();
            }
            catch (BatchException be)
            {
                // accept if poolexists
                if (be.RequestInformation?.BatchError?.Code == BatchErrorCodeStrings.PoolExists)
                    Console.WriteLine("The pool [{0}] already existed", poolId);
                else
                    throw;
            }
        }

        private static async Task CreateContainerIfNotExistAsync(CloudBlobClient blobClient, string containerName)
        {
            var container = blobClient.GetContainerReference(containerName);
            await container.CreateIfNotExistsAsync();
            Console.WriteLine("Creating container [{0}].", containerName);
        }

        private static async Task<List<ResourceFile>> UploadFilesToContainerAsync(CloudBlobClient blobClient,
            string inputContainerName, List<string> filePaths)
        {
            var resourceFiles = new List<ResourceFile>();

            foreach (var filePath in filePaths)
                resourceFiles.Add(await UploadResourceFileToContainerAsync(blobClient, inputContainerName, filePath));

            return resourceFiles;
        }

        private static async Task<ResourceFile> UploadResourceFileToContainerAsync(CloudBlobClient blobClient,
            string containerName, string filePath)
        {
            Console.WriteLine("Uploading file {0} to container [{1}]...", filePath, containerName);

            var blobName = Path.GetFileName(filePath);
            var fileStream = File.OpenRead(filePath);

            var container = blobClient.GetContainerReference(containerName);
            var blobData = container.GetBlockBlobReference(blobName);
            await blobData.UploadFromFileAsync(filePath);

            // Set the expiry time and permissions for the blob shared access signature. In this case, no start time is specified,
            // so the shared access signature becomes valid immediately
            var sasConstraints = new SharedAccessBlobPolicy
            {
                SharedAccessExpiryTime = DateTime.UtcNow.AddHours(2),
                Permissions = SharedAccessBlobPermissions.Read
            };

            // Construct the SAS URL for the blob
            var sasBlobToken = blobData.GetSharedAccessSignature(sasConstraints);
            var blobSasUri = string.Format("{0}{1}", blobData.Uri, sasBlobToken);

            return ResourceFile.FromUrl(blobSasUri, blobName);
        }

        private static string GetContainerSasUrl(CloudBlobClient blobClient, string containerName,
            SharedAccessBlobPermissions permissions)
        {
            // Set the expiry time and permissions for the container access signature. In this case, no start time is specified,
            // so the shared access signature becomes valid immediately. Expiration is in 2 hours.
            var sasConstraints = new SharedAccessBlobPolicy
            {
                SharedAccessExpiryTime = DateTime.UtcNow.AddHours(2),
                Permissions = permissions
            };

            // Generate the shared access signature on the container, setting the constraints directly on the signature
            var container = blobClient.GetContainerReference(containerName);
            var sasContainerToken = container.GetSharedAccessSignature(sasConstraints);

            // Return the URL string for the container, including the SAS token
            return string.Format("{0}{1}", container.Uri, sasContainerToken);
        }

        private static async Task CreateJobAsync(BatchClient batchClient, string jobId, string poolId)
        {
            Console.WriteLine("Creating job [{0}]...", jobId);

            var job = batchClient.JobOperations.CreateJob();
            job.Id = jobId;
            job.PoolInformation = new PoolInformation {PoolId = poolId};

            await job.CommitAsync();
        }

        private static async Task<List<CloudTask>> AddTasksAsync(BatchClient batchClient, string jobId,
            List<ResourceFile> inputFiles, string outputContainerSasUrl)
        {
            Console.WriteLine("Adding {0} tasks to job [{1}]...", inputFiles.Count, jobId);

            // Create a collection to hold the added tasks
            var tasks = new List<CloudTask>();

            for (var i = 0; i < inputFiles.Count; i++)
            {
                // assign task Id for iteration
                var taskId = string.Format("Task{0}", i);

                var appPath = string.Format("%AZ_BATCH_APP_PACKAGE_{0}#{1}%", appPackageId, appPackageVersion);
                var inputMediaFile = inputFiles[i].FilePath;
                var outputMediaFile = string.Format("{0}{1}",
                    Path.GetFileNameWithoutExtension(inputMediaFile),
                    ".gif");

                // build dos command for ffmpeg
                var taskCommandLine = string.Format("cmd /c {0}\\ffmpeg-3.4-win64-static\\bin\\ffmpeg.exe -i {1} {2}",
                    appPath, inputMediaFile, outputMediaFile);

                // create cloud task from taskId and commandline, add it to the task list
                var task = new CloudTask(taskId, taskCommandLine);
                task.ResourceFiles = new List<ResourceFile> {inputFiles[i]};

                // task output goes to output container in Storage
                var outputFileList = new List<OutputFile>();
                var outputContainer = new OutputFileBlobContainerDestination(outputContainerSasUrl);
                var outputFile = new OutputFile(outputMediaFile, new OutputFileDestination(outputContainer),
                    new OutputFileUploadOptions(OutputFileUploadCondition.TaskSuccess));
                outputFileList.Add(outputFile);
                task.OutputFiles = outputFileList;
                tasks.Add(task);
            }

            // Call BatchClient.JobOperations.AddTask() to add the tasks to a collection rther than making a
            // separate call for each. Bulk taks submission to ensure efficien api calls.
            await batchClient.JobOperations.AddTaskAsync(jobId, tasks);

            return tasks;
        }


        private static async Task Main(string[] args)
        {
            // Read the environment variables to allow the app to connect to the Azure Batch Account
            batchAccountUrl = Environment.GetEnvironmentVariable(envVarBatchURI);
            batchAccountName = "az203batch0922";
            batchAccountKey = Environment.GetEnvironmentVariable(envVarKey);
            storageAccountName = Environment.GetEnvironmentVariable(envVarStorage);
            storageAccountKey = Environment.GetEnvironmentVariable(envVarStorageKey);

            // show the user the batch the app is attaching to
            Console.WriteLine("URL: {0}, Name: {1}, Key: {2}", batchAccountUrl, batchAccountName, batchAccountKey);
            Console.WriteLine("Storage Name: {0}, Key: {1}", storageAccountName, storageAccountKey);

            var storageConnectionString = string.Format("DefaultEndpointsProtocol=https;AccountName={0};AccountKey={1}",
                storageAccountName, storageAccountKey);

            // retrieve storage account
            var storageAccount = CloudStorageAccount.Parse(storageConnectionString);

            // create blob client
            var blobClient = storageAccount.CreateCloudBlobClient();

            // create containers
            const string inputContainerName = "input";
            const string outputContainerName = "output";

            await CreateContainerIfNotExistAsync(blobClient, inputContainerName);
            await CreateContainerIfNotExistAsync(blobClient, outputContainerName);

            // add MP4 files to \<solutiondir>\InputFiles folder
            var inputPath = Path.Combine(Environment.CurrentDirectory, "InputFiles");
            var inputFilePaths = new List<string>(
                Directory.GetFileSystemEntries(inputPath, "*.mp4", SearchOption.TopDirectoryOnly));

            // upload datafiles using UploadResourceFilesToContainer()
            var inputFiles = await UploadFilesToContainerAsync(blobClient, inputContainerName, inputFilePaths);

            // obtain SAS signature with write access to output container
            var outputContainerSasUrl =
                GetContainerSasUrl(blobClient, outputContainerName, SharedAccessBlobPermissions.Write);

            // the batch client requires BatchSharedKeyCredentials object to open a connection
            var sharedKeyCredentials =
                new BatchSharedKeyCredentials(batchAccountUrl, batchAccountName, batchAccountKey);

            using (var batchClient = BatchClient.Open(sharedKeyCredentials))
            {
                // create batch poool contianing compute nodes
                await CreateBatchPoolAsync(batchClient, PoolId);

                // create the job that runs the tasks
                await CreateJobAsync(batchClient, JobId, PoolId);

                // create a collection of tasks and add them to the Batch job
                var runningTasks = await AddTasksAsync(batchClient, JobId, inputFiles, outputContainerSasUrl);

                await MonitorTasksAsync(batchClient, JobId, TimeSpan.FromMinutes(30));

                // Delete input container in storage
                Console.WriteLine("Deleting container [{0}]...", inputContainerName);
                var container = blobClient.GetContainerReference(inputContainerName);
                await container.DeleteIfExistsAsync();

                // Clean up the job (if the user so chooses)
                Console.WriteLine();
                Console.Write("Delete job? [yes] no: ");
                var response = Console.ReadLine().ToLower();
                if (response != "n" && response != "no") await batchClient.JobOperations.DeleteJobAsync(JobId);

                // Clean up the pool (if the user so chooses - do not delete the pool if new batches of videos are ready to process)
                Console.Write("Delete pool? [yes] no: ");
                response = Console.ReadLine().ToLower();
                if (response != "n" && response != "no")
                {
                    Console.WriteLine("Deleting pool ...");
                    await batchClient.PoolOperations.DeletePoolAsync(PoolId);
                    Console.WriteLine("Pool deleted.");
                }
            }
        }
    }
}