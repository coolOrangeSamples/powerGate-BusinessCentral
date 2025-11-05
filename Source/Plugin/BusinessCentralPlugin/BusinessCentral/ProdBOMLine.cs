using System.Text.Json.Serialization;

namespace BusinessCentralPlugin.BusinessCentral
{
    public class ProdBOMLineMin
    {
        [JsonPropertyName("@odata.context")]
        public string odatacontext { get; set; }
        [JsonPropertyName("@odata.etag")]
        public string odataetag { get; set; }
        public string Production_BOM_No { get; set; }
        public int Line_No { get; set; }
        public string No { get; set; }
    }

    public class ProdBOMLine
    {
        [JsonPropertyName("@odata.context")]
        public string odatacontext { get; set; }
        [JsonPropertyName("@odata.etag")]
        public string odataetag { get; set; }
        public string Production_BOM_No { get; set; }
        public int Line_No { get; set; }
        public string No { get; set; }
        public string Description { get; set; }
        public decimal Quantity_per { get; set; }
        public string Unit_of_Measure_Code { get; set; }
        public string Routing_Link_Code { get; set; }
    }
}
