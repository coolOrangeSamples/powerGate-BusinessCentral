using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace BusinessCentralPlugin.BusinessCentral
{
    public class ProductionBOMMin
    {
        [JsonPropertyName("@odata.context")]
        public string odatacontext { get; set; }
        [JsonPropertyName("@odata.etag")]
        public string odataetag { get; set; }
        public string No { get; set; }
    }

    public class ProductionBOM
    {
        [JsonPropertyName("@odata.context")]
        public string odatacontext { get; set; }
        [JsonPropertyName("@odata.etag")]
        public string odataetag { get; set; }
        public string No { get; set; }
        public string Description { get; set; }
        public string Unit_of_Measure_Code { get; set; }
        public List<ProdBOMLine> ProductionBOMsProdBOMLine { get; set; }
    }
}
