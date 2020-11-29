using DIT.IntegrationService.Domain.Converters;
using DIT.IntegrationService.Domain.OData;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace DIT.IntegrationService.Domain.Crm
{
    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class Product : BaseEntity
    {
        [JsonProperty(PropertyName = "productid")]
        public Guid ProductId
        {
            get => Id;
            set => Id = value;
        }

        [JsonProperty(PropertyName = "productnumber")]
        public string ProductNumber { get; set; }

        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }
        [JsonProperty(PropertyName = "description")]
        public string Description { get; set; }

        [JsonProperty(PropertyName = "currentcost")]
        public decimal? CurrentCost { get; set; }

        [JsonProperty(PropertyName = "quantitydecimal")]
        public int? QuantityDecimal { get; set; }

        [JsonDynamicName("_dict_pricelevelid_value", "pricelevelid@odata.bind", PluralNameConstants.PriceLevels)]
        public Guid? DefaultPriceList { get; set; }

        [JsonDynamicName("_dict_itemcategoryid_value", "dict_itemcategoryid@odata.bind", PluralNameConstants.ItemCategories)]
        public Guid? ItemCategoryId { get; set; }

        [JsonDynamicName("_defaultuomid_value", "defaultuomid@odata.bind", PluralNameConstants.UoMs)]
        public Guid? DefaultUomId { get; set; }

        [JsonDynamicName("_defaultuomscheduleid_value", "defaultuomscheduleid@odata.bind", PluralNameConstants.UoMSchedules)]
        public Guid? DefaultUomScheduleId { get; set; }

        [JsonProperty(PropertyName = "vendorname")]
        public string VendorName { get; set; }

        [JsonProperty(PropertyName = "statecode")]
        public int? StateCode { get; set; }

        [JsonProperty(PropertyName = "statuscode")]
        public int? StatusCode { get; set; }
    }
}
