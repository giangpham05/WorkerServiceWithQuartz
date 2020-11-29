using System;

namespace DIT.IntegrationService.Domain.Converters
{
    [AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    public class JsonDynamicNameAttribute : Attribute
    {
        public string ReadName { get; }
        public string WriteName { get; set; }
        public string PluralName { get; set; }

        public JsonDynamicNameAttribute(string readName, string writeName, string pluralName)
        {
            ReadName = readName;
            WriteName = writeName;
            PluralName = pluralName;
        }
    }
}
