using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SKS_Blazor.Application.DTOs.Core;
using SKS_Blazor.Application.DTOs.Order;
using SKS_Blazor.Domain.Entities.Core;
using SKS_Blazor.Domain.Entities.Products;
using SKS_Blazor.Infrastructure.Data;
using SKS_Blazor.Domain.Entities.Order;
using SKS_Blazor.Infrastructure.Helpers;

namespace SKS_Blazor.Application.Services.Order;

/// <summary>
/// Service implementation for order reception operations.
/// Port of <c>frmOrderReception</c> business logic.
/// </summary>
public class OrderReceptionService : IOrderReceptionService
{
    private readonly IDbContextFactory<UpgradedDbContext> _dbFactory;
    private readonly ILogger<OrderReceptionService> _logger;

    public OrderReceptionService(
        IDbContextFactory<UpgradedDbContext> dbFactory,
        ILogger<OrderReceptionService> logger)
    {
        _dbFactory = dbFactory;
        _logger = logger;
    }

    /// <summary>
    /// Port of <c>DoSearchProvider</c> from frmOrderReception.
    /// Searches providers by ID, name, contact first name, and contact last name
    /// using filters from the state text fields.
    /// </summary>
    public async Task<List<ProvidersDto>> DoSearchProviderAsync(
        OrderReceptionStateDto state, int Id, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        IQueryable<SKS_Blazor.Domain.Entities.Core.Providers> query = db.Providers.AsNoTracking();

        if (Id != 0)
        {
            query = query.Where(p => p.ProviderID == Id);
        }

        if (!string.IsNullOrEmpty(state.TxtProviderNameText))
        {
            var providerNameFilter = state.TxtProviderNameText;
            query = query.Where(p => p.ProviderName != null && EF.Functions.Like(p.ProviderName, "%" + providerNameFilter + "%"));
        }

        if (!string.IsNullOrEmpty(state.TxtContactNameText))
        {
            var contactNameFilter = state.TxtContactNameText;
            query = query.Where(p => p.ContactFirstName != null && EF.Functions.Like(p.ContactFirstName, "%" + contactNameFilter + "%"));
        }

        if (!string.IsNullOrEmpty(state.TxtContactLastNameText))
        {
            var contactLastNameFilter = state.TxtContactLastNameText;
            query = query.Where(p => p.ContactLastName != null && EF.Functions.Like(p.ContactLastName, "%" + contactLastNameFilter + "%"));
        }

        var entities = await query.ToListAsync(ct);

        if (entities.Count == 0)
        {
            _logger.LogInformation("DoSearchProviderAsync: No providers found matching criteria (Id={Id})", Id);
        }

        var result = new List<ProvidersDto>(entities.Count);
        foreach (var e in entities)
        {
            result.Add(new ProvidersDto
            {
                ProviderID = e.ProviderID,
                ProviderName = e.ProviderName ?? string.Empty,
                ContactFirstName = e.ContactFirstName ?? string.Empty,
                ContactLastName = e.ContactLastName ?? string.Empty,
                City = e.City ?? string.Empty,
                StateOrProvince = e.StateOrProvince ?? string.Empty,
                CountryRegion = e.CountryRegion ?? string.Empty,
                Address = e.Address ?? string.Empty,
                ContactTitle = e.ContactTitle ?? string.Empty,
                EmailAddress = e.EmailAddress ?? string.Empty,
                Extension = e.Extension ?? string.Empty,
                FaxNumber = e.FaxNumber ?? string.Empty,
                Notes = e.Notes ?? string.Empty,
                PaymentTerms = e.PaymentTerms ?? string.Empty,
                PhoneNumber = e.PhoneNumber ?? string.Empty,
                PostalCode = e.PostalCode ?? string.Empty
            });
        }

        return result;
    }

    /// <summary>
    /// Port of <c>SaveEdits</c> from frmOrderReception.
    /// Validates entry text and recalculates line price and totals after inline edit.
    /// </summary>
    public async Task<OrderReceptionStateDto> SaveEditsAsync(
        OrderReceptionStateDto state, CancellationToken ct = default)
    {
        await Task.CompletedTask;

        if (string.IsNullOrWhiteSpace(state.TxtEntryText))
        {
            _logger.LogWarning("SaveEditsAsync: Entry text is empty, aborting edit");
            return state;
        }

        if (!decimal.TryParse(state.TxtEntryText, NumberStyles.Any, CultureInfo.InvariantCulture, out _))
        {
            _logger.LogWarning("SaveEditsAsync: Entry text '{Text}' is not a valid number", state.TxtEntryText);
            return state;
        }

        state.EditingData = true;

        state.CurrentTotalTax = state.CurrentSubTotal * state.CurrentTax * 0.01m;
        state.CurrentTotal = state.CurrentFreightCharge + state.CurrentSubTotal + state.CurrentTotalTax;

        state.TxtSubTotalText = state.CurrentSubTotal.ToString("#,##0.00", CultureInfo.InvariantCulture);
        state.TxtTotalTaxText = state.CurrentTotalTax.ToString("#,##0.00", CultureInfo.InvariantCulture);
        state.TxtTotalText = state.CurrentTotal.ToString("#,##0.00", CultureInfo.InvariantCulture);

        return state;
    }

    /// <summary>
    /// Port of <c>LoadProductsById</c> from frmOrderReception.
    /// Loads products associated with the currently selected provider via ProductsByProvider join table.
    /// Returns ProductsDto list matching the plan signature.
    /// </summary>
    public async Task<List<ProductsDto>> LoadProductsByIdAsync(
        OrderReceptionStateDto state, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        int providerId = state.CurrentIdProvider;

        // Legacy query: Select p.ProductID, p.ProductName, p.UnitPrice, p.UnitsInStock,
        //               p.UnitsOnOrder, p.QuantityPerUnit, p.Unit
        //               from Products as p, ProductsByProvider as pb
        //               Where pb.ProviderID = {Id} And pb.ProductId = p.ProductId
        var rows = await db.ProductsByProviders
            .AsNoTracking()
            .Where(pb => pb.ProviderID == providerId)
            .Join(
                db.Products.AsNoTracking(),
                pb => pb.ProductID,
                p => p.ProductID,
                (pb, p) => p)
            .ToListAsync(ct);

        var result = new List<ProductsDto>(rows.Count);
        foreach (var p in rows)
        {
            result.Add(new ProductsDto
            {
                ProductID = p.ProductID ?? string.Empty,
                ProductName = p.ProductName ?? string.Empty,
                ProductDescription = p.ProductDescription ?? string.Empty,
                UnitPrice = p.UnitPrice,
                UnitsInStock = p.UnitsInStock,
                UnitsOnOrder = p.UnitsOnOrder,
                QuantityPerUnit = p.QuantityPerUnit,
                Unit = p.Unit ?? string.Empty,
                CategoryID = p.CategoryID,
                Discontinued = p.Discontinued,
                LeadTime = p.LeadTime ?? string.Empty,
                ReorderLevel = p.ReorderLevel,
                SerialNumber = p.SerialNumber ?? string.Empty,
                SupplierID = p.SupplierID
            });
        }

        _logger.LogInformation("LoadProductsByIdAsync: Loaded {Count} products for provider {ProviderId}",
            result.Count, providerId);

        return result;
    }

    /// <summary>
    /// Port of <c>cmdSave_Click</c> from frmOrderReception.
    /// Persists a new OrderReception header and all non-zero-quantity detail lines.
    /// The eventSender object carries the product lines list (List&lt;OrderReceptionProductLineDto&gt;).
    /// Returns updated state with success/failure info in TxtNotesText and EditingData flag.
    /// TRANSACTION: wraps insert in BeginTransactionAsync/CommitAsync.
    /// LOCKRETRY: retries on DbUpdateConcurrencyException up to 3 times.
    /// PRESAVEVALIDATION: validates provider is selected and at least one line has quantity.
    /// PARAMETERITERATION: iterates product lines to insert detail rows.
    /// </summary>
    public async Task<OrderReceptionStateDto> SaveAsync(
        OrderReceptionStateDto state, object eventSender, CancellationToken ct = default)
    {
        // Extract product lines from eventSender
        var productLines = eventSender as List<OrderReceptionProductLineDto>;
        if (productLines == null)
        {
            _logger.LogWarning("SaveAsync: eventSender is not List<OrderReceptionProductLineDto>");
            state.TxtNotesText = "Internal error: no product lines provided.";
            return state;
        }

        // PRESAVEVALIDATION: provider must be selected
        if (state.CurrentIdProvider <= 0)
        {
            _logger.LogWarning("SaveAsync: No provider selected (CurrentIdProvider={Id})", state.CurrentIdProvider);
            state.TxtNotesText = "No provider selected. Please select a provider before saving.";
            return state;
        }

        // PRESAVEVALIDATION: at least one line must have non-zero quantity
        bool hasLines = false;
        foreach (var line in productLines)
        {
            if (line.Quantity != 0m)
            {
                hasLines = true;
                break;
            }
        }

        if (!hasLines)
        {
            _logger.LogWarning("SaveAsync: No product lines with non-zero quantity");
            state.TxtNotesText = "No product quantities entered. Please enter at least one product quantity.";
            return state;
        }

        // Retry loop replaces the generated `goto retry`, which jumped back over a using declaration (CS8648).
        for (int retries = 0; ; retries++)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            await using var transaction = await db.Database.BeginTransactionAsync(ct);
            try
            {
                // Create the order reception header entity
                var orderReception = new Domain.Entities.Order.OrderReceptions
                {
                    ProviderID = state.CurrentIdProvider,
                    ReceivedBy = state.CurrentUserId,
                    OrderDate = LegacyDate.Today(),
                    Notes = state.TxtNotesText ?? string.Empty,
                    FreightCharge = state.CurrentFreightCharge,
                    SalesTaxRate = state.CurrentTax * 0.01m,
                    Status = "RECEIVED"
                };

                db.OrderReceptions.Add(orderReception);
                await db.SaveChangesAsync(ct);

                int newOrderId = orderReception.OrderID;

                // PARAMETERITERATION: iterate product lines to insert detail rows
                var detailsToAdd = new List<Domain.Entities.Order.OrderReceptionDetails>();
                foreach (var line in productLines)
                {
                    if (line.Quantity == 0m)
                    {
                        continue;
                    }

                    if (line.UnitPrice < 0m)
                    {
                        _logger.LogWarning("SaveAsync: Negative unit price for product {ProductID}", line.ProductID);
                    }

                    decimal linePrice = line.Quantity * line.UnitPrice;
                    decimal salesTaxRate = state.CurrentTax * 0.01m;

                    var detail = new Domain.Entities.Order.OrderReceptionDetails
                    {
                        OrderID = newOrderId,
                        ProductID = line.ProductID,
                        DateSold = LegacyDate.Today(),
                        Quantity = line.Quantity,
                        UnitPrice = line.UnitPrice,
                        SalePrice = linePrice,
                        SalesTax = salesTaxRate,
                        LineTotal = linePrice
                    };
                    detailsToAdd.Add(detail);
                }

                db.OrderReceptionDetails.AddRange(detailsToAdd);
                await db.SaveChangesAsync(ct);

                await transaction.CommitAsync(ct);

                // Update state to reflect successful save
                state.EditingData = false;
                state.TxtNotesText = $"Order reception #{newOrderId} saved successfully with {detailsToAdd.Count} line(s).";

                _logger.LogInformation(
                    "SaveAsync: Order reception {OrderId} created with {DetailCount} lines for provider {ProviderId}",
                    newOrderId, detailsToAdd.Count, state.CurrentIdProvider);

                // Reset totals after save (legacy clears the form)
                state.CurrentSubTotal = 0m;
                state.CurrentTotal = 0m;
                state.CurrentTotalTax = 0m;
                state.CurrentFreightCharge = 0m;
                state.CurrentTax = 0m;
                state.CurrentIdProvider = 0;
                state.TxtSubTotalText = "0.00";
                state.TxtTotalText = "0.00";
                state.TxtTotalTaxText = "0.00";
                state.TxtFreightChargeText = string.Empty;
                state.TxtSalesTaxText = string.Empty;
                state.TxtEntryText = string.Empty;
                state.TxtProviderNameText = string.Empty;
                state.TxtContactLastNameText = string.Empty;
                state.TxtContactNameText = string.Empty;
                state.TxtProviderContactText = string.Empty;
                state.TxtProviderCompanyText = string.Empty;

                return state;
            }
            catch (DbUpdateConcurrencyException ex) when (retries < 3)
            {
                _logger.LogWarning(ex,
                    "SaveAsync: Concurrency exception on attempt {Attempt}, retrying", retries + 1);
                await transaction.RollbackAsync(ct);
                await Task.Delay(100 * (retries + 1), ct);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(ct);
                state.TxtNotesText = $"An error has occurred adding the data. Error: {ex.Message}";
                _logger.LogError(ex, "SaveAsync: Failed to save order reception for provider {ProviderId}",
                    state.CurrentIdProvider);
                return state;
            }
        }
    }

    /// <summary>
    /// Port of <c>ReCalculateTotals</c> from frmOrderReception.
    /// Recalculates subtotal from all product lines (passed as eventSender), then computes tax and total.
    /// </summary>
    public async Task<OrderReceptionStateDto> ProductsAsync(
        OrderReceptionStateDto state, object eventSender, CancellationToken ct = default)
    {
        await Task.CompletedTask;

        var productLines = eventSender as List<OrderReceptionProductLineDto>;
        if (productLines == null)
        {
            _logger.LogWarning("ProductsAsync: eventSender is null or wrong type, returning state unchanged");
            return state;
        }

        decimal subTotal = 0m;
        foreach (var line in productLines)
        {
            subTotal += line.Price;
        }

        state.CurrentSubTotal = subTotal;

        RecalculateTotalsInternal(state);

        return state;
    }

    /// <summary>
    /// Port of <c>txtFreightCharge_TextChanged</c> + <c>ReCalculateTotals</c>.
    /// Parses the freight charge from state and recalculates totals.
    /// </summary>
    public async Task<OrderReceptionStateDto> FreightChargeAsync(
        OrderReceptionStateDto state, object eventSender, CancellationToken ct = default)
    {
        await Task.CompletedTask;

        // Legacy: currentFreightCharge = modFunctions.DoubleValue(txtFreightCharge.Text);
        if (decimal.TryParse(state.TxtFreightChargeText, NumberStyles.Any, CultureInfo.InvariantCulture, out var freightCharge))
        {
            state.CurrentFreightCharge = freightCharge;
        }
        else
        {
            state.CurrentFreightCharge = 0m;
        }

        // Recalculate subtotal from product lines if provided
        var productLines = eventSender as List<OrderReceptionProductLineDto>;
        if (productLines != null)
        {
            decimal subTotal = 0m;
            foreach (var line in productLines)
            {
                subTotal += line.Price;
            }
            state.CurrentSubTotal = subTotal;
        }

        // Legacy: ReCalculateTotals(0, 0) — does not change subtotal, just recalculates tax + total
        RecalculateTotalsInternal(state);

        state.EditingData = true;

        return state;
    }

    /// <summary>
    /// Port of <c>txtSalesTax_TextChanged</c> + <c>ReCalculateTotals</c>.
    /// Parses the sales tax percentage from state and recalculates totals.
    /// </summary>
    public async Task<OrderReceptionStateDto> SalesTaxAsync(
        OrderReceptionStateDto state, object eventSender, CancellationToken ct = default)
    {
        await Task.CompletedTask;

        // Legacy: currentTax = modFunctions.DoubleValue(txtSalesTax.Text);
        if (decimal.TryParse(state.TxtSalesTaxText, NumberStyles.Any, CultureInfo.InvariantCulture, out var tax))
        {
            state.CurrentTax = tax;
        }
        else
        {
            state.CurrentTax = 0m;
        }

        // Recalculate subtotal from product lines if provided
        var productLines = eventSender as List<OrderReceptionProductLineDto>;
        if (productLines != null)
        {
            decimal subTotal = 0m;
            foreach (var line in productLines)
            {
                subTotal += line.Price;
            }
            state.CurrentSubTotal = subTotal;
        }

        // Legacy: ReCalculateTotals(0, 0)
        RecalculateTotalsInternal(state);

        state.EditingData = true;

        return state;
    }

    /// <summary>
    /// Retrieve all order reception detail records from the database.
    /// </summary>
    public async Task<List<OrderReceptionDetailsDto>> GetOrderReceptionDetailsAsync(CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var entities = await db.OrderReceptionDetails
            .AsNoTracking()
            .ToListAsync(ct);

        var result = new List<OrderReceptionDetailsDto>(entities.Count);
        foreach (var e in entities)
        {
            result.Add(new OrderReceptionDetailsDto
            {
                OrderDetailID = e.OrderDetailID,
                OrderID = e.OrderID,
                ProductID = e.ProductID ?? string.Empty,
                DateSold = e.DateSold ?? string.Empty,
                Quantity = e.Quantity,
                UnitPrice = e.UnitPrice,
                SalePrice = e.SalePrice,
                SalesTax = e.SalesTax,
                Discount = e.Discount,
                LineTotal = e.LineTotal
            });
        }

        return result;
    }

    /// <summary>
    /// Retrieve all order reception header records from the database.
    /// </summary>
    public async Task<List<OrderReceptionsDto>> GetOrderReceptionsAsync(CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var entities = await db.OrderReceptions
            .AsNoTracking()
            .ToListAsync(ct);

        var result = new List<OrderReceptionsDto>(entities.Count);
        foreach (var e in entities)
        {
            result.Add(new OrderReceptionsDto
            {
                OrderID = e.OrderID,
                ProviderID = e.ProviderID,
                ReceivedBy = e.ReceivedBy ?? string.Empty,
                OrderDate = e.OrderDate ?? string.Empty,
                Notes = e.Notes ?? string.Empty,
                FreightCharge = e.FreightCharge,
                SalesTaxRate = e.SalesTaxRate,
                Status = e.Status ?? string.Empty,
                ChangedBy = e.ChangedBy ?? string.Empty,
                ChangedDate = e.ChangedDate ?? string.Empty,
                PromisedByDate = e.PromisedByDate ?? string.Empty,
                PurchaseOrderNumber = e.PurchaseOrderNumber ?? string.Empty,
                RequiredByDate = e.RequiredByDate ?? string.Empty
            });
        }

        return result;
    }

    /// <summary>
    /// Retrieve all order request detail records from the database.
    /// </summary>
    public async Task<List<OrderRequestDetailsDto>> GetOrderRequestDetailsAsync(CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var entities = await db.OrderRequestDetails
            .AsNoTracking()
            .ToListAsync(ct);

        var result = new List<OrderRequestDetailsDto>(entities.Count);
        foreach (var e in entities)
        {
            result.Add(new OrderRequestDetailsDto
            {
                OrderDetailID = e.OrderDetailID,
                OrderID = e.OrderID,
                ProductID = e.ProductID ?? string.Empty,
                DateSold = e.DateSold ?? string.Empty,
                Quantity = e.Quantity,
                UnitPrice = e.UnitPrice,
                SalePrice = e.SalePrice,
                SalesTax = e.SalesTax,
                Discount = e.Discount,
                LineTotal = e.LineTotal
            });
        }

        return result;
    }

    /// <summary>
    /// Retrieve all order request header records from the database.
    /// </summary>
    public async Task<List<OrderRequestsDto>> GetOrderRequestsAsync(CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var entities = await db.OrderRequests
            .AsNoTracking()
            .ToListAsync(ct);

        var result = new List<OrderRequestsDto>(entities.Count);
        foreach (var e in entities)
        {
            result.Add(new OrderRequestsDto
            {
                OrderID = e.OrderID,
                CustomerID = e.CustomerID,
                EmployeeID = e.EmployeeID ?? string.Empty,
                OrderDate = e.OrderDate ?? string.Empty,
                Notes = e.Notes ?? string.Empty,
                FreightCharge = e.FreightCharge,
                SalesTaxRate = e.SalesTaxRate,
                Status = e.Status ?? string.Empty,
                ChangedBy = e.ChangedBy ?? string.Empty,
                ChangedDate = e.ChangedDate ?? string.Empty,
                PromisedByDate = e.PromisedByDate ?? string.Empty,
                PurchaseOrderNumber = e.PurchaseOrderNumber ?? string.Empty,
                RequiredByDate = e.RequiredByDate ?? string.Empty,
                ShipAddress = e.ShipAddress ?? string.Empty,
                ShipCity = e.ShipCity ?? string.Empty,
                ShipCountry = e.ShipCountry ?? string.Empty,
                ShipDate = e.ShipDate ?? string.Empty,
                ShipName = e.ShipName ?? string.Empty,
                ShipPhoneNumber = e.ShipPhoneNumber ?? string.Empty,
                ShippingMethodID = e.ShippingMethodID,
                ShipPostalCode = e.ShipPostalCode ?? string.Empty,
                ShipState = e.ShipState ?? string.Empty,
                ShipStateOrProvince = e.ShipStateOrProvince ?? string.Empty
            });
        }

        return result;
    }

    // ─── Private helpers ────────────────────────────────────────────────

    /// <summary>
    /// Port of <c>ReCalculateTotals(double previous, double current)</c> from frmOrderReception.
    /// Recalculates totalTax and total from current subtotal, tax rate, and freight charge,
    /// then formats the display strings.
    /// </summary>
    private static void RecalculateTotalsInternal(OrderReceptionStateDto state)
    {
        // Legacy:
        // currentSubTotal = currentSubTotal - previous + current;  (already done by caller)
        // currentTotalTax = currentSubTotal * currentTax * 0.01d;
        // currentTotal = currentFreightCharge + currentSubTotal + currentTotalTax;
        state.CurrentTotalTax = state.CurrentSubTotal * state.CurrentTax * 0.01m;
        state.CurrentTotal = state.CurrentFreightCharge + state.CurrentSubTotal + state.CurrentTotalTax;

        state.TxtSubTotalText = state.CurrentSubTotal.ToString("#,##0.00", CultureInfo.InvariantCulture);
        state.TxtTotalTaxText = state.CurrentTotalTax.ToString("#,##0.00", CultureInfo.InvariantCulture);
        state.TxtTotalText = state.CurrentTotal.ToString("#,##0.00", CultureInfo.InvariantCulture);
    }
}
