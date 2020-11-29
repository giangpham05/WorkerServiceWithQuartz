using CSharpFunctionalExtensions;
using Dapper;
using DIT.IntegrationService.Domain;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DIT.IntegrationService.Data
{
    public interface IStockItemRepository
    {
        Result<IEnumerable<StockItem>> GetAllItems();
    }

    public class StockItemRepository: IStockItemRepository
    {
        private readonly SqlConnectionFactory _factory;

        public StockItemRepository(string connection) =>
            _factory = new SqlConnectionFactory(connection);


        public Result<IEnumerable<StockItem>> GetAllItems()
        {
            IEnumerable<StockItem> items = null;

            string query = @"SELECT StockCode
                                  , RetailPriceInc
                                  , ResellerPriceInc
                               FROM StockItems
                           ORDER BY StockCode";

            using (var con = _factory.CreateOpenConnection())
            {
                try
                {
                    items = con.Query<StockItem>(query);
                    Log.Debug("{StockItemCount} Stock Item retreived.", items.Count());
                }
                catch (Exception ex)
                {
                    Log.Error("Stock Item retreive failed. {ErrorMesg}", ex.Message);
                    return Result.Failure<IEnumerable<StockItem>>(ex.Message);
                }
            }

            return Result.Ok(items);
        }
    }
}
