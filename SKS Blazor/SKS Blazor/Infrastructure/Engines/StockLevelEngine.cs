using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SKS_Blazor.Infrastructure.Data;
using SKS_Blazor.Domain.Entities.Core;
using SKS_Blazor.Domain.Entities.Manual;
using SKS_Blazor.Domain.Entities.Stock;
using SKS_Blazor.Infrastructure.Helpers;

namespace SKS_Blazor.Infrastructure.Engines;

/// <summary>
/// Encapsulates business rules for computing stock adjustments, reorder thresholds,
/// and manual stock corrections. Port of the inline stock calculation logic from
/// <c>frmAddStockManual.cmdSave_Click</c>, <c>frmAdjustStockManual.cmdSave_Click</c>,
/// and the stock update SQL in <c>frmActionOrderReception.cmdApprove_Click</c>.
/// </summary>
public class StockLevelEngine
{
    private readonly IDbContextFactory<UpgradedDbContext> _dbFactory;
    private readonly ILogger<StockLevelEngine> _logger;

    public StockLevelEngine(
        IDbContextFactory<UpgradedDbContext> dbFactory,
        ILogger<StockLevelEngine> logger)
    {
        _dbFactory = dbFactory;
        _logger = logger;
    }

    /// <summary>
    /// Calculates the total available stock for a product across all stock records.
    /// Port of the legacy pattern: <c>SELECT SUM(Stock) FROM Stocks WHERE ProductId = @productId</c>.
    /// The <paramref name="warehouseId"/> parameter is reserved for future multi-warehouse support;
    /// the legacy app did not partition stock by warehouse.
    /// </summary>
    /// <param name="productId">The product identifier (matches Products.ProductID parsed to int context).</param>
    /// <param name="warehouseId">Reserved for future warehouse partitioning. Currently ignored.</param>
    /// <returns>The sum of all stock quantities for the given product, or 0 if none exist.</returns>
    public async Task<decimal> CalculateAvailableStockAsync(int productId, int warehouseId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var productIdStr = productId.ToString();

        var totalStock = await db.Stocks
            .AsNoTracking()
            .Where(s => s.ProductID == productIdStr)
            .SumAsync(s => s.Stock ?? 0m);

        _logger.LogDebug(
            "CalculateAvailableStock: ProductId={ProductId}, WarehouseId={WarehouseId}, Total={Total}",
            productId, warehouseId, totalStock);

        return totalStock;
    }

    /// <summary>
    /// Applies a manual stock adjustment (add or modify) for a product.
    /// Port of the legacy pattern from <c>frmAdjustStockManual.cmdSave_Click</c>:
    ///   1. Update the Stocks record with new StockPrice and Stock values.
    ///   2. Insert a ManualStocks log entry with action "MOD".
    ///   3. Insert a StockLog entry with DocType "MANUAL".
    ///   4. Update Products.UnitsInStock with the delta.
    /// All within a transaction to preserve data integrity.
    /// </summary>
    /// <param name="productId">The product identifier.</param>
    /// <param name="warehouseId">Reserved — currently ignored.</param>
    /// <param name="delta">The stock quantity change (positive = add, negative = reduce).</param>
    /// <param name="reason">The reason for the adjustment, stored in the log.</param>
    public async Task ApplyManualAdjustmentAsync(int productId, int warehouseId, decimal delta, string reason)
    {
        if (delta == 0m)
        {
            _logger.LogInformation(
                "ApplyManualAdjustment skipped: delta is zero for ProductId={ProductId}", productId);
            return;
        }

        await using var db = await _dbFactory.CreateDbContextAsync();
        await using var transaction = await db.Database.BeginTransactionAsync();

        try
        {
            var productIdStr = productId.ToString();

            // Find the most recent stock record for this product
            var stockRecord = await db.Stocks
                .Where(s => s.ProductID == productIdStr)
                .OrderByDescending(s => s.StockID)
                .FirstOrDefaultAsync();

            if (stockRecord is null)
            {
                _logger.LogWarning(
                    "ApplyManualAdjustment: No stock record found for ProductId={ProductId}", productId);
                await transaction.RollbackAsync();
                return;
            }

            var previousStock = stockRecord.Stock ?? 0m;
            var previousStockPrice = stockRecord.StockPrice ?? 0m;

            stockRecord.Stock = previousStock + delta;
            stockRecord.DateModified = LegacyDate.Today();

            db.Stocks.Update(stockRecord);
            await db.SaveChangesAsync();

            // Insert ManualStocks log
            var manualEntry = new Domain.Entities.Manual.ManualStocks
            {
                StockID = stockRecord.StockID,
                Quantity = delta,
                Price = 0m, // delta price not computed in this path
                User = reason,
                Date = LegacyDate.Today(),
                Action = delta > 0 ? "ADD" : "MOD"
            };
            db.ManualStocks.Add(manualEntry);
            await db.SaveChangesAsync();

            // Insert StockLog entry
            var logEntry = new Domain.Entities.Stock.StockLog
            {
                User = reason,
                Date = LegacyDate.Today(),
                Quantity = delta,
                StockPrice = 0m,
                ProductID = productIdStr,
                StockID = stockRecord.StockID,
                DocType = "MANUAL",
                DocID = manualEntry.ManualID
            };
            db.StockLogs.Add(logEntry);
            await db.SaveChangesAsync();

            // Update Products.UnitsInStock
            var product = await db.Products.FirstOrDefaultAsync(p => p.ProductID == productIdStr);
            if (product is not null)
            {
                product.UnitsInStock = (product.UnitsInStock ?? 0) + (int)delta;
                db.Products.Update(product);
                await db.SaveChangesAsync();
            }

            await transaction.CommitAsync();

            _logger.LogInformation(
                "ApplyManualAdjustment: ProductId={ProductId}, Delta={Delta}, Reason={Reason}",
                productId, delta, reason);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "ApplyManualAdjustment failed for ProductId={ProductId}, Delta={Delta}",
                productId, delta);
            await transaction.RollbackAsync();
            throw;
        }
    }
}
