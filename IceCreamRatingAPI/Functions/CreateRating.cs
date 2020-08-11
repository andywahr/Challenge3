using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net.Http;
using System.Web.Http;
using Microsoft.Azure.Documents.SystemFunctions;

namespace Functions
{
    public class CreateRating
    {
        private IHttpClientFactory _httpClientFactory;

        public CreateRating(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [FunctionName("CreateRating")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
                        [CosmosDB(
                            databaseName: "IceCream",
                            collectionName: "Ratings",
                            ConnectionStringSetting = "CosmosDBConnection",
                            Id = "id",
                            PartitionKey = "/id")]
                            IAsyncCollector<CreateRatingResponse> ratings, ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var data = JsonConvert.DeserializeObject<CreateRatingRequest>(requestBody);

            var userAPI = _httpClientFactory.CreateClient("Users");
            var productAPI = _httpClientFactory.CreateClient("Products");

            if (data.rating < 0 || data.rating > 5)
            {
                return new BadRequestErrorMessageResult($"Rating of {data.rating} is not between 0 and 5.");
            }

            var userResp = await userAPI.GetAsync($"/api/GetUser?userId={data.userId}");

            if (!userResp.IsSuccessStatusCode)
            {
                return new BadRequestErrorMessageResult($"User id [{data.userId}] doesn't exists");
            }

            var productResp = await productAPI.GetAsync($"/api/GetProduct?productId={data.productId}");

            if (!productResp.IsSuccessStatusCode)
            {
                return new BadRequestErrorMessageResult($"Product id [{data.productId}] doesn't exists");
            }

            var newRating = new CreateRatingResponse(data);

            await ratings.AddAsync(newRating);

            return new OkObjectResult(newRating);
        }
    }
}
