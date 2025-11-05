using System;
using System.Text.Json.Serialization;

namespace BusinessCentralPlugin.BusinessCentral
{
    public class Document
    {
        [JsonPropertyName("@odata.etag")]
        public string odataetag { get; set; }
        public string id { get; set; }
        public string fileName { get; set; }
        public int byteSize { get; set; }
        public string parentType { get; set; }
        public string parentId { get; set; }
        public int lineNumber { get; set; }
        public bool documentFlowSales { get; set; }
        public bool documentFlowPurchase { get; set; }
        public DateTime lastModifiedDateTime { get; set; }

        [JsonPropertyName("attachmentContent@odata.mediaEditLink")]
        public string attachmentContentodatamediaEditLink { get; set; }

        [JsonPropertyName("attachmentContent@odata.mediaReadLink")]
        public string attachmentContentodatamediaReadLink { get; set; }
    }
}