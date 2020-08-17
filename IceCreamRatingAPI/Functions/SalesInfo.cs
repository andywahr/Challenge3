using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Functions
{
    public class SalesEvent
    {
        public string id
        {
            get
            {
                return SalesHeader?.salesNumber;
            }
            set
            {

            }
        }

        [JsonProperty("header")]
        public SalesHeader SalesHeader { get; set; }
        [JsonProperty("details")]
        public IEnumerable<SalesLineItem> SalesLineItems { get; set; }
    }

    public class SalesHeader
    {
        public string salesNumber { get; set; }
        public DateTimeOffset datetime { get; set; }
        public string locationid { get; set; }
        public string locationname { get; set; }
        public string locationaddress { get; set; }
        public int locationpostcode { get; set; }
        public float totalcost { get; set; }
        public float totaltax { get; set; }
        public string receiptUrl { get; set; }
    }

    public class SalesLineItem
    {
        public Guid productid { get; set; }
        public int quantity { get; set; }
        public float unitcost { get; set; }
        public float totalcost { get; set; }
        public float totaltax { get; set; }
        public string productname { get; set; }
        public string productdescription { get; set; }
    }

    public class OrderInfo
    {

        public string id
        {
            get
            {
                return SalesHeader?.salesNumber;
            }
            set
            {

            }
        }
        [JsonProperty("headers")]
        public SalesHeader SalesHeader { get; set; }
        [JsonProperty("details")]
        public IEnumerable<SalesLineItem> SalesLineItems { get; set; }
    }


    public class Receipt
    {
        public Receipt()
        {

        }

        public static async Task<Receipt> FromSalesEvent(SalesEvent salesEvent, bool skipReceipt = false)
        {
            string receiptImage = string.Empty;

            if (!string.IsNullOrEmpty(salesEvent.SalesHeader.receiptUrl) && !skipReceipt)
            {
                using (HttpClient httpClient = new HttpClient())
                {
                    receiptImage = System.Convert.ToBase64String(await httpClient.GetByteArrayAsync(salesEvent.SalesHeader.receiptUrl));
                }
            }

            return new Receipt()
            {
                Store = salesEvent.SalesHeader.locationid,
                SalesNumber = salesEvent.SalesHeader.salesNumber,
                TotalCost = salesEvent.SalesLineItems.Sum(s => s.totalcost),
                Items = salesEvent.SalesLineItems.Sum(s => s.quantity),
                SalesDate = salesEvent.SalesHeader.datetime,
                ReceiptImage = receiptImage
            };
        }

        public string id
        {
            get
            {
                return SalesNumber.ToString();
            }
            set
            {

            }
        }

        [JsonProperty("Store")]
        public string Store { get; set; }

        [JsonProperty("SalesNumber")]
        public string SalesNumber { get; set; }

        [JsonProperty("TotalCost")]
        public float TotalCost { get; set; }

        [JsonProperty("Items")]
        public int Items { get; set; }

        [JsonProperty("SalesDate")]
        public DateTimeOffset SalesDate { get; set; }

        [JsonProperty("ReceiptImage")]
        public string ReceiptImage { get; set; }

    }
}
