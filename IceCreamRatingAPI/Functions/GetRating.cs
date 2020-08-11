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
using System.Collections.Generic;
using System.Linq;

namespace Functions
{
    public class GetRating
    {
        [FunctionName("GetRating")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "GetRating/{ratingId}")] HttpRequest req,
                        [CosmosDB(
                            databaseName: "IceCream",
                            collectionName: "Ratings",
                            ConnectionStringSetting = "CosmosDBConnection",
                            SqlQuery = "select * from Ratings r where r.id = {ratingId}",
                            PartitionKey = "/id")]
                            IEnumerable<CreateRatingResponse> ratings, ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            if (ratings == null || !ratings.Any())
            {
                return new NotFoundResult();
            }

            return new OkObjectResult(ratings.First());
        }
    }
}
