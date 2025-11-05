using System.Text.Json.Serialization;

namespace BusinessCentralPlugin.BusinessCentral
{
    public class Vendor
    {
        [JsonPropertyName("@odata.etag")]
        public string odataetag { get; set; }
        public string id { get; set; }
        public string number { get; set; }
        public string displayName { get; set; }
    }
}