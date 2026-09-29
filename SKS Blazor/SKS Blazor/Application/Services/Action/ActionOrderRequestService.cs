using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SKS_Blazor.Application.DTOs.Action;
using SKS_Blazor.Domain.Entities.Core;
using SKS_Blazor.Domain.Entities.Order;
using SKS_Blazor.Infrastructure.Data;
using SKS_Blazor.Infrastructure.Helpers;

namespace SKS_Blazor.Application.Services.Action;

/// <summary>Port of <c>frmActionOrderRequest</c> — view/approve/cancel an order request.</summary>
public class ActionOrderRequestService : IActionOrderRequestService
{
    private readonly IDbContextFactory<UpgradedDbContext> _dbFactory;
    private readonly ILogger<ActionOrderRequestService> _logger;

    public ActionOrderRequestService(
        IDbContextFactory<UpgradedDbContext> dbFactory,
        ILogger<ActionOrderRequestService> logger)
    {
        _dbFactory = dbFactory;
        _logger = logger;
    }

    /// <summary>
    /// Port of <c>frmActionOrderRequest.LoadData</c> + <c>DisplayTotals</c> + <c>AddToTotals</c>.
    /// Loads the order request header by joining OrderRequests, Users, Customers,
    /// then loads detail lines by joining OrderRequestDetails with Products,
    /// iterates each detail line accumulating subtotal/tax/total (PARAMETER ITERATION).
    /// </summary>
    public async Task<ActionOrderRequestStateDto> LoadDataAsync(ActionOrderRequestStateDto state, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        // The page sets state.OrderID before calling.
        int orderId = state?.OrderID ?? 0;

        // --- Header query: port of legacy SQL join ---
        // Select o.OrderDate, u.Fullname, o.Status, c.CompanyName,
        //   c.ContactFirstName + ' ' + c.ContactLastName as Contact,
        //   o.ChangedDate, o.ChangedBy, o.FreightCharge, o.SalesTaxRate,
        //   o.RequiredByDate, o.PromisedByDate, o.Notes
        // From OrderRequests o, Users u, Customers c
        // Where o.OrderID = @orderId And u.Username = o.EmployeeId And c.CustomerId = o.CustomerId
        var headerRow = await (
            from o in db.OrderRequests.AsNoTracking()
            join u in db.Users.AsNoTracking() on o.EmployeeID equals u.Username
            join c in db.Customers.AsNoTracking() on o.CustomerID equals (int?)c.CustomerID
            where o.OrderID == orderId
            select new
            {
                o.OrderDate,
                u.Fullname,
                o.Status,
                c.CompanyName,
                ContactFirstName = c.ContactFirstName ?? string.Empty,
                ContactLastName = c.ContactLastName ?? string.Empty,
                o.ChangedDate,
                o.ChangedBy,
                o.FreightCharge,
                o.SalesTaxRate,
                o.RequiredByDate,
                o.PromisedByDate,
                o.Notes
            }
        ).FirstOrDefaultAsync(ct);

        if (headerRow is null)
        {
            _logger.LogWarning("Order request with ID {OrderId} does not exist", orderId);
            state!.TxtStatusText = $"The order with the ID '{orderId}' does not exist";
            return state;
        }

        var status = headerRow.Status ?? string.Empty;

        decimal currentFreightCharge = headerRow.FreightCharge ?? 0m;
        decimal currentTax = headerRow.SalesTaxRate ?? 0m;

        // --- Details query: port of LoadDetails / LoadData detail grid population ---
        // Select d.Quantity, p.ProductID, p.ProductName, d.UnitPrice, d.SalePrice,
        //   p.UnitsInStock, p.UnitsOnOrder, cast(p.QuantityPerUnit as text) + ' ' + p.Unit, d.LineTotal
        // From Products p, OrderRequestDetails d
        // Where d.OrderID = @orderId And d.ProductId = p.ProductId
        var details = await (
            from d in db.OrderRequestDetails.AsNoTracking()
            join p in db.Products.AsNoTracking() on d.ProductID equals p.ProductID
            where d.OrderID == orderId
            select new
            {
                d.OrderDetailID,
                d.Quantity,
                p.ProductID,
                p.ProductName,
                d.UnitPrice,
                d.SalePrice,
                p.UnitsInStock,
                p.UnitsOnOrder,
                QuantityPerUnit = p.QuantityPerUnit,
                Unit = p.Unit,
                d.LineTotal
            }
        ).ToListAsync(ct);

        // --- PARAMETER ITERATION: iterate detail lines accumulating subtotal/tax/total ---
        // Port of AddToTotals: currentSubTotal += lineTotal; currentTotalTax = currentSubTotal * (currentTax / 100);
        //   currentTotal = currentSubTotal + currentTotalTax + currentFreightCharge
        decimal currentSubTotal = 0m;

        foreach (var detail in details)
        {
            decimal lineTotal = detail.LineTotal ?? 0m;
            currentSubTotal += lineTotal;
        }

        decimal currentTotalTax = currentSubTotal * (currentTax / 100m);
        decimal currentTotal = currentSubTotal + currentTotalTax + currentFreightCharge;

        // --- DisplayTotals: format and assign to state ---
        state!.CurrentSubTotal = currentSubTotal;
        state.CurrentTax = currentTax;
        state.CurrentFreightCharge = currentFreightCharge;
        state.CurrentTotalTax = currentTotalTax;
        state.CurrentTotal = currentTotal;

        state.TxtSubTotalText = currentSubTotal.ToString("N2", CultureInfo.CurrentCulture);
        state.TxtTotalTaxText = currentTotalTax.ToString("N2", CultureInfo.CurrentCulture);
        state.TxtTotalText = currentTotal.ToString("N2", CultureInfo.CurrentCulture);
        state.TxtStatusText = status;

        _logger.LogInformation("Loaded order request {OrderId}: {DetailCount} detail lines, subtotal={SubTotal}, total={Total}",
            orderId, details.Count, currentSubTotal, currentTotal);

        return state;
    }

    /// <summary>
    /// Loads detail lines for the specified order request and returns them as DTOs.
    /// Port of <c>frmActionOrderRequest.LoadData</c> (detail grid population).
    /// </summary>
    public async Task<List<OrderRequestDetailLineDto>> LoadDataAsync(int orderId, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var details = await (
            from d in db.OrderRequestDetails.AsNoTracking()
            join p in db.Products.AsNoTracking() on d.ProductID equals p.ProductID
            where d.OrderID == orderId
            select new OrderRequestDetailLineDto
            {
                OrderDetailID = d.OrderDetailID,
                Quantity = d.Quantity,
                ProductID = p.ProductID ?? string.Empty,
                ProductName = p.ProductName ?? string.Empty,
                UnitPrice = d.UnitPrice,
                SalePrice = d.SalePrice,
                UnitsInStock = p.UnitsInStock,
                UnitsOnOrder = p.UnitsOnOrder,
                QuantityPerUnitDisplay = ((p.QuantityPerUnit ?? 0m).ToString(CultureInfo.InvariantCulture)) + " " + (p.Unit ?? string.Empty),
                LineTotal = d.LineTotal
            }
        ).ToListAsync(ct);

        _logger.LogInformation("LoadDataAsync: Loaded {Count} detail lines for order {OrderId}", details.Count, orderId);

        return details;
    }

    /// <summary>
    /// Port of <c>frmActionOrderRequest.cmdApprove_Click</c>.
    /// Validates current status (PRESAVEVALIDATION), updates to APPROVED with transaction (TRANSACTION),
    /// retries on concurrency (LOCKRETRY), typed exception handling (TYPEDEXCEPTIONS).
    /// </summary>
    public async Task ApproveAsync(ActionOrderRequestStateDto state, object eventSender, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        // Extract OrderID from state
        int orderId = state?.OrderID ?? 0;

        // --- PRESAVEVALIDATION: check current status before update ---
        var order = await db.OrderRequests.FirstOrDefaultAsync(o => o.OrderID == orderId, ct);

        if (order is null)
        {
            _logger.LogWarning("ApproveAsync: Order request {OrderId} not found", orderId);
            state!.TxtStatusText = $"The order with the ID '{orderId}' does not exist";
            return;
        }

        var currentStatus = (order.Status ?? string.Empty).Trim().ToUpperInvariant();

        // Port of: if (txtStatus.Text.ToUpper() == "APPROVED") { LogStatus("Order is already approved..."); return; }
        if (currentStatus == "APPROVED")
        {
            _logger.LogInformation("ApproveAsync: Order {OrderId} is already approved", orderId);
            state!.TxtStatusText = "Order is already approved, not need to be approved again";
            return;
        }

        // Port of: if (txtStatus.Text.ToUpper() == "CANCELLED") { LogStatus("Order was already cancelled by..."); return; }
        if (currentStatus == "CANCELLED")
        {
            _logger.LogInformation("ApproveAsync: Order {OrderId} was already cancelled", orderId);
            state!.TxtStatusText = $"Order was already cancelled by {order.ChangedBy ?? string.Empty} on {order.ChangedDate ?? string.Empty}, it cannot be approved";
            return;
        }

        // --- TRANSACTION + LOCKRETRY ---
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        int retries = 0;

        retry:
        try
        {
            // Port of: Update OrderRequests Set Status = 'APPROVED', ChangedBy = @userId, ChangedDate = @today Where OrderId = @orderId
            order.Status = "APPROVED";
            order.ChangedBy = (eventSender as string) ?? string.Empty;
            order.ChangedDate = LegacyDate.Today();

            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

            _logger.LogInformation("Order request {OrderId} approved by {UserId}", orderId, order.ChangedBy);
            state!.TxtStatusText = "The order was successfully approved";
        }
        catch (DbUpdateConcurrencyException ex) when (retries < 3) // LOCKRETRY
        {
            retries++;
            _logger.LogWarning(ex, "Concurrency conflict approving order {OrderId}, retry {Retry}/3", orderId, retries);

            // Reload entity and retry
            await db.Entry(order).ReloadAsync(ct);
            await Task.Delay(100 * retries, ct);
            goto retry;
        }
        catch (DbUpdateConcurrencyException ex) // TYPEDEXCEPTIONS — exhausted retries
        {
            await transaction.RollbackAsync(ct);
            _logger.LogError(ex, "Failed to approve order {OrderId} after 3 retries", orderId);
            state!.TxtStatusText = "A concurrency error occurred. Please try again.";
        }
        catch (Exception ex) // TYPEDEXCEPTIONS — general error
        {
            await transaction.RollbackAsync(ct);
            _logger.LogError(ex, "Error approving order request {OrderId}", orderId);
            state!.TxtStatusText = $"An error has occurred adding the data.\nError: {ex.Message}";
        }
    }

    /// <summary>
    /// Port of <c>frmActionOrderRequest.cmdCancel_Click</c>.
    /// Validates current status (PRESAVEVALIDATION), updates to CANCELLED with transaction (TRANSACTION),
    /// retries on concurrency (LOCKRETRY), typed exception handling (TYPEDEXCEPTIONS).
    /// </summary>
    public async Task CancelAsync(ActionOrderRequestStateDto state, object eventSender, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        // Extract OrderID from state
        int orderId = state?.OrderID ?? 0;

        // --- PRESAVEVALIDATION: check current status before update ---
        var order = await db.OrderRequests.FirstOrDefaultAsync(o => o.OrderID == orderId, ct);

        if (order is null)
        {
            _logger.LogWarning("CancelAsync: Order request {OrderId} not found", orderId);
            state!.TxtStatusText = $"The order with the ID '{orderId}' does not exist";
            return;
        }

        var currentStatus = (order.Status ?? string.Empty).Trim().ToUpperInvariant();

        // Port of: if (txtStatus.Text.ToUpper() == "CANCELLED") { LogStatus("Order was already cancelled..."); return; }
        if (currentStatus == "CANCELLED")
        {
            _logger.LogInformation("CancelAsync: Order {OrderId} is already cancelled", orderId);
            state!.TxtStatusText = "Order was already cancelled, not need to be cancelled again";
            return;
        }

        // Port of: if (txtStatus.Text.ToUpper() == "APPROVED") { LogStatus("Order was already approved by..."); return; }
        if (currentStatus == "APPROVED")
        {
            _logger.LogInformation("CancelAsync: Order {OrderId} was already approved", orderId);
            state!.TxtStatusText = $"Order was already approved by {order.ChangedBy ?? string.Empty} on {order.ChangedDate ?? string.Empty}, it cannot be cancelled";
            return;
        }

        // --- TRANSACTION + LOCKRETRY ---
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        int retries = 0;

        retry:
        try
        {
            // Port of: Update OrderRequests Set Status = 'CANCELLED', ChangedBy = @userId, ChangedDate = @today Where OrderId = @orderId
            order.Status = "CANCELLED";
            order.ChangedBy = (eventSender as string) ?? string.Empty;
            order.ChangedDate = LegacyDate.Today();

            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

            _logger.LogInformation("Order request {OrderId} cancelled by {UserId}", orderId, order.ChangedBy);
            state!.TxtStatusText = "The order was successfully cancelled";
        }
        catch (DbUpdateConcurrencyException ex) when (retries < 3) // LOCKRETRY
        {
            retries++;
            _logger.LogWarning(ex, "Concurrency conflict cancelling order {OrderId}, retry {Retry}/3", orderId, retries);

            // Reload entity and retry
            await db.Entry(order).ReloadAsync(ct);
            await Task.Delay(100 * retries, ct);
            goto retry;
        }
        catch (DbUpdateConcurrencyException ex) // TYPEDEXCEPTIONS — exhausted retries
        {
            await transaction.RollbackAsync(ct);
            _logger.LogError(ex, "Failed to cancel order {OrderId} after 3 retries", orderId);
            state!.TxtStatusText = "A concurrency error occurred. Please try again.";
        }
        catch (Exception ex) // TYPEDEXCEPTIONS — general error
        {
            await transaction.RollbackAsync(ct);
            _logger.LogError(ex, "Error cancelling order request {OrderId}", orderId);
            state!.TxtStatusText = $"An error has occurred adding the data.\nError: {ex.Message}";
        }
    }
}
