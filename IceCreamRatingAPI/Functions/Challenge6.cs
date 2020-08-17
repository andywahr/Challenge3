using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;

namespace Functions
{

    public class Challenge6
    {
        private IHttpClientFactory _httpClientFactory;

        public Challenge6(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [FunctionName("CombineOrder")]
        public async Task CombineOrder([OrchestrationTrigger] IDurableOrchestrationContext context,
                                       [CosmosDB(databaseName: "IceCream", collectionName: "Orders", ConnectionStringSetting = "CosmosDBConnection")] IAsyncCollector<OrderInfo> orders,
                                       ILogger log)
        {
            string orderid = context.InstanceId;

            var orderHeaderDetails = context.WaitForExternalEvent<string>("OrderHeaderDetails");
            var orderLineItems = context.WaitForExternalEvent<string>("OrderLineItems");
            var productInformation = context.WaitForExternalEvent<string>("ProductInformation");

            // all three departments must grant approval before a permit can be issued
            await Task.WhenAll(orderHeaderDetails, orderLineItems, productInformation);

            log.LogInformation($"Adding Order {orderid}");

            var mgmtApi = _httpClientFactory.CreateClient("Management");

string message = @$"{{
    ""orderHeaderDetailsCSVUrl"": ""{await orderHeaderDetails}"",
    ""orderLineItemsCSVUrl"": ""{await orderLineItems}"",
    ""productInformationCSVUrl"": ""{await productInformation}""
}}";

            var resp = await mgmtApi.PostAsync("/api/order/combineOrderContent", new StringContent(message, System.Text.Encoding.UTF8, "application/json"));

            if (resp.IsSuccessStatusCode)
            {
                string body = await resp.Content.ReadAsStringAsync();
                IEnumerable<OrderInfo> combinedOrders = Newtonsoft.Json.JsonConvert.DeserializeObject<IEnumerable<OrderInfo>>(body);

                foreach ( var order in combinedOrders)
                {
                    log.LogInformation($"Adding order {order.id}");
                    await orders.AddAsync(order);
                }
            }
            else
            {
                log.LogWarning($" Combined Failed: {(int)resp.StatusCode} - {resp.ReasonPhrase}");
            }

            //OrderInfo info = new OrderInfo()
            //{
            //    id = orderid,
            //    orderId = orderid,
            //    OrderHeaderDetails = OrderHeaderDetail.Parse(await orderHeaderDetails),
            //    OrderLineItems = OrderLineItem.Parse(await orderLineItems),
            //    ProductInformation = ProductInformation.Parse(await productInformation)
            //};

            //await orders.AddAsync(info);
        }

        [FunctionName("OrderWatcher")]
        public static async Task OrderWatcher([BlobTrigger("challenge6/{name}", Connection = "Challenge6Storage")] CloudBlockBlob myBlob, string name, ILogger log,
                                              [DurableClient] IDurableClient starter)
        {
            log.LogInformation($"Processing file {name}");

            string[] toks = name.Split('-');

            if (toks.Length > 1)
            {
                string id = toks[0];
                string fileName = toks[1].Split('.')[0];
                var instanceForPrefix = await starter.GetStatusAsync(id);

                if (instanceForPrefix == null)
                {
                    log.LogInformation($@"New instance needed for prefix '{id}'. Starting...");
                    var retval = await starter.StartNewAsync(@"CombineOrder", id);
                }

                //Set the expiry time and permissions for the blob.
                //In this case the start time is specified as a few minutes in the past, to mitigate clock skew.
                //The shared access signature will be valid immediately.
                SharedAccessBlobPolicy sasConstraints = new SharedAccessBlobPolicy();
                sasConstraints.SharedAccessStartTime = DateTime.UtcNow.AddMinutes(-5);
                sasConstraints.SharedAccessExpiryTime = DateTime.UtcNow.AddHours(24);
                sasConstraints.Permissions = SharedAccessBlobPermissions.Read;

                //Generate the shared access signature on the blob, setting the constraints directly on the signature.
                string sasBlobToken = myBlob.GetSharedAccessSignature(sasConstraints);

                await starter.RaiseEventAsync(id, fileName, myBlob.Uri.ToString() + sasBlobToken);
            }
        }
    }
}