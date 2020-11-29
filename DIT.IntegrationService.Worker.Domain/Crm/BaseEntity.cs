using Newtonsoft.Json;
using System;

namespace DIT.IntegrationService.Domain.Crm
{
    public class BaseEntity
    {
        [JsonIgnore()]
        public Guid Id { get; set; }

        // public bool ShouldSerializeId() => false;
    }
}
