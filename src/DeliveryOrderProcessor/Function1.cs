using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web.Http;
using DeliveryOrderProcessor.Entities;

namespace DeliveryOrderProcessor
{
    public static class DeliveryOrderProcessorFunc
    {
        [FunctionName("DeliveryOrderProcessor")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log, ExecutionContext context)
        {
            log.LogInformation($"C# Http trigger function executed at: {DateTime.Now}");

            try
            {
                var endpointUri = GetValueFromSettings(context, "EndpointUri");
                var primaryKey = GetValueFromSettings(context, "PrimaryKey");

                var options = new CosmosClientOptions()
                {
                    SerializerOptions = new CosmosSerializationOptions()
                    {
                        PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
                    }
                };
                using (var client = new CosmosClient(endpointUri, primaryKey, options))
                {
                    var databaseResponse = await client.CreateDatabaseIfNotExistsAsync("Orders");
                    var database = databaseResponse.Database;

                    var containerResponse = await database.CreateContainerIfNotExistsAsync("orders", "/buyerId");
                    var container = containerResponse.Container;
                    var order = await JsonSerializer.DeserializeAsync<Order>(req.Body, new JsonSerializerOptions()
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    });

                    var delivery = new Delivery(order);
                    var itemResponse = await container.CreateItemAsync(delivery);
                    var statusCode = itemResponse.StatusCode;

                    if (statusCode == HttpStatusCode.BadRequest)
                        throw new Exception($"Status code: {itemResponse.StatusCode}");
                }

                return new OkObjectResult("OK");
            }
            catch (Exception e)
            {
                log.LogError(e.Message);
                return new BadRequestErrorMessageResult($"Error. {e.Message}");
            }
        }

        private static string GetValueFromSettings(ExecutionContext executionContext, string settingsKey)
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(executionContext.FunctionAppDirectory)
                .AddJsonFile("local.settings.json", true, true)
                .AddEnvironmentVariables().Build();
            var storageAccount = config[settingsKey];

            return storageAccount;
        }
    }
}