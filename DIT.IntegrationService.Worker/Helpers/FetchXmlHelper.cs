using System;
using System.Net;
using System.Text;

namespace DIT.IntegrationService.Worker.Helpers
{
    public class FetchXmlHelper
    {
        public static string FetchItemCategories(params string[] cols) =>
            @$"<fetch mapping='logical' >
                  <entity name='dict_itemcategory' >
                    {BuildAttributesString(cols)}
                  </entity>
                </fetch>";

        public static string FindItemCategoryBy(string categoryCode, params string[] cols) =>
           @$"<fetch mapping='logical' >
                  <entity name='dict_itemcategory' >
                    {BuildAttributesString(cols)}
                    <filter>
                      <condition attribute='dict_category' operator='eq' value='{WebUtility.UrlEncode(categoryCode)}' />
                    </filter>
                  </entity>
                </fetch>";

        public static string GetProductBy(string navItemNo, params string[] cols) =>
           @$"<fetch mapping='logical' >
                  <entity name='product' >
                    {BuildAttributesString(cols)}
                    <filter>
                      <condition attribute='productnumber' operator='eq' value='{WebUtility.UrlEncode(navItemNo)}' />
                    </filter>
                  </entity>
                </fetch>";

        public static string GetProductWithCategoryBy(string navItemNo, params string[] cols) =>
           @$"<fetch mapping='logical' >
                  <entity name='product' >
                    {BuildAttributesString(cols)}
                    <filter>
                      <condition attribute='productnumber' operator='eq' value='{WebUtility.UrlEncode(navItemNo)}' />
                    </filter>
                    <link-entity name='dict_itemcategory' from='dict_itemcategoryid' to='dict_itemcategoryid' link-type='outer' alias='category' >
                          <attribute name='dict_category' />
                    </link-entity>
                  </entity>
                </fetch>";

        public static string FindProductPriceLevelBy(Guid productId, Guid priceLevelId, params string[] cols) =>
           @$"<fetch mapping='logical' >
                  <entity name='productpricelevel' >
                    {BuildAttributesString(cols)}
                    <filter>
                      <condition attribute='productid' operator='eq' value='{productId}' />
                      <condition attribute='pricelevelid' operator='eq' value='{priceLevelId}' />
                    </filter>
                  </entity>
                </fetch>";

        private static string BuildAttributesString(params string[] cols)
        {
            StringBuilder builder = new StringBuilder();
            foreach (var col in cols)
            {
                if (!string.IsNullOrEmpty(col))
                    builder.AppendLine($"<attribute name='{col}' />");
            }

            return builder.Length != 0 ? builder.ToString() :
                builder.Clear().Append("<all-attributes/>").ToString();
        }
    }
}
