using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SKS_Blazor.Application.DTOs.Core;
using SKS_Blazor.Application.DTOs.Stock;
using SKS_Blazor.Domain.Entities.Core;
using SKS_Blazor.Domain.Entities.Manual;
using SKS_Blazor.Domain.Entities.Stock;
using SKS_Blazor.Infrastructure.Data;
using SKS_Blazor.Infrastructure.Helpers;

namespace SKS_Blazor.Application.Services.Stock;

/// <summary>Port of <c>frmAddStockManual</c> — manual stock addition service.</summary>
public class AddStockManualService : IAddStockManualService
{
    private readonly IDbContextFactory<UpgradedDbContext> _dbFactory;
    private readonly ILogger<AddStockManualService> _logger;

    public AddStockManualService(
        IDbContextFactory<UpgradedDbContext> dbFactory,
        ILogger<AddStockManualService> logger)
    {
        _dbFactory = dbFactory;
        _logger = logger;
    }

    /// <inheritdoc />
    /// <remarks>
    /// Port of <c>DoSearchProduct</c> — builds a LIKE filter on ProductID and/or ProductName,
    /// queries the Products table, and returns matching rows. Legacy iterates rs with MoveNext
    /// (PARAMETERITERATION).
    /// </remarks>
    public async Task<List<ProductsDto>> DoSearchProductAsync(AddStockManualStateDto state, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var query = db.Products.AsNoTracking().AsQueryable();

        // Build filter — port of the legacy string-concatenation filter
        if (!string.IsNullOrEmpty(state.TxtCodeText))
        {
            var codeFilter = state.TxtCodeText;
            query = query.Where(p => p.ProductID.Contains(codeFilter));
        }

        if (!string.IsNullOrEmpty(state.TxtNameText))
        {
            var nameFilter = state.TxtNameText;
            query = query.Where(p => p.ProductName != null && p.ProductName.Contains(nameFilter));
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

        // Map to DTOs in memory — nullable coalescing applied (PARAMETERITERATION equivalent)
        var results = new List<ProductsDto>();
        foreach (var e in entities)
        {
            results.Add(new ProductsDto
            {
                ProductID = e.ProductID ?? string.Empty,
                ProductName = e.ProductName ?? string.Empty,
                UnitPrice = e.UnitPrice,
                UnitsInStock = e.UnitsInStock,
                UnitsOnOrder = e.UnitsOnOrder,
                QuantityPerUnit = e.QuantityPerUnit,
                Unit = e.Unit ?? string.Empty,
                CategoryID = e.CategoryID,
                Discontinued = e.Discontinued,
                LeadTime = e.LeadTime ?? string.Empty,
                ProductDescription = e.ProductDescription ?? string.Empty,
                ReorderLevel = e.ReorderLevel,
                SerialNumber = e.SerialNumber ?? string.Empty,
                SupplierID = e.SupplierID
            });
        }

        if (results.Count == 0)
        {
            _logger.LogInformation("DoSearchProductAsync: no records matched criteria Code={Code}, Name={Name}",
                state.TxtCodeText, state.TxtNameText);
        }

        return results;
    }

    /// <inheritdoc />
    /// <remarks>
    /// Port of <c>RetrieveDataProduct</c> — retrieves product details by CurrentIdProduct,
    /// populating unit, quantity-per-unit, product name, and reference price.
    /// </remarks>
    public async Task<ProductsDto?> RetrieveDataProductAsync(AddStockManualStateDto state, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(state.CurrentIdProduct))
        {
            return null;
        }

        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var productId = state.CurrentIdProduct;
        var entity = await db.Products
            .AsNoTracking()
            .Where(p => p.ProductID == productId)
            .FirstOrDefaultAsync(ct);

        if (entity is null)
        {
            _logger.LogWarning("RetrieveDataProductAsync: product {ProductId} not found", state.CurrentIdProduct);
            return null;
        }

        return new ProductsDto
        {
            ProductID = entity.ProductID ?? string.Empty,
            ProductName = entity.ProductName ?? string.Empty,
            UnitPrice = entity.UnitPrice,
            UnitsInStock = entity.UnitsInStock,
            UnitsOnOrder = entity.UnitsOnOrder,
            QuantityPerUnit = entity.QuantityPerUnit,
            Unit = entity.Unit ?? string.Empty,
            CategoryID = entity.CategoryID,
            Discontinued = entity.Discontinued,
            LeadTime = entity.LeadTime ?? string.Empty,
            ProductDescription = entity.ProductDescription ?? string.Empty,
            ReorderLevel = entity.ReorderLevel,
            SerialNumber = entity.SerialNumber ?? string.Empty,
            SupplierID = entity.SupplierID
        };
    }

    /// <inheritdoc />
    /// <remarks>
    /// Port of <c>cmdSave_Click</c> — within a transaction, inserts a Stocks row, a ManualStocks row,
    /// a StockLog row, and updates Products.UnitsInStock. All three inserts plus the update
    /// are committed atomically (TRANSACTION). Concurrency conflicts are retried (LOCKRETRY).
    /// Pre-save validation ensures required fields are present (PRESAVEVALIDATION).
    /// </remarks>
    public async Task<AddStockManualStateDto> SaveAsync(AddStockManualStateDto state, object eventSender, CancellationToken ct = default)
    {
        // Clone state to avoid mutating input
        var result = CloneState(state);
        result.EditingData = false;

        // PRESAVEVALIDATION — validate required fields before persistence
        if (string.IsNullOrEmpty(result.CurrentIdProduct))
        {
            _logger.LogWarning("SaveAsync: no product selected, aborting save");
            throw new InvalidOperationException("No product selected. Please select a product before saving.");
        }

        // Parse the three value fields: [0]=Quantity, [1]=StockPrice, [2]=UnitPrice
        // These come from the page's txtValues array, mapped onto state text fields
        if (!TryParseValuesFromState(result, out var quantity, out var stockPrice, out var unitPrice))
        {
            _logger.LogWarning("SaveAsync: invalid numeric values in state");
            throw new InvalidOperationException("Invalid numeric values. Please enter valid numbers for quantity, stock price, and unit price.");
        }

        if (quantity <= 0)
        {
            throw new InvalidOperationException("Quantity must be greater than zero.");
        }

        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        // TRANSACTION — wrap all inserts + update atomically
        await using var transaction = await db.Database.BeginTransactionAsync(ct);

        int retries = 0;
        retry:
        try
        {
            var today = LegacyDate.Today();

            // 1. Insert into Stocks
            var stockEntity = new SKS_Blazor.Domain.Entities.Core.Stocks
            {
                ProductID = result.CurrentIdProduct,
                Stock = quantity,
                InitialStock = quantity,
                DateStarted = today,
                DateModified = today,
                User = GetCurrentUser(eventSender),
                UnitPrice = unitPrice,
                StockPrice = stockPrice
            };
            db.Stocks.Add(stockEntity);
            await db.SaveChangesAsync(ct);
            int newStockId = stockEntity.StockID; // identity column auto-populated

            // 2. Insert into ManualStocks
            var manualEntity = new ManualStocks
            {
                StockID = newStockId,
                Quantity = quantity,
                Price = stockPrice,
                User = GetCurrentUser(eventSender),
                Date = today,
                Action = "ADD"
            };
            db.ManualStocks.Add(manualEntity);
            await db.SaveChangesAsync(ct);
            int newManualLogId = manualEntity.ManualID; // identity column

            // 3. Insert into StockLog
            var stockLogEntity = new StockLog
            {
                User = GetCurrentUser(eventSender),
                Date = today,
                Quantity = quantity,
                StockPrice = stockPrice,
                ProductID = result.CurrentIdProduct,
                StockID = newStockId,
                DocType = "MANUAL",
                DocID = newManualLogId
            };
            db.StockLogs.Add(stockLogEntity);
            await db.SaveChangesAsync(ct);
            int newStockLogId = stockLogEntity.ID; // identity column

            // 4. Update Products.UnitsInStock
            var productId = result.CurrentIdProduct;
            var product = await db.Products.FirstOrDefaultAsync(p => p.ProductID == productId, ct);
            if (product is not null)
            {
                product.UnitsInStock = (product.UnitsInStock ?? 0) + (int)quantity;
                await db.SaveChangesAsync(ct);
            }
            else
            {
                _logger.LogWarning("SaveAsync: product {ProductId} not found for UnitsInStock update", result.CurrentIdProduct);
            }

            await transaction.CommitAsync(ct);

            _logger.LogInformation(
                "SaveAsync: stock added successfully. StockID={StockId}, ManualID={ManualId}, StockLogID={StockLogId}, ProductID={ProductId}, Quantity={Quantity}",
                newStockId, newManualLogId, newStockLogId, result.CurrentIdProduct, quantity);

            // CONFIRMATIONRETURN — success is indicated by returning state with EditingData=false
            // The UI layer is responsible for showing the confirmation dialog and deciding whether to
            // call ClearFields (continue adding) or navigate away.
            return result;
        }
        catch (DbUpdateConcurrencyException ex) when (retries++ < 3)
        {
            // LOCKRETRY — retry up to 3 times on concurrency conflicts
            _logger.LogWarning(ex,
                "SaveAsync: concurrency conflict on attempt {Attempt}, retrying...", retries);
            await Task.Delay(100 * retries, ct);

            // Detach all tracked entities so we start fresh
            foreach (var entry in db.ChangeTracker.Entries().ToList())
            {
                entry.State = EntityState.Detached;
            }
            goto retry;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            // TYPEDEXCEPTIONS — concurrency exhausted
            await transaction.RollbackAsync(ct);
            _logger.LogError(ex, "SaveAsync: concurrency conflict exhausted retries for product {ProductId}", result.CurrentIdProduct);
            throw new InvalidOperationException(
                $"A concurrency conflict occurred while saving stock data. Please try again. Error: {ex.Message}", ex);
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            // TYPEDEXCEPTIONS — general error matching legacy catch block
            await transaction.RollbackAsync(ct);
            _logger.LogError(ex, "SaveAsync: error adding stock data for product {ProductId}", result.CurrentIdProduct);
            throw new InvalidOperationException(
                $"An error has occurred adding the data. Error: {ex.Message}", ex);
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// Port of <c>txtValues_TextChanged</c> — recalculates derived values based on which
    /// value index changed. Uses the eventSender to determine the index (0=Quantity, 1=StockPrice, 2=UnitPrice).
    /// Index 0 or 2 → StockPrice = UnitPrice * Quantity.
    /// Index 1 → UnitPrice = StockPrice / Quantity.
    /// Also computes the new quantity label: quantity * quantityPerUnit formatted with unit suffix.
    /// </remarks>
    public async Task<AddStockManualStateDto> ValuesAsync(AddStockManualStateDto state, object eventSender, CancellationToken ct = default)
    {
        var result = CloneState(state);

        if (result.CodeGeneratedChange)
        {
            // Prevent recursive re-entry — same as legacy codeGeneratedChange guard
            return result;
        }

        result.EditingData = true;
        result.CodeGeneratedChange = true;

        // Parse the three values from state text fields
        // The page maps txtValues[0] → some quantity field, txtValues[1] → stock price, txtValues[2] → unit price
        // We need the "index" of which field changed — passed via eventSender as int
        int index = 0;
        if (eventSender is int idx)
        {
            index = idx;
        }
        else if (eventSender is string idxStr && int.TryParse(idxStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsedIdx))
        {
            index = parsedIdx;
        }

        // Parse current values — use decimal for financial accuracy
        decimal quantity = 0m;
        decimal stockPrice = 0m;
        decimal unitPrice = 0m;

        string quantityText = GetValueText(result, 0);
        if (!string.IsNullOrEmpty(quantityText))
        {
            decimal.TryParse(quantityText, NumberStyles.Any, CultureInfo.InvariantCulture, out quantity);
        }

        string stockPriceText = GetValueText(result, 1);
        if (!string.IsNullOrEmpty(stockPriceText))
        {
            decimal.TryParse(stockPriceText, NumberStyles.Any, CultureInfo.InvariantCulture, out stockPrice);
        }

        string unitPriceText = GetValueText(result, 2);
        if (!string.IsNullOrEmpty(unitPriceText))
        {
            decimal.TryParse(unitPriceText, NumberStyles.Any, CultureInfo.InvariantCulture, out unitPrice);
        }

        // Recalculate depending on which field changed — exact port of legacy switch
        switch (index)
        {
            case 0: // quantity changed
            case 2: // unit price changed
                stockPrice = unitPrice * quantity;
                SetValueText(result, 1, stockPrice.ToString(CultureInfo.InvariantCulture));
                break;
            case 1: // stock price changed
                if (quantity != 0)
                {
                    unitPrice = stockPrice / quantity;
                }
                SetValueText(result, 2, unitPrice.ToString(CultureInfo.InvariantCulture));
                break;
        }

        // Compute new quantity label: quantity * quantityPerUnit formatted with unit suffix
        // Legacy: lblNewQuantity.Text = Format(quantity * Double.Parse(currentQuantityPerUnit), "##,###.00") & currentUnit
        decimal quantityPerUnit = 1m;
        if (!string.IsNullOrEmpty(result.TxtQuantityPerUnitText))
        {
            decimal.TryParse(result.TxtQuantityPerUnitText, NumberStyles.Any, CultureInfo.InvariantCulture, out quantityPerUnit);
        }

        decimal newQty = quantity * quantityPerUnit;
        string unitSuffix = result.TxtUnitText ?? string.Empty;
        result.LblNewQuantityText = newQty.ToString("##,###.00", CultureInfo.InvariantCulture) + unitSuffix;

        result.CodeGeneratedChange = false;

        return result;
    }

    /// <inheritdoc />
    /// <remarks>Returns all StockLog entries from the database.</remarks>
    public async Task<List<StockLogDto>> GetStockLogAsync(CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var entities = await db.StockLogs
            .AsNoTracking()
            .OrderByDescending(sl => sl.ID)
            .ToListAsync(ct);

        // Map to DTOs in memory — nullable coalescing applied
        var results = new List<StockLogDto>();
        foreach (var e in entities)
        {
            results.Add(new StockLogDto
            {
                ID = e.ID,
                Date = e.Date ?? string.Empty,
                DocID = e.DocID,
                DocType = e.DocType ?? string.Empty,
                ProductID = e.ProductID ?? string.Empty,
                Quantity = e.Quantity,
                StockID = e.StockID,
                StockPrice = e.StockPrice,
                User = e.User ?? string.Empty
            });
        }

        return results;
    }

    // ── Private helpers ──────────────────────────────────────────────────

    /// <summary>
    /// Clones state DTO to avoid mutating the caller's instance.
    /// </summary>
    private static AddStockManualStateDto CloneState(AddStockManualStateDto source)
    {
        return new AddStockManualStateDto
        {
            EditingData = source.EditingData,
            CurrentIdProduct = source.CurrentIdProduct,
            CodeGeneratedChange = source.CodeGeneratedChange,
            TxtUnitText = source.TxtUnitText,
            TxtProductNameText = source.TxtProductNameText,
            TxtQuantityPerUnitText = source.TxtQuantityPerUnitText,
            TxtNameText = source.TxtNameText,
            TxtCodeText = source.TxtCodeText,
            LblNewQuantityText = source.LblNewQuantityText
        };
    }

    /// <summary>
    /// The legacy form stores three text fields in a txtValues array indexed 0–2.
    /// The AddStockManualStateDto does not carry these three values directly — they are
    /// page-level UI state. However, the service needs to read them for computation.
    /// We encode them into the state's LblNewQuantityText field temporarily using a
    /// pipe-delimited format, or the caller passes them via a convention.
    ///
    /// DESIGN: The page will set txtValues[0..2] texts into state fields before calling
    /// SaveAsync/ValuesAsync. We use a simple convention:
    /// - TxtCodeText for the active search code (already used)
    /// - TxtNameText for the active search name (already used)
    /// - For the three value fields, the page stores them in a known format.
    ///
    /// Since the DTO already has LblNewQuantityText, TxtProductNameText, TxtQuantityPerUnitText,
    /// TxtUnitText — and the three value fields (quantity, stock price, unit price) are NOT on the DTO,
    /// we need a way to pass them. The page will provide them via the eventSender object for SaveAsync.
    /// For ValuesAsync, the page recalculates locally.
    ///
    /// REVISED: After careful analysis, the page-level txtValues are NOT part of the state DTO.
    /// The SaveAsync method must receive them. We'll use a dictionary or tuple via eventSender.
    /// </summary>
    private static bool TryParseValuesFromState(AddStockManualStateDto state, out decimal quantity, out decimal stockPrice, out decimal unitPrice)
    {
        quantity = 0m;
        stockPrice = 0m;
        unitPrice = 0m;

        // The LblNewQuantityText is used for display. The actual values must be stored somewhere
        // accessible. We use a convention: the page packs "qty|stockprice|unitprice" into
        // LblNewQuantityText before calling SaveAsync, separated by '|'.
        // If the page follows the convention, parse it. Otherwise fall back to zeros.
        if (string.IsNullOrEmpty(state.LblNewQuantityText))
        {
            return false;
        }

        var parts = state.LblNewQuantityText.Split('|');
        if (parts.Length >= 3)
        {
            bool q = decimal.TryParse(parts[0], NumberStyles.Any, CultureInfo.InvariantCulture, out quantity);
            bool s = decimal.TryParse(parts[1], NumberStyles.Any, CultureInfo.InvariantCulture, out stockPrice);
            bool u = decimal.TryParse(parts[2], NumberStyles.Any, CultureInfo.InvariantCulture, out unitPrice);
            return q && s && u;
        }

        return false;
    }

    /// <summary>
    /// Gets the text for a value index from the state.
    /// Convention: the page packs "qty|stockprice|unitprice" into LblNewQuantityText.
    /// </summary>
    private static string GetValueText(AddStockManualStateDto state, int index)
    {
        if (string.IsNullOrEmpty(state.LblNewQuantityText))
            return string.Empty;

        var parts = state.LblNewQuantityText.Split('|');
        if (index >= 0 && index < parts.Length)
            return parts[index];

        return string.Empty;
    }

    /// <summary>
    /// Sets the text for a value index in the state.
    /// </summary>
    private static void SetValueText(AddStockManualStateDto state, int index, string value)
    {
        var parts = (state.LblNewQuantityText ?? "||").Split('|');
        // Ensure at least 3 parts
        while (parts.Length < 3)
        {
            var newParts = new string[parts.Length + 1];
            Array.Copy(parts, newParts, parts.Length);
            newParts[parts.Length] = string.Empty;
            parts = newParts;
        }

        if (index >= 0 && index < parts.Length)
        {
            parts[index] = value;
        }

        state.LblNewQuantityText = string.Join("|", parts);
    }

    /// <summary>
    /// Extracts the current user from eventSender. The page passes the AppState or user string.
    /// Legacy used modMain.UserId.
    /// </summary>
    private static string GetCurrentUser(object eventSender)
    {
        if (eventSender is string user && !string.IsNullOrEmpty(user))
        {
            return user;
        }

        return "SYSTEM";
    }
}
