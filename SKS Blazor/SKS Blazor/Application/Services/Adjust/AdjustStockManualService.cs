using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SKS_Blazor.Application.DTOs.Adjust;
using SKS_Blazor.Application.DTOs.Core;
using SKS_Blazor.Domain.Entities.Core;
using SKS_Blazor.Domain.Entities.Manual;
using SKS_Blazor.Domain.Entities.Stock;
using SKS_Blazor.Infrastructure.Data;
using SKS_Blazor.Infrastructure.Helpers;

namespace SKS_Blazor.Application.Services.Adjust;

/// <summary>Port of <c>frmAdjustStockManual</c> — manual stock adjustment service.</summary>
public class AdjustStockManualService : IAdjustStockManualService
{
    private readonly IDbContextFactory<UpgradedDbContext> _dbFactory;
    private readonly ILogger<AdjustStockManualService> _logger;

    public AdjustStockManualService(
        IDbContextFactory<UpgradedDbContext> dbFactory,
        ILogger<AdjustStockManualService> logger)
    {
        _dbFactory = dbFactory;
        _logger = logger;
    }

    /// <inheritdoc />
    /// <remarks>Port of <c>DoSearchProduct()</c> — filters Products by ProductID LIKE and/or ProductName LIKE.</remarks>
    public async Task<List<ProductsDto>> DoSearchProductAsync(AdjustStockManualStateDto state, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        // Build query with optional filters matching legacy:
        // filter = "ProductId LIKE '%txtCode%'" AND "ProductName LIKE '%txtName%'"
        IQueryable<SKS_Blazor.Domain.Entities.Core.Products> query = db.Products.AsNoTracking();

        string codeFilter = state.TxtCodeText ?? string.Empty;
        string nameFilter = state.TxtNameText ?? string.Empty;

        if (!string.IsNullOrEmpty(codeFilter))
        {
            query = query.Where(p => p.ProductID != null && EF.Functions.Like(p.ProductID, "%" + codeFilter + "%"));
        }

        if (!string.IsNullOrEmpty(nameFilter))
        {
            query = query.Where(p => p.ProductName != null && EF.Functions.Like(p.ProductName, "%" + nameFilter + "%"));
        }

        // Legacy selects: ProductID, ProductName, UnitPrice, UnitsInStock, UnitsOnOrder, QuantityPerUnit, Unit
        var entities = await query
            .Select(p => new
            {
                p.ProductID,
                p.ProductName,
                p.UnitPrice,
                p.UnitsInStock,
                p.UnitsOnOrder,
                p.QuantityPerUnit,
                p.Unit,
                p.CategoryID,
                p.Discontinued,
                p.LeadTime,
                p.ProductDescription,
                p.ReorderLevel,
                p.SerialNumber,
                p.SupplierID
            })
            .ToListAsync(ct);

        // Map to DTOs in-memory — coalesce nullable entity properties
        var result = entities.Select(p => new ProductsDto
        {
            ProductID = p.ProductID ?? string.Empty,
            ProductName = p.ProductName ?? string.Empty,
            UnitPrice = p.UnitPrice,
            UnitsInStock = p.UnitsInStock,
            UnitsOnOrder = p.UnitsOnOrder,
            QuantityPerUnit = p.QuantityPerUnit,
            Unit = p.Unit ?? string.Empty,
            CategoryID = p.CategoryID,
            Discontinued = p.Discontinued,
            LeadTime = p.LeadTime ?? string.Empty,
            ProductDescription = p.ProductDescription ?? string.Empty,
            ReorderLevel = p.ReorderLevel,
            SerialNumber = p.SerialNumber ?? string.Empty,
            SupplierID = p.SupplierID
        }).ToList();

        if (result.Count == 0)
        {
            _logger.LogInformation("DoSearchProductAsync: no records with the selected criteria (code={Code}, name={Name})", codeFilter, nameFilter);
        }

        return result;
    }

    /// <inheritdoc />
    /// <remarks>Port of <c>DoSearchStocks()</c> — loads Stocks rows for a given ProductID.</remarks>
    public async Task<List<StocksDto>> DoSearchStocksAsync(AdjustStockManualStateDto state, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(state.CurrentIdProduct))
        {
            _logger.LogWarning("DoSearchStocksAsync called with empty CurrentIdProduct");
            return new List<StocksDto>();
        }

        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        string productId = state.CurrentIdProduct;

        // Legacy: Select StockID, Stock, InitialStock, UnitPrice, StockPrice, DateStarted, DateModified, User
        //         From Stocks Where ProductId = '{productId}'
        var entities = await db.Stocks
            .AsNoTracking()
            .Where(s => s.ProductID == productId)
            .ToListAsync(ct);

        // Map to DTOs in-memory
        var result = entities.Select(s => new StocksDto
        {
            StockID = s.StockID,
            Stock = s.Stock,
            InitialStock = s.InitialStock,
            UnitPrice = s.UnitPrice,
            StockPrice = s.StockPrice,
            DateStarted = s.DateStarted ?? string.Empty,
            DateModified = s.DateModified ?? string.Empty,
            User = s.User ?? string.Empty,
            ProductID = s.ProductID ?? string.Empty
        }).ToList();

        if (result.Count == 0)
        {
            _logger.LogInformation("DoSearchStocksAsync: no stock records for product {ProductId}", productId);
        }

        return result;
    }

    /// <inheritdoc />
    /// <remarks>
    /// Port of <c>cmdSave_Click()</c>.
    /// Within a transaction:
    ///   1. Validates that delta != 0 (no-op guard)
    ///   2. Updates Stocks row (StockPrice, Stock)
    ///   3. Inserts ManualStocks log row (action = "MOD")
    ///   4. Inserts StockLog row (docType = "MANUAL", docID = new ManualStocks ID)
    ///   5. Updates Products.UnitsInStock += deltaStock
    /// </remarks>
    public async Task<AdjustStockManualStateDto> SaveAsync(AdjustStockManualStateDto state, object eventSender, CancellationToken ct = default)
    {
        // Mark editing done
        state.EditingData = false;

        // Parse changed values from state
        decimal changedStockPrice = state.ChangedStockPrice;
        decimal changedStock = state.ChangedStock;
        decimal currentStockPrice = state.CurrentStockPrice;
        decimal currentStock = state.CurrentStock;
        int currentIdStock = state.CurrentIdStock;
        string currentIdProduct = state.CurrentIdProduct ?? string.Empty;

        // PRE-SAVE VALIDATION: compute deltas and reject no-op
        decimal deltaStockPrice = changedStockPrice - currentStockPrice;
        decimal deltaStock = changedStock - currentStock;

        if (deltaStockPrice == 0m && deltaStock == 0m)
        {
            _logger.LogInformation("SaveAsync: no modification of the Stock — data will not be saved");
            // State remains unchanged, caller should display message
            return state;
        }

        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        // TRANSACTION boundary — port of legacy multi-statement save
        await using var transaction = await db.Database.BeginTransactionAsync(ct);

        int retries = 0;
        retry:
        try
        {
            // 1. UPDATE Stocks — set StockPrice and Stock where StockId matches
            var stockEntity = await db.Stocks.FirstOrDefaultAsync(s => s.StockID == currentIdStock, ct);
            if (stockEntity == null)
            {
                await transaction.RollbackAsync(ct);
                throw new InvalidOperationException($"Stock record with StockID={currentIdStock} not found.");
            }

            stockEntity.StockPrice = changedStockPrice;
            stockEntity.Stock = changedStock;
            stockEntity.DateModified = LegacyDate.Today();

            // 2. INSERT ManualStocks — log the adjustment (action = "MOD")
            var manualStockEntry = new ManualStocks
            {
                StockID = currentIdStock,
                Quantity = deltaStock,
                Price = deltaStockPrice,
                User = eventSender as string ?? string.Empty,
                Date = LegacyDate.Today(),
                Action = "MOD"
            };
            db.ManualStocks.Add(manualStockEntry);

            // SaveChanges to get the generated ManualID (identity column)
            await db.SaveChangesAsync(ct);
            int newManualLogId = manualStockEntry.ManualID;

            // 3. INSERT StockLog — audit trail (docType = "MANUAL", docID = new ManualStocks ID)
            var stockLogEntry = new StockLog
            {
                User = eventSender as string ?? string.Empty,
                Date = LegacyDate.Today(),
                Quantity = deltaStock,
                StockPrice = deltaStockPrice,
                ProductID = currentIdProduct,
                StockID = currentIdStock,
                DocType = "MANUAL",
                DocID = newManualLogId
            };
            db.StockLogs.Add(stockLogEntry);

            // 4. UPDATE Products.UnitsInStock += deltaStock
            var productEntity = await db.Products.FirstOrDefaultAsync(
                p => p.ProductID == currentIdProduct, ct);
            if (productEntity == null)
            {
                await transaction.RollbackAsync(ct);
                throw new InvalidOperationException($"Product with ProductID='{currentIdProduct}' not found.");
            }

            int currentUnitsInStock = productEntity.UnitsInStock ?? 0;
            // deltaStock is decimal; UnitsInStock is int — legacy used double addition directly
            productEntity.UnitsInStock = currentUnitsInStock + (int)deltaStock;

            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

            _logger.LogInformation(
                "SaveAsync: Stock adjustment saved — StockID={StockID}, DeltaStock={DeltaStock}, DeltaPrice={DeltaPrice}, Product={ProductID}, ManualLogID={ManualLogID}",
                currentIdStock, deltaStock, deltaStockPrice, currentIdProduct, newManualLogId);

            // Update state to reflect the persisted values
            state.CurrentStockPrice = changedStockPrice;
            state.CurrentStock = changedStock;
            state.TxtOriginalPriceText = changedStockPrice.ToString(CultureInfo.InvariantCulture);
            state.TxtOriginalQuantityText = changedStock.ToString(CultureInfo.InvariantCulture);

            return state;
        }
        catch (DbUpdateConcurrencyException ex) when (retries < 3)
        {
            // LOCK RETRY — port of legacy VerificaErroLock pattern
            retries++;
            _logger.LogWarning(ex,
                "SaveAsync: concurrency conflict on attempt {Retry}/3 for StockID={StockID}. Retrying...",
                retries, currentIdStock);

            // Reload tracked entities for retry
            foreach (var entry in db.ChangeTracker.Entries())
            {
                await entry.ReloadAsync(ct);
            }

            await Task.Delay(100 * retries, ct);
            goto retry;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            await transaction.RollbackAsync(ct);
            _logger.LogError(ex, "SaveAsync: concurrency conflict persisted after 3 retries for StockID={StockID}", currentIdStock);
            throw;
        }
        catch (DbUpdateException ex)
        {
            await transaction.RollbackAsync(ct);
            _logger.LogError(ex, "SaveAsync: database error saving stock adjustment for StockID={StockID}", currentIdStock);
            throw;
        }
        catch (InvalidOperationException)
        {
            // Already rolled back above — rethrow for caller to handle
            throw;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(ct);
            _logger.LogError(ex, "SaveAsync: unexpected error saving stock adjustment for StockID={StockID}", currentIdStock);
            throw;
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// Port of <c>txtValues_TextChanged()</c>.
    /// Recalculates display labels when the user changes stock price or stock quantity.
    /// The <paramref name="eventSender"/> distinguishes which field changed:
    ///   - "0" or "price" → stock price changed
    ///   - "1" or "stock" → stock quantity changed (enforces changedStock &lt;= currentStock)
    /// </remarks>
    public async Task<AdjustStockManualStateDto> ValuesAsync(AdjustStockManualStateDto state, object eventSender, CancellationToken ct = default)
    {
        if (state.CodeGeneratedChange)
        {
            // Skip programmatic changes — only respond to user edits
            return state;
        }

        state.EditingData = true;
        state.CodeGeneratedChange = true;

        try
        {
            decimal changedStockPrice = state.ChangedStockPrice;
            decimal changedStock = state.ChangedStock;
            decimal currentStock = state.CurrentStock;

            // Parse QuantityPerUnit for display calculation — legacy: Double.Parse(currentQuantityPerUnit)
            decimal quantityPerUnit = 1m;
            if (!string.IsNullOrEmpty(state.TxtQuantityPerUnitText))
            {
                if (decimal.TryParse(state.TxtQuantityPerUnitText, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal parsedQpu))
                {
                    quantityPerUnit = parsedQpu;
                }
            }

            string unit = state.TxtUnitText ?? string.Empty;

            // Determine which field changed — eventSender carries the index as string
            string senderIndex = eventSender?.ToString() ?? string.Empty;

            if (senderIndex == "1" || string.Equals(senderIndex, "stock", StringComparison.OrdinalIgnoreCase))
            {
                // Legacy: case 1 — stock quantity field
                // Enforce: changedStock cannot exceed currentStock
                if (changedStock > currentStock)
                {
                    changedStock = currentStock;
                    state.ChangedStock = changedStock;
                    _logger.LogInformation(
                        "ValuesAsync: Cannot pass the original stock — clamped to {CurrentStock}. To add more, add a new stock manually.",
                        currentStock);
                }
            }

            // Recalculate display labels
            // Legacy: lblNewQuantity.Text = Format(changedStock * quantityPerUnit, "##,###.00") & unit
            decimal newQtyDisplay = changedStock * quantityPerUnit;
            state.LblNewQuantityText = string.Format(CultureInfo.InvariantCulture, "{0:#,##0.00}{1}", newQtyDisplay, unit);

            // Legacy: lblCurrentQuantity.Text = Format(currentStock * quantityPerUnit, "##,###.00") & unit
            decimal currentQtyDisplay = currentStock * quantityPerUnit;
            state.LblCurrentQuantityText = string.Format(CultureInfo.InvariantCulture, "{0:#,##0.00}{1}", currentQtyDisplay, unit);
        }
        finally
        {
            state.CodeGeneratedChange = false;
        }

        return state;
    }
}
