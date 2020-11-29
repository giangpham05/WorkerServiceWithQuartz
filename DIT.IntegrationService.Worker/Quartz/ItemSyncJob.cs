using CSharpFunctionalExtensions;
using DIT.IntegrationService.Data;
using DIT.IntegrationService.Domain;
using DIT.IntegrationService.Domain.Crm;
using DIT.IntegrationService.Domain.OData;
using DIT.IntegrationService.Worker.Helpers;
using Microsoft.Extensions.Options;
using Quartz;
using Serilog;
using System;
using System.Threading.Tasks;

namespace DIT.IntegrationService.Worker.Quartz
{
    public class ItemSyncJob
    {
        private readonly IStockItemRepository _stockRepository;
        private readonly ICrmRepository _crmRepository;
        private readonly WorkerOptions _options;

        public ItemSyncJob(IStockItemRepository stockRepository, ICrmRepository crmRepository,
            IOptions<WorkerOptions> options)
        {
            _crmRepository = crmRepository ?? throw new ArgumentNullException(nameof(crmRepository));
            _stockRepository = stockRepository ?? throw new ArgumentNullException(nameof(stockRepository));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        }

        public Task Execute(IJobExecutionContext context)
        {
             return Task.Run(() => SyncStockItems());
        }

        private async Task SyncStockItems()
        {
            Log.Information("Items sync started on {Started}.", DateTime.Now);

            var itemsResult = _stockRepository.GetAllItems();
            if (itemsResult.IsSuccess)
            {
                foreach (StockItem item in itemsResult.Value)
                {
                    Log.Debug("Stock Item No is {StockItemNo}", item.StockCode);
                    await _crmRepository.GetWithFetchXmlAsync<Product>(
                       PluralNameConstants.Products, FetchXmlHelper.GetProductBy(item.StockCode))
                       .Tap(product => UpsertProduct(product, item)
                       .Tap(productId => UpsertPriceLevel(productId, item))
                       .Tap(productId => SetActiveProductStatus(productId)));
                }
            }
        }

        private async Task<Result<Guid>> UpsertProduct(Product product, StockItem stockItem)
        {
            Result<Guid> upsertResult = Result.Ok(product.Id);
            if (product == null)
            {
                product = new Product
                {
                    Id = Guid.NewGuid(),
                    ProductNumber = stockItem.StockCode,
                    Name = stockItem.StockCode,
                    CurrentCost = stockItem.RetailPriceInc,
                    DefaultUomScheduleId = _options.SalesOptions.UoMScheduleId,
                    DefaultUomId = _options.SalesOptions.UoMId,
                };
                upsertResult = await _crmRepository.CreateAsync<Product>(PluralNameConstants.Products, product).
                    Tap(id => Log.Debug("Product(ID: {@ProductID}) is created on {CreatedOn}.", product.ProductId, DateTime.Now))
                    .OnFailure(error => Log.Error("Failed to push Stock Item(StockCode: {@StockCode}) to D365. Error: {@Error}", product.ProductNumber, error));
            }
            else
            {
                upsertResult = await _crmRepository.UpdateAsync<Product>(
                        PluralNameConstants.Products, product, product.Id)
                        .Tap(id => Log.Debug("Product(ID: {@ProductID}) is updated at {@ModifiedOn}.", product.ProductId, DateTime.Now))
                        .OnFailure(error => Log.Error("Failed to update Product(ID: {@ProductID}). Error {Error}", product.Id, error));
            }

            return upsertResult;
        }

        private async Task<Result<Guid>> UpsertPriceLevel(Guid productId, StockItem stockItem)
        {
            return await _crmRepository.GetWithFetchXmlAsync<ProductPriceLevel>(
                PluralNameConstants.ProductPriceLevels, FetchXmlHelper.FindProductPriceLevelBy(
                    productId, _options.SalesOptions.DefaultPriceList))
                .Bind(priceLevel =>
                {
                    Task<Result<Guid>> upsertResult;
                    if (priceLevel == null)
                    {
                        priceLevel = new ProductPriceLevel
                        {
                            Id = Guid.NewGuid(),
                            DefaultPriceList = _options.SalesOptions.DefaultPriceList,
                            ProductId = productId,
                            DefaultUomId = _options.SalesOptions.UoMId,
                            Amount = stockItem.ResellerPriceInc
                        };
                        upsertResult = _crmRepository.CreateAsync(PluralNameConstants.ProductPriceLevels, priceLevel)
                        .Tap(id => Log.Debug("Product({@ProductId}) Price was created on {@CreatedOn}.", productId, DateTime.Now));
                    }
                    else
                    {
                        if (priceLevel.Amount == stockItem.ResellerPriceInc)
                            return Task.FromResult(
                                Result.Ok(priceLevel.Id)
                                .Tap(id => Log.Information("Product Price for Product({@ProductPriceId}) was unchanged.", id)));

                        priceLevel.Amount = Math.Round(stockItem.ResellerPriceInc, 2);
                        upsertResult = _crmRepository.UpdateAsync(PluralNameConstants.ProductPriceLevels, priceLevel, priceLevel.Id)
                        .Tap(id => Log.Debug("Product({@ProductId}) Price was updated on {@ModifiedOn}.", productId, DateTime.Now));
                    }
                    return upsertResult;
                });
        }

        private async Task<Result<Guid>> SetActiveProductStatus(Guid productId)
        {
            return await _crmRepository.UpdateAsync<Product>(
                PluralNameConstants.Products, new Product
                {
                    Id = productId,
                    StateCode = 0,
                    StatusCode = 1
                }, productId)
                .Tap(id => Log.Debug("Product({@ProductId}) State was updated to Active on {@ModifiedOn}.", id, DateTime.Now))
                .OnFailure(error => Log.Error("Failed to update product to Active state. Error: {@Error}.", error));
        }
    }
}
