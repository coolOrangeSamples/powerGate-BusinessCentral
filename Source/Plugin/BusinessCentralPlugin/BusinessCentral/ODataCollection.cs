using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace BusinessCentralPlugin.BusinessCentral
{
    internal class ODataResponse<T>
    {
        [JsonPropertyName("@odata.context")]
        public string oDataContext { get; set; }

        [JsonPropertyName("value")]
        public List<T> Value { get; set; }
    }
}