using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;

namespace Functions
{
    public static class Challenge8
    {
        [FunctionName("GreaterThan100")]
        public static async Task GreaterThan100([ServiceBusTrigger("receipts", "ordersgreaterthan100", Connection = "Challenge8ServiceBus")] SalesEvent salesEvent,
                                                [Blob("receipts-high-value", FileAccess.Write, Connection = "Challenge8Storage")] CloudBlobContainer blobContainer, ILogger log)
        {
            log.LogInformation($"*** Processing large order {salesEvent.SalesHeader.salesNumber} ***");
            await persistReceipt(salesEvent, blobContainer);
        }

        [FunctionName("LessThan100")]
        public static async Task LessThan100([ServiceBusTrigger("receipts", "orderslessthan100", Connection = "Challenge8ServiceBus")] SalesEvent salesEvent,
                                             [Blob("receipts", FileAccess.Write, Connection = "Challenge8Storage")] CloudBlobContainer blobContainer, ILogger log)
        {
            log.LogInformation($"Processing normal order {salesEvent.SalesHeader.salesNumber}");
            await persistReceipt(salesEvent, blobContainer, true);
        }

        private static async Task persistReceipt(SalesEvent salesEvent, CloudBlobContainer blobContainer, bool skipReceiptPDF = false)
        {
            Receipt receipt = await Receipt.FromSalesEvent(salesEvent, skipReceiptPDF);
            var newBlob = blobContainer.GetBlockBlobReference($"{salesEvent.SalesHeader.salesNumber}.json");
            await newBlob.UploadTextAsync(Newtonsoft.Json.JsonConvert.SerializeObject(receipt));
        }
    }
}
