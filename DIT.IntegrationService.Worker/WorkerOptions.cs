using System;
using System.Collections.Generic;

namespace DIT.IntegrationService.Worker
{
    public class WorkerOptions
    {
        public string ConnectionString { get; set; }
        public SalesOptions SalesOptions { get; set; }
        public AdOption AdOption { get; set; }
        public string CronExpression { get; set; }
    }

    public class SalesOptions
    {
        public Guid UoMScheduleId { get; set; }
        public Guid UoMId { get; set; }
        public Guid DefaultPriceList { get; set; }

    }

    public class AdOption
    {
        public string Instance { get; set; }
        public string ClientId { get; set; }
        public string TenantId { get; set; }
        public string ClientSecret { get; set; }
        public string OrganizationUrl { get; set; }
        public string CrmApi { get; set; }
    }
}