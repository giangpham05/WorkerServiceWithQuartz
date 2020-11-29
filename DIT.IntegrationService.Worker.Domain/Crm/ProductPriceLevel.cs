using DIT.IntegrationService.Domain.Converters;
using DIT.IntegrationService.Domain.OData;
using Newtonsoft.Json;
using System;

namespace DIT.IntegrationService.Domain.Crm
{
    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class ProductPriceLevel : BaseEntity
    {
        [JsonProperty(PropertyName = "productpricelevelid")]
        public Guid ProductPriceLevelId
        {
            get => Id;
            set => Id = value;
        }

        [JsonDynamicName("_productid_value", "productid@odata.bind", PluralNameConstants.Products)]
        public Guid? ProductId { get; set; }

        [JsonDynamicName("_uomid_value", "uomid@odata.bind", PluralNameConstants.UoMs)]
        public Guid? DefaultUomId { get; set; }

        [JsonDynamicName("_dict_pricelevelid_value", "pricelevelid@odata.bind", PluralNameConstants.PriceLevels)]
        public Guid? DefaultPriceList { get; set; }

        [JsonProperty(PropertyName = "amount")]
        public decimal? Amount { get; set; }
    }
}
