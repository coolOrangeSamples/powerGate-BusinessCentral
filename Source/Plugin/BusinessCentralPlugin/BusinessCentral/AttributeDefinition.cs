using System.Text.Json.Serialization;

namespace BusinessCentralPlugin.BusinessCentral
{
    public class AttributeDefinition
    {
        [JsonPropertyName("@odata.etag")]
        public string odataetag { get; set; }
        public int ID { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public bool Blocked { get; set; }
    }
}
