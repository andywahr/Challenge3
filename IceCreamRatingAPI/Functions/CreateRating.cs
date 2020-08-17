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
using System.Text;
using System.Collections.Generic;
using System.Linq;

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

// start of Challenge 9

            if (!string.IsNullOrEmpty(newRating.userNotes))
            {
                var sentinmentApi = _httpClientFactory.CreateClient("TextAnalysis");
                string sentimentRequest = $@"{{
  ""documents"": [
    {{
      ""language"": ""en"",
      ""id"": ""{newRating.id}"",
      ""text"": ""{newRating.userNotes}""
    }}
  ]
}}";
                var sentimentAPIResponse = await sentinmentApi.PostAsync("/text/analytics/v3.0/sentiment", new StringContent(sentimentRequest, System.Text.Encoding.UTF8, "application/json"));
                SentimentResponse sentimentResponse = JsonConvert.DeserializeObject<SentimentResponse>(await sentimentAPIResponse.Content.ReadAsStringAsync());

                newRating.sentimentScore = sentimentResponse.documents.FirstOrDefault().sentiment ?? "unknown";
            }
            else
            {
                newRating.sentimentScore = "unknown";
            }


            if ( string.Equals(newRating.sentimentScore, "negative", StringComparison.OrdinalIgnoreCase))
            {
                log.LogInformation($"NEGATIVE USER NOTE: {newRating.userNotes}");
            }
// end of challenge 9

            await ratings.AddAsync(newRating);

            return new OkObjectResult(newRating);
        }
    }

    public class SentimentResponse
    {
        public IEnumerable<Sentiment> documents { get; set; }
    }

    public class Sentiment
    {
        public string id { get; set; }
        public string sentiment { get; set; }
    }
}
