using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using System;
using System.Threading.Tasks;
using System.Web.Http;

namespace OrderItemsReserver
{
    public static class UploadBlobHttpTriggerFunc
    {
        private const string ContainerName = "orders";

        [FunctionName("OrderItemsReserver")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log, ExecutionContext context)
        {
            log.LogInformation($"C# Http trigger function executed at: {DateTime.Now}");

            try
            {
                CreateContainerIfNotExists(log, context);

                var storageAccount = GetCloudStorageAccount(context);
                var blobClient = storageAccount.CreateCloudBlobClient();
                var container = blobClient.GetContainerReference(ContainerName);

                var blob = container.GetBlockBlobReference(Guid.NewGuid().ToString());
                blob.Properties.ContentType = "application/json";
                await blob.UploadFromStreamAsync(req.Body);

                log.LogInformation($"Blob is uploaded to container {container.Name}");

                await blob.SetPropertiesAsync();

                return new OkObjectResult("OK");
            }
            catch (Exception e)
            {
                return new BadRequestErrorMessageResult($"Error. {e.Message}");
            }
        }

        private static void CreateContainerIfNotExists(ILogger logger, ExecutionContext executionContext)
        {
            var storageAccount = GetCloudStorageAccount(executionContext);
            var blobClient = storageAccount.CreateCloudBlobClient();
            var containers = new[] { ContainerName };
            foreach (var item in containers)
            {
                var blobContainer = blobClient.GetContainerReference(item);
                blobContainer.CreateIfNotExistsAsync();
            }
        }

        private static CloudStorageAccount GetCloudStorageAccount(ExecutionContext executionContext)
        {
            var config = new ConfigurationBuilder()
                            .SetBasePath(executionContext.FunctionAppDirectory)
                            .AddJsonFile("local.settings.json", true, true)
                            .AddEnvironmentVariables().Build();
            var storageAccount = CloudStorageAccount.Parse(config["CloudStorageAccount"]);

            return storageAccount;
        }
    }
}