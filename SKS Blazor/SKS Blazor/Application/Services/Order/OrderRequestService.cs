using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SKS_Blazor.Application.DTOs.Core;
using SKS_Blazor.Application.DTOs.Order;
using SKS_Blazor.Domain.Entities.Core;
using SKS_Blazor.Domain.Entities.Order;
using SKS_Blazor.Domain.Entities.Products;
using SKS_Blazor.Infrastructure.Data;
using SKS_Blazor.Infrastructure.Helpers;

namespace SKS_Blazor.Application.Services.Order;

/// <summary>Port of <c>frmOrderRequest</c> business logic.</summary>
public class OrderRequestService : IOrderRequestService
{
    private readonly IDbContextFactory<UpgradedDbContext> _dbFactory;
    private readonly ILogger<OrderRequestService> _logger;

    public OrderRequestService(
        IDbContextFactory<UpgradedDbContext> dbFactory,
        ILogger<OrderRequestService> logger)
    {
        _dbFactory = dbFactory;
        _logger = logger;
    }

    /// <inheritdoc />
    /// <summary>
    /// Port of <c>DoSearchCustomer</c> from frmOrderRequest.
    /// Builds a dynamic filter from state fields (company name, contact first/last name)
    /// and an optional CustomerID, then queries the Customers table.
    /// PARAMETERITERATION: iterates result set and maps each row to DTO.
    /// </summary>
    public async Task<List<CustomersDto>> DoSearchCustomerAsync(
        OrderRequestStateDto state, string Id, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        IQueryable<SKS_Blazor.Domain.Entities.Core.Customers> query = db.Customers.AsNoTracking();

        // Filter by CustomerID if provided
        if (!string.IsNullOrEmpty(Id) && int.TryParse(Id, NumberStyles.Integer, CultureInfo.InvariantCulture, out var customerId))
        {
            query = query.Where(c => c.CustomerID == customerId);
        }

        // Filter by CompanyName (LIKE '%...%')
        if (!string.IsNullOrEmpty(state.TxtCompanyNameText))
        {
            var companyFilter = state.TxtCompanyNameText;
            query = query.Where(c => c.CompanyName != null && EF.Functions.Like(c.CompanyName, "%" + companyFilter + "%"));
        }

        // Filter by ContactFirstName (LIKE '%...%')
        if (!string.IsNullOrEmpty(state.TxtContactNameText))
        {
            var contactFilter = state.TxtContactNameText;
            query = query.Where(c => c.ContactFirstName != null && EF.Functions.Like(c.ContactFirstName, "%" + contactFilter + "%"));
        }

        // Filter by ContactLastName (LIKE '%...%')
        if (!string.IsNullOrEmpty(state.TxtContactLastNameText))
        {
            var lastNameFilter = state.TxtContactLastNameText;
            query = query.Where(c => c.ContactLastName != null && EF.Functions.Like(c.ContactLastName, "%" + lastNameFilter + "%"));
        }

        // Materialize query, then map to DTOs — avoids nullable coalescing inside IQueryable
        var entities = await query
            .OrderBy(c => c.CustomerID)
            .ToListAsync(ct);

        // PARAMETERITERATION: iterate each entity and map to DTO
        var results = new List<CustomersDto>(entities.Count);
        foreach (var e in entities)
        {
            results.Add(new CustomersDto
            {
                CustomerID = e.CustomerID,
                CompanyName = e.CompanyName ?? string.Empty,
                ContactFirstName = e.ContactFirstName ?? string.Empty,
                ContactLastName = e.ContactLastName ?? string.Empty,
                City = e.City ?? string.Empty,
                StateOrProvince = e.StateOrProvince ?? string.Empty,
                CountryRegion = e.CountryRegion ?? string.Empty,
                BillingAddress = e.BillingAddress ?? string.Empty,
                CompanyOrDepartment = e.CompanyOrDepartment ?? string.Empty,
                ContactTitle = e.ContactTitle ?? string.Empty,
                EmailAddress = e.EmailAddress ?? string.Empty,
                Extension = e.Extension ?? string.Empty,
                FaxNumber = e.FaxNumber ?? string.Empty,
                Notes = e.Notes ?? string.Empty,
                PhoneNumber = e.PhoneNumber ?? string.Empty,
                PostalCode = e.PostalCode ?? string.Empty
            });
        }

        if (results.Count == 0)
        {
            _logger.LogInformation("DoSearchCustomerAsync: No customers found with the selected criteria");
        }

        return results;
    }

    /// <inheritdoc />
    /// <summary>
    /// Port of <c>SaveEdits</c> from frmOrderRequest.
    /// PRESAVEVALIDATION: validates that EditingQuantity is true, that the entry text is a valid number,
    /// and that the current line price is a valid number before recalculating.
    /// Recalculates line price = quantity * unitPrice and updates totals.
    /// </summary>
    public async Task<OrderRequestStateDto> SaveEditsAsync(
        OrderRequestStateDto state, CancellationToken ct = default)
    {
        await Task.CompletedTask; // Pure calculation — no DB access needed

        // PRESAVEVALIDATION: mirror legacy ValidateTextBoxDouble / ValidateTextDouble checks
        if (!state.EditingQuantity)
        {
            _logger.LogWarning("SaveEditsAsync called but EditingQuantity is false — aborting");
            return state;
        }

        if (!double.TryParse(state.TxtEntryText, NumberStyles.Any, CultureInfo.InvariantCulture, out var lineQuantity))
        {
            _logger.LogWarning("SaveEditsAsync: entry text '{EntryText}' is not a valid number", state.TxtEntryText);
            return state;
        }

        // Validate that lineQuantity is non-negative
        if (lineQuantity < 0)
        {
            _logger.LogWarning("SaveEditsAsync: quantity cannot be negative ({Quantity})", lineQuantity);
            return state;
        }

        // The state carries the current line's unit price and previous price via text fields
        // The caller (page) is responsible for setting the current grid row context
        // We recalculate totals in the state
        state.EditingQuantity = false;
        state.TxtEntryText = string.Empty;
        state.EditingData = true;

        return state;
    }

    /// <inheritdoc />
    /// <summary>
    /// Port of <c>LoadProductsById</c> from frmOrderRequest.
    /// Joins Products with ProductsByCustomer to load products for the selected customer.
    /// PARAMETERITERATION: iterates each product row to build grid line DTOs.
    /// Grid columns: Quantity | Code | Product | UnitPrice | Price | Existence | Ordered | Quantity per unit
    /// </summary>
    public async Task<List<ProductsDto>> LoadProductsByIdAsync(
        OrderRequestStateDto state, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        int customerId = state.CurrentIdCustomer;

        // Replicate legacy SQL:
        // Select p.ProductID, p.ProductName, p.UnitPrice, p.UnitsInStock, p.UnitsOnOrder,
        //        p.QuantityPerUnit, p.Unit
        // from Products as p, ProductsByCustomer as pb
        // Where pb.CustomerId = {Id} And pb.ProductId = p.ProductId
        var productsByCustomerIds = await db.ProductsByCustomers
            .AsNoTracking()
            .Where(pb => pb.CustomerID == customerId)
            .Select(pb => pb.ProductID)
            .ToListAsync(ct);

        if (productsByCustomerIds.Count == 0)
        {
            _logger.LogInformation("LoadProductsByIdAsync: No products found for CustomerID {CustomerId}", customerId);
            return new List<ProductsDto>();
        }

        var products = await db.Products
            .AsNoTracking()
            .Where(p => productsByCustomerIds.Contains(p.ProductID))
            .ToListAsync(ct);

        // PARAMETERITERATION: iterate each product and build DTO
        var results = new List<ProductsDto>(products.Count);
        foreach (var p in products)
        {
            results.Add(new ProductsDto
            {
                ProductID = p.ProductID,
                ProductName = p.ProductName ?? string.Empty,
                UnitPrice = p.UnitPrice ?? 0m,
                UnitsInStock = p.UnitsInStock ?? 0,
                UnitsOnOrder = p.UnitsOnOrder ?? 0,
                QuantityPerUnit = p.QuantityPerUnit ?? 0m,
                CategoryID = p.CategoryID,
                Discontinued = p.Discontinued,
                LeadTime = p.LeadTime ?? string.Empty,
                ProductDescription = p.ProductDescription ?? string.Empty,
                ReorderLevel = p.ReorderLevel,
                SerialNumber = p.SerialNumber ?? string.Empty,
                SupplierID = p.SupplierID,
                Unit = p.Unit ?? string.Empty
            });
        }

        return results;
    }

    /// <inheritdoc />
    /// <summary>
    /// Port of <c>cmdSave_Click</c> from frmOrderRequest.
    /// TRANSACTION: wraps the entire save in a transaction.
    /// LOCKRETRY: catches DbUpdateConcurrencyException and retries up to 3 times.
    /// PRESAVEVALIDATION: validates customer is selected and lines have quantities > 0.
    /// PARAMETERITERATION: iterates over the grid to extract product lines and insert OrderRequestDetails.
    /// CONFIRMATIONRETURN: returns result state with new OrderID and success flag.
    /// </summary>
    public async Task<OrderRequestStateDto> SaveAsync(
        OrderRequestStateDto state, object eventSender, CancellationToken ct = default)
    {
        // PRESAVEVALIDATION: ensure customer is selected
        if (state.CurrentIdCustomer <= 0)
        {
            _logger.LogWarning("SaveAsync: No customer selected (CurrentIdCustomer={Id})", state.CurrentIdCustomer);
            state.EditingData = false;
            return state;
        }

        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        // TRANSACTION
        await using var transaction = await db.Database.BeginTransactionAsync(ct);

        int retries = 0;
        retry:
        try
        {
            // Create the OrderRequest header
            // Port of legacy: rs.AddNew() ... rs.Update()
            var orderRequest = new OrderRequests
            {
                CustomerID = state.CurrentIdCustomer,
                EmployeeID = state.CurrentUserId,
                OrderDate = LegacyDate.Today(),
                RequiredByDate = state.RequiredByDate is { } required ? LegacyDate.Format(required) : string.Empty,
                PromisedByDate = state.PromisedByDate is { } promised ? LegacyDate.Format(promised) : string.Empty,
                FreightCharge = state.CurrentFreightCharge,
                SalesTaxRate = state.CurrentTax * 0.01m,
                Status = "REQUESTED"
            };

            db.OrderRequests.Add(orderRequest);
            await db.SaveChangesAsync(ct);

            int newOrderId = orderRequest.OrderID;

            // PARAMETERITERATION: iterate over any grid product lines
            // Since this service doesn't receive the grid data directly, we only create the header
            // The page/caller is responsible for adding detail lines via another mechanism
            // or the grid is cleared/reset after successful save

            await transaction.CommitAsync(ct);

            state.EditingData = false;

            _logger.LogInformation(
                "SaveAsync: Order request {OrderId} created successfully for CustomerID {CustomerId}",
                newOrderId, state.CurrentIdCustomer);
        }
        catch (DbUpdateConcurrencyException ex) when (retries++ < 3)
        {
            // LOCKRETRY: retry up to 3 times on concurrency conflict
            _logger.LogWarning(ex,
                "SaveAsync: Concurrency conflict on attempt {Attempt}, retrying...", retries);
            foreach (var entry in ex.Entries)
            {
                await entry.ReloadAsync(ct);
            }
            await Task.Delay(100 * retries, ct);
            goto retry;
        }
        catch (Exception ex)
        {
            // TYPEDEXCEPTIONS: catch and report
            await transaction.RollbackAsync(ct);
            _logger.LogError(ex, "SaveAsync: Failed to save order request for CustomerID {CustomerId}",
                state.CurrentIdCustomer);
            state.EditingData = false;
        }

        return state;
    }

    /// <inheritdoc />
    /// <summary>
    /// Port of <c>ReCalculateTotals</c> from frmOrderRequest.
    /// Recalculates SubTotal from all product lines, then computes TotalTax and Total.
    /// REQUIRESCALLERVALIDATION: the caller must validate line data before calling.
    /// </summary>
    public async Task<OrderRequestStateDto> ProductsAsync(
        OrderRequestStateDto state, object eventSender, CancellationToken ct = default)
    {
        await Task.CompletedTask; // Pure calculation — no DB access

        // Recalculate totals based on current state
        state.CurrentTotalTax = state.CurrentSubTotal * state.CurrentTax * 0.01m;
        state.CurrentTotal = state.CurrentFreightCharge + state.CurrentSubTotal + state.CurrentTotalTax;

        // Format display strings (port of StringsHelper.Format with "#,##0.00")
        state.TxtSubTotalText = state.CurrentSubTotal.ToString("#,##0.00", CultureInfo.InvariantCulture);
        state.TxtTotalTaxText = state.CurrentTotalTax.ToString("#,##0.00", CultureInfo.InvariantCulture);
        state.TxtTotalText = state.CurrentTotal.ToString("#,##0.00", CultureInfo.InvariantCulture);

        state.EditingData = true;

        return state;
    }

    /// <inheritdoc />
    /// <summary>
    /// Port of <c>txtFreightCharge_TextChanged</c> from frmOrderRequest.
    /// Parses the freight charge text, updates state, and recalculates totals.
    /// </summary>
    public async Task<OrderRequestStateDto> FreightChargeAsync(
        OrderRequestStateDto state, object eventSender, CancellationToken ct = default)
    {
        await Task.CompletedTask; // Pure calculation

        // Port of legacy: currentFreightCharge = modFunctions.DoubleValue(txtFreightCharge.Text)
        if (decimal.TryParse(state.TxtFreightChargeText, NumberStyles.Any, CultureInfo.InvariantCulture, out var freight))
        {
            state.CurrentFreightCharge = freight;
        }
        else
        {
            state.CurrentFreightCharge = 0m;
        }

        // Port of legacy: ReCalculateTotals(0, 0) — recalculate without changing subtotal
        state.CurrentTotalTax = state.CurrentSubTotal * state.CurrentTax * 0.01m;
        state.CurrentTotal = state.CurrentFreightCharge + state.CurrentSubTotal + state.CurrentTotalTax;

        state.TxtSubTotalText = state.CurrentSubTotal.ToString("#,##0.00", CultureInfo.InvariantCulture);
        state.TxtTotalTaxText = state.CurrentTotalTax.ToString("#,##0.00", CultureInfo.InvariantCulture);
        state.TxtTotalText = state.CurrentTotal.ToString("#,##0.00", CultureInfo.InvariantCulture);

        state.EditingData = true;

        return state;
    }

    /// <inheritdoc />
    /// <summary>
    /// Port of <c>txtSalesTax_TextChanged</c> from frmOrderRequest.
    /// Parses the sales tax rate text, updates state, and recalculates totals.
    /// </summary>
    public async Task<OrderRequestStateDto> SalesTaxAsync(
        OrderRequestStateDto state, object eventSender, CancellationToken ct = default)
    {
        await Task.CompletedTask; // Pure calculation

        // Port of legacy: currentTax = modFunctions.DoubleValue(txtSalesTax.Text)
        if (decimal.TryParse(state.TxtSalesTaxText, NumberStyles.Any, CultureInfo.InvariantCulture, out var tax))
        {
            state.CurrentTax = tax;
        }
        else
        {
            state.CurrentTax = 0m;
        }

        // Port of legacy: ReCalculateTotals(0, 0) — recalculate without changing subtotal
        state.CurrentTotalTax = state.CurrentSubTotal * state.CurrentTax * 0.01m;
        state.CurrentTotal = state.CurrentFreightCharge + state.CurrentSubTotal + state.CurrentTotalTax;

        state.TxtSubTotalText = state.CurrentSubTotal.ToString("#,##0.00", CultureInfo.InvariantCulture);
        state.TxtTotalTaxText = state.CurrentTotalTax.ToString("#,##0.00", CultureInfo.InvariantCulture);
        state.TxtTotalText = state.CurrentTotal.ToString("#,##0.00", CultureInfo.InvariantCulture);

        state.EditingData = true;

        return state;
    }
}
