using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.EventHubs;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.ServiceBus;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Functions
{
    public static class Challenge7
    {
        [FunctionName("Challenge7")]
        [return: ServiceBus("receipts", Connection = "Challenge8ServiceBus", EntityType = EntityType.Topic)] // added Challenge 8
        public static async Task<Message> Run([EventHubTrigger("challenge7", Connection = "Challenge7EventHub")] EventData[] events,
                                     [CosmosDB(databaseName: "IceCream", collectionName: "SalesEvents", ConnectionStringSetting = "CosmosDBConnection")] IAsyncCollector<SalesEvent> salesEvents,
                                     ILogger log)

        {
            var exceptions = new List<Exception>();

            foreach (EventData eventData in events)
            {
                try
                {
                    string messageBody = Encoding.UTF8.GetString(eventData.Body.Array, eventData.Body.Offset, eventData.Body.Count);
                    SalesEvent salesEvent = Newtonsoft.Json.JsonConvert.DeserializeObject<SalesEvent>(messageBody);

                    Message msg = new Message(eventData.Body.Array); // added Challenge 8
                    msg.UserProperties.Add("totalcost", salesEvent.SalesLineItems.Sum(i => i.totalcost)); // added Challenge 8

                    await salesEvents.AddAsync(salesEvent);
                    return msg; // added Challenge 8
                }
                catch (Exception e)
                {
                    // We need to keep processing the rest of the batch - capture this exception and continue.
                    // Also, consider capturing details of the message that failed processing so it can be processed again later.
                    exceptions.Add(e);
                }
            }

            // Once processing of the batch is complete, if any messages in the batch failed processing throw an exception so that there is a record of the failure.

            if (exceptions.Count > 1)
                throw new AggregateException(exceptions);

            if (exceptions.Count == 1)
                throw exceptions.Single();

            return null;
        }
    }
}
