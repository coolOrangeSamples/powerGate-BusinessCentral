using System.Text.Json.Serialization;

namespace BusinessCentralPlugin.BusinessCentral
{
    public class RoutingLink
    {
        [JsonPropertyName("@odata.etag")]
        public string odataetag { get; set; }
        public string Code { get; set; }
        public string Description { get; set; }
    }
}