using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;

namespace DIT.IntegrationService.Domain.Converters
{
    public class JsonDynamicNameConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType.IsClass && objectType.GetProperties().Any(
                prop => prop.CustomAttributes.Any(
                    attr => attr.AttributeType == typeof(JsonDynamicNameAttribute)));
        }

        public override bool CanRead => true;
        public override object ReadJson(
            JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JObject o = JObject.Load(reader);
            var propertiesWithDynamicNameAttribute = objectType.GetProperties().Where(
                prop => prop.CustomAttributes.Any(attr => attr.AttributeType == typeof(JsonDynamicNameAttribute))
            );

            foreach (var property in propertiesWithDynamicNameAttribute)
            {
                var dynamicAttributeData = property.CustomAttributes.FirstOrDefault(
                    attr => attr.AttributeType == typeof(JsonDynamicNameAttribute));

                var propertyNameContainingNewName = property.Name;
                var currentName = (string)dynamicAttributeData.ConstructorArguments[0].Value;

                var currentJsonPropertyValue = o[currentName];
                //if (currentJsonPropertyValue == null)
                //    throw new Exception(
                //        $"{nameof(JsonDynamicNameAttribute)} contains arugments that were not defined in the json document.");

                var newJsonProperty = new JProperty(propertyNameContainingNewName, currentJsonPropertyValue);
                currentJsonPropertyValue?.Parent.Replace(newJsonProperty);
            }
            return o.ToObject(objectType);
        }

        public override bool CanWrite => true;
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var token = JToken.FromObject(value);
            if (token.Type != JTokenType.Object)
            {
                throw new Exception("Token to be serialized was unexpectedly not an object.");
            }

            JObject o = (JObject)token;
            var propertiesWithDynamicNameAttribute = value.GetType().GetProperties().Where(
                prop => prop.CustomAttributes.Any(attr => attr.AttributeType == typeof(JsonDynamicNameAttribute))
            );

            foreach (var property in propertiesWithDynamicNameAttribute)
            {
                var dynamicAttributeData = property.CustomAttributes.FirstOrDefault(
                    attr => attr.AttributeType == typeof(JsonDynamicNameAttribute));

                var currentName = property.Name;
                var propertyNameContainingNewName = (string)dynamicAttributeData.ConstructorArguments[1].Value;

                var currentJsonPropertyValue = o[currentName];
                if (property.PropertyType == typeof(Guid?))
                {
                    if (currentJsonPropertyValue != null)
                    {
                        var newValue = $"/{(string)dynamicAttributeData.ConstructorArguments[2].Value}({currentJsonPropertyValue.ToObject<Guid>()})";
                        var newJsonProperty = new JProperty(propertyNameContainingNewName, newValue);
                        currentJsonPropertyValue.Parent.Replace(newJsonProperty);
                    }
                }
            }

            token.WriteTo(writer);
        }
    }
}
