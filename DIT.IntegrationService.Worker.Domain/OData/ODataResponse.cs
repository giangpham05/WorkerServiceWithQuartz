using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace DIT.IntegrationService.Domain.OData
{

    public class ODataResponse<T> where T : class
    {
        [JsonProperty(PropertyName = "@odata.context")]
        public string Context { get; set; }

        [JsonProperty(PropertyName = "value")]
        public List<T> Value { get; set; }
    }
}
