using System.Text.Json.Serialization;

namespace BusinessCentralPlugin.BusinessCentral
{
    public class ItemCardMin
    {
        [JsonPropertyName("@odata.etag")]
        public string odataetag { get; set; }
        public string No { get; set; }
        public string Description { get; set; }
    }

    public class ItemCard
    {
        [JsonPropertyName("@odata.etag")]
        public string odataetag { get; set; }
        public string No { get; set; }
        public string Description { get; set; }
        public bool Blocked { get; set; }
        public string Type { get; set; }
        public string Base_Unit_of_Measure { get; set; }
        public string Item_Category_Code { get; set; }
        public double Net_Weight { get; set; }
        public string Gen_Prod_Posting_Group { get; set; }
        public string Inventory_Posting_Group { get; set; }
        public double Unit_Price { get; set; }
        public double Inventory { get; set; }
        public string Vendor_No { get; set; }
        public string Replenishment_System { get; set; }
        public string Routing_No { get; set; }
        public string Production_BOM_No { get; set; }
        public double Rounding_Precision { get; set; }
        public bool AssemblyBOM { get; set; }
    }
}