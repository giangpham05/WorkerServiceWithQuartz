using System;
using System.Collections.Generic;
using System.Text;

namespace DIT.IntegrationService.Domain
{
    public class StockItem
    {
        public string StockCode { get; set; }
        public decimal RetailPriceInc { get; set; }
        public decimal ResellerPriceInc { get; set; }
    }
}
