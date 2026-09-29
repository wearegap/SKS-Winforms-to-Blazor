using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SKS_Blazor.Application.DTOs.Action;
using SKS_Blazor.Infrastructure.Data;
using SKS_Blazor.Domain.Entities.Order;
using SKS_Blazor.Domain.Entities.Core;
using SKS_Blazor.Domain.Entities.Stock;
using SKS_Blazor.Infrastructure.Helpers;

namespace SKS_Blazor.Application.Services.Action;

/// <summary>Port of <c>frmActionOrderReception</c> — order reception approval/cancellation workflow.</summary>
public class ActionOrderReceptionService : IActionOrderReceptionService
{
    private readonly IDbContextFactory<UpgradedDbContext> _dbFactory;
    private readonly ILogger<ActionOrderReceptionService> _logger;

    public ActionOrderReceptionService(
        IDbContextFactory<UpgradedDbContext> dbFactory,
        ILogger<ActionOrderReceptionService> logger)
    {
        _dbFactory = dbFactory;
        _logger = logger;
    }

    /// <inheritdoc />
    /// <remarks>Port of <c>frmActionOrderReception.LoadData</c> + <c>LoadDetails</c> + <c>DisplayTotals</c>.</remarks>
    public async Task<ActionOrderReceptionStateDto> LoadDataAsync(ActionOrderReceptionStateDto state, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        int orderId = state.OrderID;

        // --- Header query: port of LoadData's JOIN ---
        // Legacy: Select o.OrderDate, u.Fullname, o.Status, p.ProviderName,
        //   p.ContactFirstName + ' ' + p.ContactLastName as Contact,
        //   o.ChangedDate, o.ChangedBy, o.FreightCharge, o.SalesTaxRate, o.Notes
        //   From OrderReceptions as o, Users as u, Providers as p
        //   Where o.OrderID = {OrderId} And u.Username = o.ReceivedBy And p.ProviderId = o.ProviderId
        var headerRow = await (from o in db.OrderReceptions.AsNoTracking()
                               join u in db.Users.AsNoTracking() on o.ReceivedBy equals u.Username
                               join p in db.Providers.AsNoTracking() on o.ProviderID equals (int?)p.ProviderID
                               where o.OrderID == orderId
                               select new
                               {
                                   o.OrderDate,
                                   u.Fullname,
                                   o.Status,
                                   p.ProviderName,
                                   Contact = (p.ContactFirstName ?? string.Empty) + " " + (p.ContactLastName ?? string.Empty),
                                   o.ChangedDate,
                                   o.ChangedBy,
                                   o.FreightCharge,
                                   o.SalesTaxRate,
                                   o.Notes
                               }).FirstOrDefaultAsync(ct);

        if (headerRow is null)
        {
            state.OrderFound = false;
            state.ErrorMessage = $"The order with the ID '{orderId}' does not exist";
            _logger.LogWarning("Order reception {OrderId} not found", orderId);
            return state;
        }

        state.OrderFound = true;
        state.OrderDate = headerRow.OrderDate ?? string.Empty;
        state.ReceivedByFullname = headerRow.Fullname ?? string.Empty;
        state.Notes = headerRow.Notes ?? string.Empty;
        state.FreightCharge = headerRow.FreightCharge ?? 0m;
        state.SalesTaxRate = headerRow.SalesTaxRate ?? 0m;
        state.ProviderCompany = headerRow.ProviderName ?? string.Empty;
        state.ProviderContact = (headerRow.Contact ?? string.Empty).Trim();
        state.TxtStatusText = headerRow.Status ?? string.Empty;
        state.ChangedDate = headerRow.ChangedDate ?? string.Empty;
        state.ChangedBy = headerRow.ChangedBy ?? string.Empty;

        // Port of visibility/label logic from LoadData
        bool isReceived = string.Equals(state.TxtStatusText, "RECEIVED", StringComparison.OrdinalIgnoreCase);
        state.IsReceived = isReceived;
        state.ShowChangedFields = !isReceived;
        state.ApproveEnabled = true;
        state.CancelEnabled = true;

        if (string.Equals(state.TxtStatusText, "APPROVED", StringComparison.OrdinalIgnoreCase))
        {
            state.ChangedDateLabel = "Approved Date:";
            state.ChangedByLabel = "Approved By:";
        }
        else
        {
            state.ChangedDateLabel = "Cancelled Date:";
            state.ChangedByLabel = "Cancelled By:";
        }

        // Load details
        await LoadDetailsAsync(state, db, ct);

        // Compute and display totals (port of AddToTotals + DisplayTotals)
        DisplayTotals(state);

        return state;
    }

    /// <inheritdoc />
    /// <remarks>Port of <c>frmActionOrderReception.LoadDetails</c> (public interface entry point).</remarks>
    public async Task LoadDetailsAsync(ActionOrderReceptionStateDto state, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        await LoadDetailsAsync(state, db, ct);
        DisplayTotals(state);
    }

    /// <summary>
    /// Internal helper to load details using a provided DbContext.
    /// Port of <c>frmActionOrderReception.LoadDetails</c>.
    /// Legacy: Select d.Quantity, p.ProductID, p.ProductName, d.UnitPrice, d.SalePrice,
    ///   p.UnitsInStock, p.UnitsOnOrder, p.QuantityPerUnit + p.Unit, d.LineTotal
    ///   From Products as p, OrderReceptionDetails as d
    ///   Where d.OrderID = {OrderId} And d.ProductId = p.ProductId
    /// </summary>
    private async Task LoadDetailsAsync(ActionOrderReceptionStateDto state, UpgradedDbContext db, CancellationToken ct)
    {
        int orderId = state.OrderID;

        var detailRows = await (from d in db.OrderReceptionDetails.AsNoTracking()
                                join p in db.Products.AsNoTracking() on d.ProductID equals p.ProductID
                                where d.OrderID == orderId
                                select new
                                {
                                    d.Quantity,
                                    p.ProductID,
                                    p.ProductName,
                                    d.UnitPrice,
                                    d.SalePrice,
                                    p.UnitsInStock,
                                    p.UnitsOnOrder,
                                    p.QuantityPerUnit,
                                    p.Unit,
                                    d.LineTotal
                                }).ToListAsync(ct);

        // Map to DTOs — port of the row iteration in LoadDetails
        state.Details.Clear();
        state.CurrentSubTotal = 0m;

        foreach (var row in detailRows)
        {
            var qtyPerUnitStr = (row.QuantityPerUnit.HasValue
                ? row.QuantityPerUnit.Value.ToString(CultureInfo.InvariantCulture)
                : string.Empty) + (row.Unit ?? string.Empty);

            var detail = new ActionOrderReceptionDetailDto
            {
                Quantity = row.Quantity ?? 0m,
                ProductID = row.ProductID ?? string.Empty,
                ProductName = row.ProductName ?? string.Empty,
                UnitPrice = row.UnitPrice ?? 0m,
                SalePrice = row.SalePrice ?? 0m,
                UnitsInStock = row.UnitsInStock ?? 0,
                UnitsOnOrder = row.UnitsOnOrder ?? 0,
                QuantityPerUnitDisplay = qtyPerUnitStr,
                LineTotal = row.LineTotal ?? 0m
            };
            state.Details.Add(detail);

            // Port of AddToTotals(Convert.ToDouble(modConnection.rs["LineTotal"]))
            decimal lineTotal = row.LineTotal ?? 0m;
            AddToTotals(state, lineTotal);
        }
    }

    /// <summary>
    /// Port of <c>frmActionOrderReception.AddToTotals</c>.
    /// Legacy: currentSubTotal = currentSubTotal + lineTotal
    /// </summary>
    private static void AddToTotals(ActionOrderReceptionStateDto state, decimal lineTotal)
    {
        state.CurrentSubTotal += lineTotal;
    }

    /// <summary>
    /// Port of <c>frmActionOrderReception.DisplayTotals</c>.
    /// Legacy:
    ///   txtSubTotal.Text = Format(currentSubTotal, "#,##0.00")
    ///   currentTotalTax = currentSubTotal * currentTax
    ///   txtTotalTax.Text = Format(currentTotalTax, "#,##0.00")
    ///   currentTotal = currentFreightCharge + currentSubTotal + currentTotalTax
    ///   txtTotal.Text = Format(currentTotal, "#,##0.00")
    /// </summary>
    private static void DisplayTotals(ActionOrderReceptionStateDto state)
    {
        state.CurrentTax = state.SalesTaxRate;
        state.CurrentFreightCharge = state.FreightCharge;
        state.CurrentTotalTax = state.CurrentSubTotal * state.CurrentTax;
        state.CurrentTotal = state.CurrentFreightCharge + state.CurrentSubTotal + state.CurrentTotalTax;

        state.TxtSubTotalText = state.CurrentSubTotal.ToString("#,##0.00", CultureInfo.InvariantCulture);
        state.TxtTotalTaxText = state.CurrentTotalTax.ToString("#,##0.00", CultureInfo.InvariantCulture);
        state.TxtTotalText = state.CurrentTotal.ToString("#,##0.00", CultureInfo.InvariantCulture);
    }

    /// <inheritdoc />
    /// <remarks>Port of <c>frmActionOrderReception.cmdApprove_Click</c>.</remarks>
    public async Task ApproveAsync(ActionOrderReceptionStateDto state, object eventSender, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        int orderId = state.OrderID;
        string currentUserId = state.CurrentUserId;

        // Pre-save validation: load current order to check status
        var order = await db.OrderReceptions.FirstOrDefaultAsync(o => o.OrderID == orderId, ct);
        if (order is null)
        {
            state.ErrorMessage = $"Order reception {orderId} not found";
            return;
        }

        // Port of: if (txtStatus.Text.ToUpper() == "APPROVED")
        if (string.Equals(order.Status, "APPROVED", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation("Order {OrderId} is already approved, no need to be approved again", orderId);
            state.ErrorMessage = "Order is already approved, not need to be approved again";
            return;
        }

        // Port of: if (txtStatus.Text.ToUpper() == "CANCELLED")
        if (string.Equals(order.Status, "CANCELLED", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation("Order {OrderId} was already cancelled by {ChangedBy} on {ChangedDate}", orderId, order.ChangedBy, order.ChangedDate);
            state.ErrorMessage = $"Order was already cancelled by {order.ChangedBy ?? string.Empty} on {order.ChangedDate ?? string.Empty}, it cannot be approved";
            return;
        }

        // Load all reception details for this order upfront (avoid N+1)
        var receptionDetails = await db.OrderReceptionDetails
            .Where(d => d.OrderID == orderId)
            .AsNoTracking()
            .ToListAsync(ct);

        var todayStr = LegacyDate.Today();

        // Transaction boundary: port of legacy multi-statement update
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        int retries = 0;
        retry:
        try
        {
            // UPDATE OrderReceptions Set Status = 'APPROVED', ChangedBy, ChangedDate
            order.Status = "APPROVED";
            order.ChangedBy = currentUserId;
            order.ChangedDate = todayStr;

            // Port of: Insert Into Stocks ... Select from OrderReceptionDetails
            // AND Insert Into StockLog for each detail
            // Collect all product IDs to batch-update Products at end
            var productQuantities = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

            foreach (var detail in receptionDetails)
            {
                string productId = detail.ProductID ?? string.Empty;
                decimal quantity = detail.Quantity ?? 0m;
                decimal unitPrice = detail.UnitPrice ?? 0m;
                decimal lineTotal = detail.LineTotal ?? 0m;

                // Insert into Stocks
                var newStock = new Domain.Entities.Core.Stocks
                {
                    ProductID = productId,
                    Stock = quantity,
                    InitialStock = quantity,
                    DateStarted = todayStr,
                    DateModified = todayStr,
                    User = currentUserId,
                    UnitPrice = unitPrice,
                    StockPrice = lineTotal
                };
                db.Stocks.Add(newStock);

                // Flush to get the auto-generated StockID for StockLog
                await db.SaveChangesAsync(ct);

                // Insert into StockLog
                var stockLogEntry = new Domain.Entities.Stock.StockLog
                {
                    Date = todayStr,
                    DocID = orderId,
                    DocType = "OrderReception",
                    ProductID = productId,
                    Quantity = quantity,
                    StockID = newStock.StockID,
                    StockPrice = lineTotal,
                    User = currentUserId
                };
                db.StockLogs.Add(stockLogEntry);

                // Accumulate quantities per product for the batch Products update
                if (productQuantities.ContainsKey(productId))
                {
                    productQuantities[productId] += quantity;
                }
                else
                {
                    productQuantities[productId] = quantity;
                }
            }

            // Port of: Update Products Set UnitsInStock = UnitsInStock +
            //   (Select Sum(Quantity) From OrderReceptionDetails Where OrderID = {OrderId} and ProductID = Products.ProductID)
            //   Where ProductId in (Select ProductID From OrderReceptionDetails Where OrderID = {OrderId})
            var affectedProductIds = productQuantities.Keys.ToList();
            var productsToUpdate = await db.Products
                .Where(p => affectedProductIds.Contains(p.ProductID))
                .ToListAsync(ct);

            foreach (var product in productsToUpdate)
            {
                if (productQuantities.TryGetValue(product.ProductID, out decimal totalQty))
                {
                    product.UnitsInStock = (product.UnitsInStock ?? 0) + (int)totalQty;
                }
            }

            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

            _logger.LogInformation("Order reception {OrderId} approved by {UserId}", orderId, currentUserId);

            // Update state to reflect new status
            state.TxtStatusText = "APPROVED";
            state.ChangedBy = currentUserId;
            state.ChangedDate = todayStr;
            state.ChangedDateLabel = "Approved Date:";
            state.ChangedByLabel = "Approved By:";
            state.IsReceived = false;
            state.ShowChangedFields = true;
            state.SuccessMessage = "The order was successfully approved";
            state.ErrorMessage = string.Empty;
        }
        catch (DbUpdateConcurrencyException ex) when (retries++ < 3)
        {
            _logger.LogWarning(ex, "Concurrency conflict approving order {OrderId}, retry {Retry}/3", orderId, retries);

            // Reload entities for retry
            foreach (var entry in db.ChangeTracker.Entries())
            {
                await entry.ReloadAsync(ct);
            }

            await Task.Delay(100 * retries, ct);
            goto retry;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(ct);
            _logger.LogError(ex, "Error approving order reception {OrderId}", orderId);
            throw;
        }
    }

    /// <inheritdoc />
    /// <remarks>Port of <c>frmActionOrderReception.cmdCancel_Click</c>.</remarks>
    public async Task CancelAsync(ActionOrderReceptionStateDto state, object eventSender, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        int orderId = state.OrderID;
        string currentUserId = state.CurrentUserId;

        // Pre-save validation: load current order to check status
        var order = await db.OrderReceptions.FirstOrDefaultAsync(o => o.OrderID == orderId, ct);
        if (order is null)
        {
            state.ErrorMessage = $"Order reception {orderId} not found";
            return;
        }

        // Port of: if (txtStatus.Text.ToUpper() == "CANCELLED")
        if (string.Equals(order.Status, "CANCELLED", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation("Order {OrderId} was already cancelled", orderId);
            state.ErrorMessage = "Order was already cancelled, not need to be cancelled again";
            return;
        }

        // Port of: if (txtStatus.Text.ToUpper() == "APPROVED")
        if (string.Equals(order.Status, "APPROVED", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation("Order {OrderId} was already approved by {ChangedBy} on {ChangedDate}", orderId, order.ChangedBy, order.ChangedDate);
            state.ErrorMessage = $"Order was already approved by {order.ChangedBy ?? string.Empty} on {order.ChangedDate ?? string.Empty}, it cannot be cancelled";
            return;
        }

        // Note: legacy prompts "Do you want to cancel the order reception?" — that confirmation
        // is the caller's (page's) responsibility via dialog before calling this service method.

        var todayStr = LegacyDate.Today();

        // Transaction boundary
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        int retries = 0;
        retry:
        try
        {
            // Port of: Update OrderReceptions Set Status = 'CANCELLED', ChangedBy, ChangedDate
            order.Status = "CANCELLED";
            order.ChangedBy = currentUserId;
            order.ChangedDate = todayStr;

            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

            _logger.LogInformation("Order reception {OrderId} cancelled by {UserId}", orderId, currentUserId);

            // Update state to reflect new status
            state.TxtStatusText = "CANCELLED";
            state.ChangedBy = currentUserId;
            state.ChangedDate = todayStr;
            state.ChangedDateLabel = "Cancelled Date:";
            state.ChangedByLabel = "Cancelled By:";
            state.IsReceived = false;
            state.ShowChangedFields = true;
            state.SuccessMessage = "The order was successfully cancelled";
            state.ErrorMessage = string.Empty;
        }
        catch (DbUpdateConcurrencyException ex) when (retries++ < 3)
        {
            _logger.LogWarning(ex, "Concurrency conflict cancelling order {OrderId}, retry {Retry}/3", orderId, retries);

            foreach (var entry in db.ChangeTracker.Entries())
            {
                await entry.ReloadAsync(ct);
            }

            await Task.Delay(100 * retries, ct);
            goto retry;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(ct);
            _logger.LogError(ex, "Error cancelling order reception {OrderId}", orderId);
            throw;
        }
    }
}
