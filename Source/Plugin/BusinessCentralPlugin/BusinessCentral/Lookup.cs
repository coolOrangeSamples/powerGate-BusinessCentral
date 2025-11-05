using System.Text.Json.Serialization;

namespace BusinessCentralPlugin.BusinessCentral
{
    public class Lookup
    {
        [JsonPropertyName("@odata.etag")]
        public string odataetag { get; set; }
        public string id { get; set; }
        public string code { get; set; }
    }
}
