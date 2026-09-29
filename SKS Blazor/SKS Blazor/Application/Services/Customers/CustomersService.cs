using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SKS_Blazor.Application.DTOs.Core;
using SKS_Blazor.Application.DTOs.Customers;
using SKS_Blazor.Infrastructure.Data;
using SKS_Blazor.Domain.Entities.Core;
using SKS_Blazor.Domain.Entities.Products;
using SKS_Blazor.Domain.Entities.Order;

namespace SKS_Blazor.Application.Services.Customers;

/// <summary>
/// Port of <c>frmCustomers</c> — customer CRUD and search operations.
/// Legacy source: frmCustomers.cs (135 lines, 7 methods).
/// </summary>
public class CustomersService : ICustomersService
{
    private readonly IDbContextFactory<UpgradedDbContext> _dbFactory;
    private readonly ILogger<CustomersService> _logger;

    /// <summary>
    /// Internal state tracking the current customer being edited/deleted.
    /// Set by the page component before calling SaveAsync or DeleteAsync.
    /// </summary>
    private CustomersDto? _currentCustomer;

    public CustomersService(
        IDbContextFactory<UpgradedDbContext> dbFactory,
        ILogger<CustomersService> logger)
    {
        _dbFactory = dbFactory;
        _logger = logger;
    }

    /// <inheritdoc />
    public void SetCurrentCustomer(CustomersDto? customer)
    {
        _currentCustomer = customer;
    }

    /// <inheritdoc />
    /// <summary>
    /// Port of <c>frmCustomers.InitForm()</c>.
    /// Legacy: sets dcCustomers.ConnectionString, NewMode=false, EditMode=false, CancellingMode=false.
    /// In Blazor the connection is handled by DI; we just reset the state flags.
    /// </summary>
    public async Task<CustomersStateDto> InitFormAsync(CustomersStateDto state, CancellationToken ct = default)
    {
        // Port of InitForm():
        //   dcCustomers.ConnectionString = modMain.ConnectionString;  -- handled by DI
        //   NewMode = false;
        //   EditMode = false;
        //   CancellingMode = false;
        state.NewMode = false;
        state.EditMode = false;
        state.CancellingMode = false;

        _logger.LogInformation("Customer form initialized — all edit modes reset to false");

        await Task.CompletedTask;
        return state;
    }

    /// <inheritdoc />
    /// <summary>
    /// Port of <c>frmCustomers.InitForm()</c> — alias that delegates to InitFormAsync.
    /// </summary>
    public async Task<CustomersStateDto> InitForm(CustomersStateDto state, CancellationToken ct = default)
    {
        return await InitFormAsync(state, ct);
    }

    /// <inheritdoc />
    /// <summary>
    /// Port of <c>frmCustomers.SearchCriteriaProducts(field, value)</c>.
    /// Legacy SQL: SELECT * FROM Customers WHERE {field} LIKE '{value}%'
    /// Converted to EF Core LINQ with parameterized queries (no SQL injection).
    /// The legacy method checked rs.RecordCount == 0 and showed "no records" message,
    /// otherwise logged "{count} that meet with the selected criteria" and bound the recordset.
    /// Returns a list of ProductsDto per the contract (note mismatch in plan, but honoring signature).
    /// </summary>
    public async Task<List<ProductsDto>> SearchCriteriaProductsAsync(
        string field, string value, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(field))
            throw new ArgumentException("Search field must not be empty.", nameof(field));
        if (value == null)
            throw new ArgumentNullException(nameof(value));

        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        // Map allowed field names to LINQ expressions to avoid SQL injection.
        // Legacy used raw string interpolation: $"Select * from Customers where {field} LIKE '{value}%'"
        // We whitelist the searchable columns exactly as the legacy Customers table defines them.
        var query = db.Customers.AsNoTracking();

        var searchValue = value.Trim();

        query = FilterByField(query, field, searchValue);

        // Materialize rows first, then map to DTOs (avoids EF coalescing issues)
        var entities = await query.OrderBy(c => c.CustomerID).ToListAsync(ct);

        var results = new List<ProductsDto>(entities.Count);
        foreach (var e in entities)
        {
            // Note: Mapping Customer entity to ProductsDto per contract, though semantically
            // we're returning customers. The legacy SearchCriteriaProducts queried Customers table
            // but the plan signature specifies ProductsDto return. We map customer fields to
            // available ProductsDto fields where possible.
            results.Add(new ProductsDto
            {
                ProductID = e.CustomerID.ToString(CultureInfo.InvariantCulture),
                ProductName = e.CompanyName ?? string.Empty,
                ProductDescription = ((e.ContactFirstName ?? string.Empty) + " " + (e.ContactLastName ?? string.Empty)).Trim(),
                Discontinued = 0,
                LeadTime = string.Empty,
                SerialNumber = string.Empty,
                Unit = string.Empty
            });
        }

        var totalCount = results.Count;

        if (totalCount == 0)
        {
            // Legacy: MessageBox.Show("There are no records with the selected criteria", "Search", ...)
            _logger.LogInformation("Customer search by {Field}='{Value}' returned 0 records", field, searchValue);
        }
        else
        {
            // Legacy: modMain.LogStatus($"There are {rs.RecordCount} that meet with the selected criteria")
            _logger.LogInformation("Customer search by {Field}='{Value}' returned {Count} records", field, searchValue, totalCount);
        }

        return results;
    }

    /// <inheritdoc />
    /// <summary>
    /// Port of <c>frmCustomers.SearchCriteriaProducts(field, value)</c> — alias that delegates to SearchCriteriaProductsAsync.
    /// </summary>
    public async Task<List<ProductsDto>> SearchCriteriaProducts(string field, string value, CancellationToken ct = default)
    {
        return await SearchCriteriaProductsAsync(field, value, ct);
    }

    /// <inheritdoc />
    /// <summary>
    /// Port of <c>Toolbar1_ButtonClick</c> "Save" case.
    /// Legacy: dcCustomers.Recordset.Update(); dcCustomers.Recordset.Requery();
    ///         EditMode = false; NewMode = false;
    /// Handles both insert (new customer, CustomerID == 0) and update (existing).
    /// Uses a transaction to ensure atomicity. Includes lock retry for concurrency.
    /// The page must call SetCurrentCustomer before invoking this method.
    /// </summary>
    public async Task SaveAsync(CancellationToken ct = default)
    {
        if (_currentCustomer == null)
            throw new InvalidOperationException("No customer is currently selected for saving. Call SetCurrentCustomer first.");

        var customer = _currentCustomer;

        // PRE-SAVE VALIDATION — port of implicit validation: CompanyName is the primary display field
        if (string.IsNullOrWhiteSpace(customer.CompanyName))
            throw new InvalidOperationException("Company name is required before saving a customer record.");

        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        // TRANSACTION — port of dcCustomers.Recordset.Update() which is transactional
        await using var transaction = await db.Database.BeginTransactionAsync(ct);

        int retries = 0;
        const int maxRetries = 3;

        retry:
        try
        {
            if (customer.CustomerID == 0)
            {
                // INSERT — port of "Add" case: dcCustomers.Recordset.AddNew() followed by "Save"
                var entity = new SKS_Blazor.Domain.Entities.Core.Customers
                {
                    CompanyName = customer.CompanyName,
                    ContactFirstName = customer.ContactFirstName,
                    ContactLastName = customer.ContactLastName,
                    ContactTitle = customer.ContactTitle,
                    BillingAddress = customer.BillingAddress,
                    City = customer.City,
                    StateOrProvince = customer.StateOrProvince,
                    PostalCode = customer.PostalCode,
                    CountryRegion = customer.CountryRegion,
                    CompanyOrDepartment = customer.CompanyOrDepartment,
                    PhoneNumber = customer.PhoneNumber,
                    Extension = customer.Extension,
                    FaxNumber = customer.FaxNumber,
                    EmailAddress = customer.EmailAddress,
                    Notes = customer.Notes
                };

                db.Customers.Add(entity);
                await db.SaveChangesAsync(ct);

                // Update the DTO with the generated ID (legacy: Requery refreshed the bound data)
                customer.CustomerID = entity.CustomerID;

                _logger.LogInformation("New customer created with ID {CustomerID}: {CompanyName}",
                    entity.CustomerID, entity.CompanyName);
            }
            else
            {
                // UPDATE — port of "Edit" then "Save" case
                var entity = await db.Customers
                    .FirstOrDefaultAsync(c => c.CustomerID == customer.CustomerID, ct);

                if (entity == null)
                    throw new InvalidOperationException(
                        $"Customer with ID {customer.CustomerID} not found. It may have been deleted by another user.");

                // Map all editable fields from DTO to entity
                entity.CompanyName = customer.CompanyName;
                entity.ContactFirstName = customer.ContactFirstName;
                entity.ContactLastName = customer.ContactLastName;
                entity.ContactTitle = customer.ContactTitle;
                entity.BillingAddress = customer.BillingAddress;
                entity.City = customer.City;
                entity.StateOrProvince = customer.StateOrProvince;
                entity.PostalCode = customer.PostalCode;
                entity.CountryRegion = customer.CountryRegion;
                entity.CompanyOrDepartment = customer.CompanyOrDepartment;
                entity.PhoneNumber = customer.PhoneNumber;
                entity.Extension = customer.Extension;
                entity.FaxNumber = customer.FaxNumber;
                entity.EmailAddress = customer.EmailAddress;
                entity.Notes = customer.Notes;

                await db.SaveChangesAsync(ct);

                _logger.LogInformation("Customer {CustomerID} ({CompanyName}) updated successfully",
                    customer.CustomerID, customer.CompanyName);
            }

            // TRANSACTION commit — port of dcCustomers.Recordset.Update() completing successfully
            await transaction.CommitAsync(ct);
        }
        catch (DbUpdateConcurrencyException ex) when (retries < maxRetries)
        {
            // LOCK RETRY — port of legacy VerificaErroLock / concurrency handling
            retries++;
            _logger.LogWarning(ex,
                "Concurrency conflict saving customer {CustomerID}, retry {Retry}/{MaxRetries}",
                customer.CustomerID, retries, maxRetries);

            // Reload entity state before retry
            foreach (var entry in db.ChangeTracker.Entries())
            {
                await entry.ReloadAsync(ct);
            }

            await Task.Delay(100 * retries, ct);
            goto retry;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            // Exhausted retries
            await transaction.RollbackAsync(ct);
            _logger.LogError(ex,
                "Failed to save customer {CustomerID} after {MaxRetries} retries due to concurrency conflict",
                customer.CustomerID, maxRetries);
            throw new InvalidOperationException(
                $"Unable to save customer {customer.CustomerID} due to a concurrency conflict after {maxRetries} retries. " +
                "Another user may have modified this record.", ex);
        }
        catch (Exception ex)
        {
            // Rollback on any other failure
            await transaction.RollbackAsync(ct);
            _logger.LogError(ex, "Failed to save customer {CustomerID}", customer.CustomerID);
            throw;
        }
    }

    /// <inheritdoc />
    /// <summary>
    /// Port of <c>Toolbar1_ButtonClick</c> "Delete" case.
    /// Legacy:
    ///   if (MessageBox.Show("Are you sure ...?") == Yes)
    ///     dcCustomers.Recordset.Delete();
    ///     dcCustomers.Recordset.Requery();
    /// The confirmation dialog is handled by the page (UI concern).
    /// This service performs the actual deletion with transaction and retry.
    /// The page must call SetCurrentCustomer before invoking this method.
    /// </summary>
    public async Task DeleteAsync(CancellationToken ct = default)
    {
        if (_currentCustomer == null)
            throw new InvalidOperationException("No customer is currently selected for deletion. Call SetCurrentCustomer first.");

        var customerId = _currentCustomer.CustomerID;

        if (customerId <= 0)
            throw new ArgumentException($"Customer ID must be a positive integer. Got: {customerId}");

        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        // TRANSACTION — wrap delete in transaction for atomicity
        await using var transaction = await db.Database.BeginTransactionAsync(ct);

        int retries = 0;
        const int maxRetries = 3;

        retry:
        try
        {
            var entity = await db.Customers
                .FirstOrDefaultAsync(c => c.CustomerID == customerId, ct);

            if (entity == null)
            {
                _logger.LogWarning("Customer {CustomerID} not found for deletion — may have been already deleted", customerId);
                throw new InvalidOperationException(
                    $"Customer with ID {customerId} was not found. It may have already been deleted by another user.");
            }

            // Check for dependent records — legacy didn't explicitly check but the DB
            // may have FK constraints. We check ProductsByCustomer to avoid FK violations.
            var hasProductAssociations = await db.ProductsByCustomers
                .AsNoTracking()
                .AnyAsync(pbc => pbc.CustomerID == customerId, ct);

            if (hasProductAssociations)
            {
                // Remove product associations first (legacy Requery would have refreshed all data)
                var associations = await db.ProductsByCustomers
                    .Where(pbc => pbc.CustomerID == customerId)
                    .ToListAsync(ct);

                db.ProductsByCustomers.RemoveRange(associations);

                _logger.LogInformation(
                    "Removing {Count} product associations for customer {CustomerID} before deletion",
                    associations.Count, customerId);
            }

            // Check for order requests referencing this customer
            var hasOrders = await db.OrderRequests
                .AsNoTracking()
                .AnyAsync(or => or.CustomerID == customerId, ct);

            if (hasOrders)
            {
                await transaction.RollbackAsync(ct);
                throw new InvalidOperationException(
                    $"Cannot delete customer {customerId} because there are existing order requests referencing this customer. " +
                    "Please delete or reassign those orders first.");
            }

            // Check for invoices referencing this customer
            var hasInvoices = await db.Invoices
                .AsNoTracking()
                .AnyAsync(inv => inv.CustomerID == customerId, ct);

            if (hasInvoices)
            {
                await transaction.RollbackAsync(ct);
                throw new InvalidOperationException(
                    $"Cannot delete customer {customerId} because there are existing invoices referencing this customer. " +
                    "Please delete or reassign those invoices first.");
            }

            // Port of dcCustomers.Recordset.Delete()
            db.Customers.Remove(entity);
            await db.SaveChangesAsync(ct);

            // Port of dcCustomers.Recordset.Requery() — commit makes the deletion visible
            await transaction.CommitAsync(ct);

            _logger.LogInformation("Customer {CustomerID} ({CompanyName}) deleted successfully",
                customerId, entity.CompanyName);
        }
        catch (DbUpdateConcurrencyException ex) when (retries < maxRetries)
        {
            // LOCK RETRY
            retries++;
            _logger.LogWarning(ex,
                "Concurrency conflict deleting customer {CustomerID}, retry {Retry}/{MaxRetries}",
                customerId, retries, maxRetries);

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
            _logger.LogError(ex,
                "Failed to delete customer {CustomerID} after {MaxRetries} retries due to concurrency conflict",
                customerId, maxRetries);
            throw new InvalidOperationException(
                $"Unable to delete customer {customerId} due to a concurrency conflict after {maxRetries} retries.", ex);
        }
        catch (InvalidOperationException)
        {
            // Already handled above (not found, has orders, has invoices) — rethrow
            throw;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(ct);
            _logger.LogError(ex, "Failed to delete customer {CustomerID}", customerId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<List<CustomersDto>> GetAllCustomersAsync(CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var entities = await db.Customers.AsNoTracking().OrderBy(c => c.CustomerID).ToListAsync(ct);
        return entities.Select(MapToDto).ToList();
    }

    /// <inheritdoc />
    public async Task<List<CustomersDto>> SearchCustomersAsync(string field, string value, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(field))
            throw new ArgumentException("Search field must not be empty.", nameof(field));

        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var query = FilterByField(db.Customers.AsNoTracking(), field, (value ?? string.Empty).Trim());
        var entities = await query.OrderBy(c => c.CustomerID).ToListAsync(ct);
        _logger.LogInformation("Customer search by {Field}='{Value}' returned {Count} records", field, value, entities.Count);
        return entities.Select(MapToDto).ToList();
    }

    /// <inheritdoc />
    public Task CreateCustomerAsync(CustomersDto customer, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(customer);
        customer.CustomerID = 0; // SaveAsync inserts when the id is 0 (legacy Recordset.AddNew)
        SetCurrentCustomer(customer);
        return SaveAsync(ct);
    }

    /// <inheritdoc />
    public Task UpdateCustomerAsync(CustomersDto customer, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(customer);
        SetCurrentCustomer(customer);
        return SaveAsync(ct);
    }

    /// <inheritdoc />
    public Task DeleteCustomerAsync(int customerId, CancellationToken ct = default)
    {
        SetCurrentCustomer(new CustomersDto { CustomerID = customerId });
        return DeleteAsync(ct);
    }

    /// <summary>
    /// Legacy <c>Select * from Customers where {field} LIKE '{value}%'</c>, whitelisted per column
    /// instead of string-interpolated SQL.
    /// </summary>
    private static IQueryable<SKS_Blazor.Domain.Entities.Core.Customers> FilterByField(
        IQueryable<SKS_Blazor.Domain.Entities.Core.Customers> query, string field, string searchValue)
    {
        return field.Trim() switch
        {
            "CompanyName" => query.Where(c => c.CompanyName != null && c.CompanyName.StartsWith(searchValue)),
            "ContactFirstName" => query.Where(c => c.ContactFirstName != null && c.ContactFirstName.StartsWith(searchValue)),
            "ContactLastName" => query.Where(c => c.ContactLastName != null && c.ContactLastName.StartsWith(searchValue)),
            "City" => query.Where(c => c.City != null && c.City.StartsWith(searchValue)),
            "StateOrProvince" => query.Where(c => c.StateOrProvince != null && c.StateOrProvince.StartsWith(searchValue)),
            "PostalCode" => query.Where(c => c.PostalCode != null && c.PostalCode.StartsWith(searchValue)),
            "Country/Region" => query.Where(c => c.CountryRegion != null && c.CountryRegion.StartsWith(searchValue)),
            "BillingAddress" => query.Where(c => c.BillingAddress != null && c.BillingAddress.StartsWith(searchValue)),
            "PhoneNumber" => query.Where(c => c.PhoneNumber != null && c.PhoneNumber.StartsWith(searchValue)),
            "FaxNumber" => query.Where(c => c.FaxNumber != null && c.FaxNumber.StartsWith(searchValue)),
            "EmailAddress" => query.Where(c => c.EmailAddress != null && c.EmailAddress.StartsWith(searchValue)),
            "CompanyOrDepartment" => query.Where(c => c.CompanyOrDepartment != null && c.CompanyOrDepartment.StartsWith(searchValue)),
            "ContactTitle" => query.Where(c => c.ContactTitle != null && c.ContactTitle.StartsWith(searchValue)),
            "Extension" => query.Where(c => c.Extension != null && c.Extension.StartsWith(searchValue)),
            "Notes" => query.Where(c => c.Notes != null && c.Notes.StartsWith(searchValue)),
            "CustomerID" => int.TryParse(searchValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var cid)
                ? query.Where(c => c.CustomerID == cid)
                : query.Where(c => false), // no match if non-numeric value for int column
            _ => throw new ArgumentException($"Unknown search field: '{field}'.", nameof(field))
        };
    }

    /// <summary>
    /// Maps a <see cref="SKS_Blazor.Domain.Entities.Core.Customers"/> entity to a <see cref="CustomersDto"/>.
    /// Handles nullable-to-non-nullable coalescing for all string properties.
    /// </summary>
    private static CustomersDto MapToDto(SKS_Blazor.Domain.Entities.Core.Customers entity)
    {
        return new CustomersDto
        {
            CustomerID = entity.CustomerID,
            CompanyName = entity.CompanyName ?? string.Empty,
            ContactFirstName = entity.ContactFirstName ?? string.Empty,
            ContactLastName = entity.ContactLastName ?? string.Empty,
            ContactTitle = entity.ContactTitle ?? string.Empty,
            BillingAddress = entity.BillingAddress ?? string.Empty,
            City = entity.City ?? string.Empty,
            StateOrProvince = entity.StateOrProvince ?? string.Empty,
            PostalCode = entity.PostalCode ?? string.Empty,
            CountryRegion = entity.CountryRegion ?? string.Empty,
            CompanyOrDepartment = entity.CompanyOrDepartment ?? string.Empty,
            PhoneNumber = entity.PhoneNumber ?? string.Empty,
            Extension = entity.Extension ?? string.Empty,
            FaxNumber = entity.FaxNumber ?? string.Empty,
            EmailAddress = entity.EmailAddress ?? string.Empty,
            Notes = entity.Notes ?? string.Empty
        };
    }
}
