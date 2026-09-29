using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SKS_Blazor.Application.DTOs.Core;
using SKS_Blazor.Application.DTOs.Search;
using SKS_Blazor.Infrastructure.Data;
using SKS_Blazor.Domain.Entities.Core;

namespace SKS_Blazor.Application.Services.Search;

/// <summary>Port of <c>frmSearch</c> — dynamic search across Customers, Products, Providers tables.</summary>
public class SearchService : ISearchService
{
    private readonly IDbContextFactory<UpgradedDbContext> _dbFactory;
    private readonly ILogger<SearchService> _logger;

    // Whitelisted searchable column names per table — prevents SQL injection that the legacy
    // code was vulnerable to (it built raw SQL from user-selected combo items).
    // These lists mirror the columns returned by "Select * from <Table> limit 1" in the legacy code,
    // which populated the cboSrchBy combo box with column names from the recordset metadata.
    private static readonly Dictionary<string, List<string>> _searchableFields = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Customers"] = new List<string>
        {
            "BillingAddress", "City", "CompanyName", "CompanyOrDepartment",
            "ContactFirstName", "ContactLastName", "ContactTitle", "Country/Region",
            "CustomerID", "EmailAddress", "Extension", "FaxNumber", "Notes",
            "PhoneNumber", "PostalCode", "StateOrProvince"
        },
        ["Products"] = new List<string>
        {
            "CategoryID", "Discontinued", "LeadTime", "ProductDescription",
            "ProductID", "ProductName", "QuantityPerUnit", "ReorderLevel",
            "SerialNumber", "SupplierID", "Unit", "UnitPrice", "UnitsInStock", "UnitsOnOrder"
        },
        ["Providers"] = new List<string>
        {
            "Address", "City", "ContactFirstName", "ContactLastName", "ContactTitle",
            "Country/Region", "EmailAddress", "Extension", "FaxNumber", "Notes",
            "PaymentTerms", "PhoneNumber", "PostalCode", "ProviderID", "ProviderName",
            "StateOrProvince"
        }
    };

    // Maps DB column name → entity property name for each table (needed because some DB columns
    // use names like "Country/Region" that differ from the C# property name "CountryRegion").
    private static readonly Dictionary<string, Dictionary<string, string>> _columnToProperty = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Customers"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["BillingAddress"] = "BillingAddress",
            ["City"] = "City",
            ["CompanyName"] = "CompanyName",
            ["CompanyOrDepartment"] = "CompanyOrDepartment",
            ["ContactFirstName"] = "ContactFirstName",
            ["ContactLastName"] = "ContactLastName",
            ["ContactTitle"] = "ContactTitle",
            ["Country/Region"] = "CountryRegion",
            ["CustomerID"] = "CustomerID",
            ["EmailAddress"] = "EmailAddress",
            ["Extension"] = "Extension",
            ["FaxNumber"] = "FaxNumber",
            ["Notes"] = "Notes",
            ["PhoneNumber"] = "PhoneNumber",
            ["PostalCode"] = "PostalCode",
            ["StateOrProvince"] = "StateOrProvince"
        },
        ["Products"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["CategoryID"] = "CategoryID",
            ["Discontinued"] = "Discontinued",
            ["LeadTime"] = "LeadTime",
            ["ProductDescription"] = "ProductDescription",
            ["ProductID"] = "ProductID",
            ["ProductName"] = "ProductName",
            ["QuantityPerUnit"] = "QuantityPerUnit",
            ["ReorderLevel"] = "ReorderLevel",
            ["SerialNumber"] = "SerialNumber",
            ["SupplierID"] = "SupplierID",
            ["Unit"] = "Unit",
            ["UnitPrice"] = "UnitPrice",
            ["UnitsInStock"] = "UnitsInStock",
            ["UnitsOnOrder"] = "UnitsOnOrder"
        },
        ["Providers"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Address"] = "Address",
            ["City"] = "City",
            ["ContactFirstName"] = "ContactFirstName",
            ["ContactLastName"] = "ContactLastName",
            ["ContactTitle"] = "ContactTitle",
            ["Country/Region"] = "CountryRegion",
            ["EmailAddress"] = "EmailAddress",
            ["Extension"] = "Extension",
            ["FaxNumber"] = "FaxNumber",
            ["Notes"] = "Notes",
            ["PaymentTerms"] = "PaymentTerms",
            ["PhoneNumber"] = "PhoneNumber",
            ["PostalCode"] = "PostalCode",
            ["ProviderID"] = "ProviderID",
            ["ProviderName"] = "ProviderName",
            ["StateOrProvince"] = "StateOrProvince"
        }
    };

    public SearchService(
        IDbContextFactory<UpgradedDbContext> dbFactory,
        ILogger<SearchService> logger)
    {
        _dbFactory = dbFactory;
        _logger = logger;
    }

    /// <summary>
    /// Returns the list of searchable field names (DB column names) for the given table.
    /// Port of the legacy metadata iteration: rs.FieldsMetadata used to populate cboSrchBy.
    /// </summary>
    public List<string> GetSearchableFields(string table)
    {
        if (!string.IsNullOrEmpty(table) && _searchableFields.TryGetValue(table, out var fields))
            return new List<string>(fields);
        return new List<string>();
    }

    /// <summary>
    /// Port of <c>frmSearch.Search</c> (the legacy Search sub that initializes the search form state).
    /// Legacy code:
    ///   SearchTable = Table
    ///   modConnection.ExecuteSql("Select * from " &amp; Table &amp; " limit 1;")
    ///   For i = 0 To rs.FieldsMetadata.Count - 1
    ///       cboSrchBy.AddItem rs.GetField(i).FieldMetadata.ColumnName
    ///   Next i
    ///   cboSrchBy.Text = fieldToSearch
    ///   If itemToSearch &lt;&gt; "" Then Label20.Text = "Search for a " &amp; itemToSearch
    ///
    /// This method sets SearchTable, populates combo text (CboSrchByText) and label (LblSrchByText).
    /// </summary>
    public void Search(SearchStateDto state, string table, string fieldToSearch, string itemToSearch)
    {
        ArgumentNullException.ThrowIfNull(state);

        // Legacy: SearchTable = Table
        state.SearchTable = table ?? string.Empty;

        // Legacy: cboSrchBy.Text = fieldToSearch
        state.CboSrchByText = fieldToSearch ?? string.Empty;

        // Legacy: if (!String.IsNullOrEmpty(itemToSearch)) { Label20.Text = $"Search for a {itemToSearch}"; }
        if (!string.IsNullOrEmpty(itemToSearch))
        {
            state.LblSrchByText = $"Search for a {itemToSearch}";
        }

        if (!string.IsNullOrEmpty(table) && _searchableFields.ContainsKey(table))
        {
            _logger.LogInformation("Search initialized for table {Table} with field {Field}", table, fieldToSearch);
        }
        else
        {
            _logger.LogWarning("Search requested for unrecognized table {Table}", table);
        }
    }

    /// <summary>
    /// Port of <c>frmSearch.Search</c> + <c>cmdSearch_Click</c>.
    /// Initializes search context: sets the table, retrieves searchable field names (legacy: iterating
    /// rs.FieldsMetadata), sets the selected field, and builds the label text.
    /// The state DTO is returned with SearchTable, CboSrchByText (the selected combo value),
    /// and LblSrchByText (the label hint) populated.
    /// </summary>
    public async Task<SearchStateDto> SearchAsync(
        SearchStateDto state,
        string Table,
        string fieldToSearch,
        string itemToSearch,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(state);

        // Delegate to the synchronous Search method for state initialization
        Search(state, Table, fieldToSearch, itemToSearch);

        await Task.CompletedTask; // No async DB work needed for initialization
        return state;
    }

    /// <summary>
    /// Port of <c>frmSearch.SearchCriteriaCustomers</c>.
    /// Legacy code:
    ///   modConnection.ExecuteSql("Select * from Customers where " &amp; field &amp; " LIKE '" &amp; value &amp; "%'")
    ///   Do While Not rs.EOF
    ///       ' add row to list
    ///       rs.MoveNext
    ///   Loop
    /// Queries Customers where the specified field value starts with the given search value (LIKE 'value%').
    /// </summary>
    public List<CustomersDto> SearchCriteriaCustomers(string field, string value)
    {
        var propertyName = ResolvePropertyName("Customers", field);
        if (propertyName == null)
        {
            _logger.LogWarning("SearchCriteriaCustomers: unrecognized field {Field}", field);
            return new List<CustomersDto>();
        }

        using var db = _dbFactory.CreateDbContext();

        var searchValue = value ?? "%";
        var isWildcard = searchValue == "%";

        var query = db.Customers.AsNoTracking();
        query = ApplyCustomerFilter(query, propertyName, searchValue, isWildcard);

        var entities = query.ToList();

        _logger.LogInformation(
            "SearchCriteriaCustomers: field={Field}, value={Value}, results={Count}",
            field, value, entities.Count);

        return MapCustomerEntities(entities);
    }

    /// <summary>
    /// Async version of <c>SearchCriteriaCustomers</c>.
    /// Port of <c>frmSearch.SearchCriteriaCustomers</c>.
    /// Queries Customers where the specified field value starts with the given search value (LIKE 'value%').
    /// </summary>
    public async Task<List<CustomersDto>> SearchCriteriaCustomersAsync(
        string field,
        string value,
        CancellationToken ct = default)
    {
        var propertyName = ResolvePropertyName("Customers", field);
        if (propertyName == null)
        {
            _logger.LogWarning("SearchCriteriaCustomers: unrecognized field {Field}", field);
            return new List<CustomersDto>();
        }

        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var searchValue = value ?? "%";
        var isWildcard = searchValue == "%";

        var query = db.Customers.AsNoTracking();
        query = ApplyCustomerFilter(query, propertyName, searchValue, isWildcard);

        var entities = await query.ToListAsync(ct);

        _logger.LogInformation(
            "SearchCriteriaCustomers: field={Field}, value={Value}, results={Count}",
            field, value, entities.Count);

        return MapCustomerEntities(entities);
    }

    /// <summary>
    /// Port of <c>frmSearch.SearchCriteriaProducts</c>.
    /// Legacy code:
    ///   modConnection.ExecuteSql("Select * from Products where " &amp; field &amp; " LIKE '" &amp; value &amp; "%'")
    ///   Do While Not rs.EOF
    ///       ' add row to list
    ///       rs.MoveNext
    ///   Loop
    /// Queries Products where the specified field value starts with the given search value (LIKE 'value%').
    /// </summary>
    public List<ProductsDto> SearchCriteriaProducts(string field, string value)
    {
        var propertyName = ResolvePropertyName("Products", field);
        if (propertyName == null)
        {
            _logger.LogWarning("SearchCriteriaProducts: unrecognized field {Field}", field);
            return new List<ProductsDto>();
        }

        using var db = _dbFactory.CreateDbContext();

        var searchValue = value ?? "%";
        var isWildcard = searchValue == "%";

        var query = db.Products.AsNoTracking();
        query = ApplyProductFilter(query, propertyName, searchValue, isWildcard);

        var entities = query.ToList();

        _logger.LogInformation(
            "SearchCriteriaProducts: field={Field}, value={Value}, results={Count}",
            field, value, entities.Count);

        return MapProductEntities(entities);
    }

    /// <summary>
    /// Async version of <c>SearchCriteriaProducts</c>.
    /// Port of <c>frmSearch.SearchCriteriaProducts</c>.
    /// Queries Products where the specified field value starts with the given search value (LIKE 'value%').
    /// </summary>
    public async Task<List<ProductsDto>> SearchCriteriaProductsAsync(
        string field,
        string value,
        CancellationToken ct = default)
    {
        var propertyName = ResolvePropertyName("Products", field);
        if (propertyName == null)
        {
            _logger.LogWarning("SearchCriteriaProducts: unrecognized field {Field}", field);
            return new List<ProductsDto>();
        }

        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var searchValue = value ?? "%";
        var isWildcard = searchValue == "%";

        var query = db.Products.AsNoTracking();
        query = ApplyProductFilter(query, propertyName, searchValue, isWildcard);

        var entities = await query.ToListAsync(ct);

        _logger.LogInformation(
            "SearchCriteriaProducts: field={Field}, value={Value}, results={Count}",
            field, value, entities.Count);

        return MapProductEntities(entities);
    }

    /// <summary>
    /// Port of <c>frmSearch.SearchCriteriaProviders</c>.
    /// Legacy code:
    ///   modConnection.ExecuteSql("Select * from Providers where " &amp; field &amp; " LIKE '" &amp; value &amp; "%'")
    ///   Do While Not rs.EOF
    ///       ' add row to list
    ///       rs.MoveNext
    ///   Loop
    /// Queries Providers where the specified field value starts with the given search value (LIKE 'value%').
    /// </summary>
    public List<ProvidersDto> SearchCriteriaProviders(string field, string value)
    {
        var propertyName = ResolvePropertyName("Providers", field);
        if (propertyName == null)
        {
            _logger.LogWarning("SearchCriteriaProviders: unrecognized field {Field}", field);
            return new List<ProvidersDto>();
        }

        using var db = _dbFactory.CreateDbContext();

        var searchValue = value ?? "%";
        var isWildcard = searchValue == "%";

        var query = db.Providers.AsNoTracking();
        query = ApplyProviderFilter(query, propertyName, searchValue, isWildcard);

        var entities = query.ToList();

        _logger.LogInformation(
            "SearchCriteriaProviders: field={Field}, value={Value}, results={Count}",
            field, value, entities.Count);

        return MapProviderEntities(entities);
    }

    /// <summary>
    /// Async version of <c>SearchCriteriaProviders</c>.
    /// Port of <c>frmSearch.SearchCriteriaProviders</c>.
    /// Queries Providers where the specified field value starts with the given search value (LIKE 'value%').
    /// </summary>
    public async Task<List<ProvidersDto>> SearchCriteriaProvidersAsync(
        string field,
        string value,
        CancellationToken ct = default)
    {
        var propertyName = ResolvePropertyName("Providers", field);
        if (propertyName == null)
        {
            _logger.LogWarning("SearchCriteriaProviders: unrecognized field {Field}", field);
            return new List<ProvidersDto>();
        }

        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var searchValue = value ?? "%";
        var isWildcard = searchValue == "%";

        var query = db.Providers.AsNoTracking();
        query = ApplyProviderFilter(query, propertyName, searchValue, isWildcard);

        var entities = await query.ToListAsync(ct);

        _logger.LogInformation(
            "SearchCriteriaProviders: field={Field}, value={Value}, results={Count}",
            field, value, entities.Count);

        return MapProviderEntities(entities);
    }

    // ──────────────────────────────────────────────────────────────────────
    // Private helpers — mapping
    // ──────────────────────────────────────────────────────────────────────

    private static List<CustomersDto> MapCustomerEntities(List<SKS_Blazor.Domain.Entities.Core.Customers> entities)
    {
        var results = new List<CustomersDto>(entities.Count);
        foreach (var e in entities)
        {
            results.Add(new CustomersDto
            {
                CustomerID = e.CustomerID,
                BillingAddress = e.BillingAddress ?? string.Empty,
                City = e.City ?? string.Empty,
                CompanyName = e.CompanyName ?? string.Empty,
                CompanyOrDepartment = e.CompanyOrDepartment ?? string.Empty,
                ContactFirstName = e.ContactFirstName ?? string.Empty,
                ContactLastName = e.ContactLastName ?? string.Empty,
                ContactTitle = e.ContactTitle ?? string.Empty,
                CountryRegion = e.CountryRegion ?? string.Empty,
                EmailAddress = e.EmailAddress ?? string.Empty,
                Extension = e.Extension ?? string.Empty,
                FaxNumber = e.FaxNumber ?? string.Empty,
                Notes = e.Notes ?? string.Empty,
                PhoneNumber = e.PhoneNumber ?? string.Empty,
                PostalCode = e.PostalCode ?? string.Empty,
                StateOrProvince = e.StateOrProvince ?? string.Empty
            });
        }
        return results;
    }

    private static List<ProductsDto> MapProductEntities(List<SKS_Blazor.Domain.Entities.Core.Products> entities)
    {
        var results = new List<ProductsDto>(entities.Count);
        foreach (var e in entities)
        {
            results.Add(new ProductsDto
            {
                ProductID = e.ProductID ?? string.Empty,
                ProductName = e.ProductName ?? string.Empty,
                ProductDescription = e.ProductDescription ?? string.Empty,
                CategoryID = e.CategoryID,
                Discontinued = e.Discontinued,
                LeadTime = e.LeadTime ?? string.Empty,
                QuantityPerUnit = e.QuantityPerUnit,
                ReorderLevel = e.ReorderLevel,
                SerialNumber = e.SerialNumber ?? string.Empty,
                SupplierID = e.SupplierID,
                Unit = e.Unit ?? string.Empty,
                UnitPrice = e.UnitPrice,
                UnitsInStock = e.UnitsInStock,
                UnitsOnOrder = e.UnitsOnOrder
            });
        }
        return results;
    }

    private static List<ProvidersDto> MapProviderEntities(List<SKS_Blazor.Domain.Entities.Core.Providers> entities)
    {
        var results = new List<ProvidersDto>(entities.Count);
        foreach (var e in entities)
        {
            results.Add(new ProvidersDto
            {
                ProviderID = e.ProviderID,
                ProviderName = e.ProviderName ?? string.Empty,
                Address = e.Address ?? string.Empty,
                City = e.City ?? string.Empty,
                ContactFirstName = e.ContactFirstName ?? string.Empty,
                ContactLastName = e.ContactLastName ?? string.Empty,
                ContactTitle = e.ContactTitle ?? string.Empty,
                CountryRegion = e.CountryRegion ?? string.Empty,
                EmailAddress = e.EmailAddress ?? string.Empty,
                Extension = e.Extension ?? string.Empty,
                FaxNumber = e.FaxNumber ?? string.Empty,
                Notes = e.Notes ?? string.Empty,
                PaymentTerms = e.PaymentTerms ?? string.Empty,
                PhoneNumber = e.PhoneNumber ?? string.Empty,
                PostalCode = e.PostalCode ?? string.Empty,
                StateOrProvince = e.StateOrProvince ?? string.Empty
            });
        }
        return results;
    }

    // ──────────────────────────────────────────────────────────────────────
    // Private helpers — resolution and filtering
    // ──────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Resolves a user-facing DB column name (from the combo box) to the C# entity property name.
    /// Returns null if the field is not in the whitelist for the given table.
    /// </summary>
    private static string? ResolvePropertyName(string table, string field)
    {
        if (string.IsNullOrEmpty(table) || string.IsNullOrEmpty(field))
            return null;

        if (!_columnToProperty.TryGetValue(table, out var mapping))
            return null;

        if (!mapping.TryGetValue(field, out var propertyName))
            return null;

        return propertyName;
    }

    /// <summary>
    /// Applies a starts-with filter on the appropriate Customers property.
    /// Legacy: WHERE {field} LIKE '{value}%'
    /// When isWildcard is true (value == "%"), returns all rows (no filter).
    /// </summary>
    private static IQueryable<SKS_Blazor.Domain.Entities.Core.Customers> ApplyCustomerFilter(
        IQueryable<SKS_Blazor.Domain.Entities.Core.Customers> query,
        string propertyName,
        string searchValue,
        bool isWildcard)
    {
        if (isWildcard)
            return query;

        var likePattern = searchValue + "%";

        return propertyName switch
        {
            "BillingAddress" => query.Where(c => c.BillingAddress != null && EF.Functions.Like(c.BillingAddress, likePattern)),
            "City" => query.Where(c => c.City != null && EF.Functions.Like(c.City, likePattern)),
            "CompanyName" => query.Where(c => c.CompanyName != null && EF.Functions.Like(c.CompanyName, likePattern)),
            "CompanyOrDepartment" => query.Where(c => c.CompanyOrDepartment != null && EF.Functions.Like(c.CompanyOrDepartment, likePattern)),
            "ContactFirstName" => query.Where(c => c.ContactFirstName != null && EF.Functions.Like(c.ContactFirstName, likePattern)),
            "ContactLastName" => query.Where(c => c.ContactLastName != null && EF.Functions.Like(c.ContactLastName, likePattern)),
            "ContactTitle" => query.Where(c => c.ContactTitle != null && EF.Functions.Like(c.ContactTitle, likePattern)),
            "CountryRegion" => query.Where(c => c.CountryRegion != null && EF.Functions.Like(c.CountryRegion, likePattern)),
            "CustomerID" => query.Where(c => EF.Functions.Like(c.CustomerID.ToString(), likePattern)),
            "EmailAddress" => query.Where(c => c.EmailAddress != null && EF.Functions.Like(c.EmailAddress, likePattern)),
            "Extension" => query.Where(c => c.Extension != null && EF.Functions.Like(c.Extension, likePattern)),
            "FaxNumber" => query.Where(c => c.FaxNumber != null && EF.Functions.Like(c.FaxNumber, likePattern)),
            "Notes" => query.Where(c => c.Notes != null && EF.Functions.Like(c.Notes, likePattern)),
            "PhoneNumber" => query.Where(c => c.PhoneNumber != null && EF.Functions.Like(c.PhoneNumber, likePattern)),
            "PostalCode" => query.Where(c => c.PostalCode != null && EF.Functions.Like(c.PostalCode, likePattern)),
            "StateOrProvince" => query.Where(c => c.StateOrProvince != null && EF.Functions.Like(c.StateOrProvince, likePattern)),
            _ => query
        };
    }

    /// <summary>
    /// Applies a starts-with filter on the appropriate Products property.
    /// Legacy: WHERE {field} LIKE '{value}%'
    /// </summary>
    private static IQueryable<SKS_Blazor.Domain.Entities.Core.Products> ApplyProductFilter(
        IQueryable<SKS_Blazor.Domain.Entities.Core.Products> query,
        string propertyName,
        string searchValue,
        bool isWildcard)
    {
        if (isWildcard)
            return query;

        var likePattern = searchValue + "%";

        return propertyName switch
        {
            "CategoryID" => query.Where(p => p.CategoryID != null && EF.Functions.Like(p.CategoryID.Value.ToString(CultureInfo.InvariantCulture), likePattern)),
            "Discontinued" => query.Where(p => EF.Functions.Like(p.Discontinued.ToString(CultureInfo.InvariantCulture), likePattern)),
            "LeadTime" => query.Where(p => p.LeadTime != null && EF.Functions.Like(p.LeadTime, likePattern)),
            "ProductDescription" => query.Where(p => p.ProductDescription != null && EF.Functions.Like(p.ProductDescription, likePattern)),
            "ProductID" => query.Where(p => p.ProductID != null && EF.Functions.Like(p.ProductID, likePattern)),
            "ProductName" => query.Where(p => p.ProductName != null && EF.Functions.Like(p.ProductName, likePattern)),
            "QuantityPerUnit" => query.Where(p => p.QuantityPerUnit != null && EF.Functions.Like(p.QuantityPerUnit.Value.ToString(CultureInfo.InvariantCulture), likePattern)),
            "ReorderLevel" => query.Where(p => p.ReorderLevel != null && EF.Functions.Like(p.ReorderLevel.Value.ToString(CultureInfo.InvariantCulture), likePattern)),
            "SerialNumber" => query.Where(p => p.SerialNumber != null && EF.Functions.Like(p.SerialNumber, likePattern)),
            "SupplierID" => query.Where(p => p.SupplierID != null && EF.Functions.Like(p.SupplierID.Value.ToString(CultureInfo.InvariantCulture), likePattern)),
            "Unit" => query.Where(p => p.Unit != null && EF.Functions.Like(p.Unit, likePattern)),
            "UnitPrice" => query.Where(p => p.UnitPrice != null && EF.Functions.Like(p.UnitPrice.Value.ToString(CultureInfo.InvariantCulture), likePattern)),
            "UnitsInStock" => query.Where(p => p.UnitsInStock != null && EF.Functions.Like(p.UnitsInStock.Value.ToString(CultureInfo.InvariantCulture), likePattern)),
            "UnitsOnOrder" => query.Where(p => p.UnitsOnOrder != null && EF.Functions.Like(p.UnitsOnOrder.Value.ToString(CultureInfo.InvariantCulture), likePattern)),
            _ => query
        };
    }

    /// <summary>
    /// Applies a starts-with filter on the appropriate Providers property.
    /// Legacy: WHERE {field} LIKE '{value}%'
    /// </summary>
    private static IQueryable<SKS_Blazor.Domain.Entities.Core.Providers> ApplyProviderFilter(
        IQueryable<SKS_Blazor.Domain.Entities.Core.Providers> query,
        string propertyName,
        string searchValue,
        bool isWildcard)
    {
        if (isWildcard)
            return query;

        var likePattern = searchValue + "%";

        return propertyName switch
        {
            "Address" => query.Where(p => p.Address != null && EF.Functions.Like(p.Address, likePattern)),
            "City" => query.Where(p => p.City != null && EF.Functions.Like(p.City, likePattern)),
            "ContactFirstName" => query.Where(p => p.ContactFirstName != null && EF.Functions.Like(p.ContactFirstName, likePattern)),
            "ContactLastName" => query.Where(p => p.ContactLastName != null && EF.Functions.Like(p.ContactLastName, likePattern)),
            "ContactTitle" => query.Where(p => p.ContactTitle != null && EF.Functions.Like(p.ContactTitle, likePattern)),
            "CountryRegion" => query.Where(p => p.CountryRegion != null && EF.Functions.Like(p.CountryRegion, likePattern)),
            "EmailAddress" => query.Where(p => p.EmailAddress != null && EF.Functions.Like(p.EmailAddress, likePattern)),
            "Extension" => query.Where(p => p.Extension != null && EF.Functions.Like(p.Extension, likePattern)),
            "FaxNumber" => query.Where(p => p.FaxNumber != null && EF.Functions.Like(p.FaxNumber, likePattern)),
            "Notes" => query.Where(p => p.Notes != null && EF.Functions.Like(p.Notes, likePattern)),
            "PaymentTerms" => query.Where(p => p.PaymentTerms != null && EF.Functions.Like(p.PaymentTerms, likePattern)),
            "PhoneNumber" => query.Where(p => p.PhoneNumber != null && EF.Functions.Like(p.PhoneNumber, likePattern)),
            "PostalCode" => query.Where(p => p.PostalCode != null && EF.Functions.Like(p.PostalCode, likePattern)),
            "ProviderID" => query.Where(p => EF.Functions.Like(p.ProviderID.ToString(CultureInfo.InvariantCulture), likePattern)),
            "ProviderName" => query.Where(p => p.ProviderName != null && EF.Functions.Like(p.ProviderName, likePattern)),
            "StateOrProvince" => query.Where(p => p.StateOrProvince != null && EF.Functions.Like(p.StateOrProvince, likePattern)),
            _ => query
        };
    }
}
