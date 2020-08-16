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
using Newtonsoft.Json;

namespace Functions
{

    public static class Challenge6
    {
        [FunctionName("CombineOrder")]
        public static async Task CombineOrder([OrchestrationTrigger] IDurableOrchestrationContext context,
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

            OrderInfo info = new OrderInfo()
            {
                id = orderid,
                orderId = orderid,
                OrderHeaderDetails = OrderHeaderDetail.Parse(await orderHeaderDetails),
                OrderLineItems = OrderLineItem.Parse(await orderLineItems),
                ProductInformation = ProductInformation.Parse(await productInformation)
            };

            await orders.AddAsync(info);
        }


        [FunctionName("OrderWatcher")]
        public static async Task Run([BlobTrigger("challenge6/{name}", Connection = "Challenge6Storage")] string myBlob, string name, ILogger log,
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

                await starter.RaiseEventAsync(id, fileName, myBlob);
            }
        }
    }

    public class OrderInfo
    {
        public string id { get; set; } = Guid.NewGuid().ToString();

        [JsonProperty("orderId")]
        public string orderId { get; set; }
        [JsonProperty("header")]
        public IEnumerable<OrderHeaderDetail> OrderHeaderDetails { get; set; }
        [JsonProperty("items")]
        public IEnumerable<OrderLineItem> OrderLineItems { get; set; }
        [JsonProperty("product")]
        public IEnumerable<ProductInformation> ProductInformation { get; set; }
    }

    public class OrderHeaderDetail
    {
        public static IEnumerable<OrderHeaderDetail> Parse(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return Enumerable.Empty<OrderHeaderDetail>();
            }

            string[] lines = value.Split("\r\n");

            if (lines.Length == 1)
            {
                return Enumerable.Empty<OrderHeaderDetail>();
            }

            return lines.Skip(1).Select(line =>
            {
                string[] toks = line.Split(',');

                return new OrderHeaderDetail()
                {
                    ponumber = toks[0],
                    datetime = DateTimeOffset.Parse(toks[1]),
                    locationid = toks[2],
                    locationname = toks[3],
                    locationaddress = toks[4],
                    locationpostcode = int.Parse(toks[5]),
                    totalcost = float.Parse(toks[6]),
                    totaltax = float.Parse(toks[7])
                };
            });
        }

        public string ponumber { get; set; }
        public DateTimeOffset datetime { get; set; }
        public string locationid { get; set; }
        public string locationname { get; set; }
        public string locationaddress { get; set; }
        public int locationpostcode { get; set; }
        public float totalcost { get; set; }
        public float totaltax { get; set; }
    }

    public class OrderLineItem
    {
        public static IEnumerable<OrderLineItem> Parse(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return Enumerable.Empty<OrderLineItem>();
            }

            string[] lines = value.Split("\r\n");

            if (lines.Length == 1)
            {
                return Enumerable.Empty<OrderLineItem>();
            }

            return lines.Skip(1).Select(line =>
            {
                string[] toks = line.Split(',');

                return new OrderLineItem()
                {
                    ponumber = toks[0],
                    productid = Guid.Parse(toks[1]),
                    quantity = int.Parse(toks[2]),
                    unitcost = float.Parse(toks[3]),
                    totalcost = float.Parse(toks[4]),
                    totaltax = float.Parse(toks[5])
                };
            });
        }

        public string ponumber { get; set; }
        public Guid productid { get; set; }
        public int quantity { get; set; }
        public float unitcost { get; set; }
        public float totalcost { get; set; }
        public float totaltax { get; set; }
    }

    public class ProductInformation
    {
        public static IEnumerable<ProductInformation> Parse(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return Enumerable.Empty<ProductInformation>();
            }

            string[] lines = value.Split("\r\n");

            if (lines.Length == 1)
            {
                return Enumerable.Empty<ProductInformation>();
            }

            return lines.Skip(1).Select(line =>
            {
                string[] toks = line.Split(',');

                return new ProductInformation()
                {
                    productid = Guid.Parse(toks[0]),
                    productname = toks[1],
                    productdescription = toks[2]
                };
            });
        }

        public Guid productid { get; set; }
        public string productname { get; set; }
        public string productdescription { get; set; }

    }

}