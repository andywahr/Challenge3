using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

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
}
