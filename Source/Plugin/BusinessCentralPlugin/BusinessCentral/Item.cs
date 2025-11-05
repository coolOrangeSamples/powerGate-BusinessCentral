using System.Text.Json.Serialization;

namespace BusinessCentralPlugin.BusinessCentral
{
    public class ItemMin
    {
        [JsonPropertyName("@odata.etag")]
        public string odataetag { get; set; }
        public string id { get; set; }
        public string number { get; set; }
        public Itemspicture Itemspicture { get; set; }
    }

    public class Itemspicture
    {
        [JsonPropertyName("@odata.etag")]
        public string odataetag { get; set; }
        public string id { get; set; }
        [JsonPropertyName("pictureContent@odata.mediaEditLink")]
        public string pictureContentodatamediaEditLink { get; set; }
        [JsonPropertyName("pictureContent@odata.mediaReadLink")]
        public string pictureContentodatamediaReadLink { get; set; }
    }
}