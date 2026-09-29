using System.Collections.Specialized;
using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SKS_Blazor.Infrastructure.Data;
using SKS_Blazor.Domain.Entities.Core;
using SKS_Blazor.Domain.Entities.Order;
using SKS_Blazor.Domain.Entities.Manual;
using SKS_Blazor.Domain.Entities.Invoice;

namespace SKS_Blazor.Application.Services.Functions;

/// <summary>Port of <c>modFunctions</c> — shared utility functions used across forms.</summary>
public class FunctionsService : IFunctionsService
{
    private readonly IDbContextFactory<UpgradedDbContext> _dbFactory;
    private readonly ILogger<FunctionsService> _logger;

    public FunctionsService(
        IDbContextFactory<UpgradedDbContext> dbFactory,
        ILogger<FunctionsService> logger)
    {
        _dbFactory = dbFactory;
        _logger = logger;
    }

    /// <summary>
    /// Port of <c>modFunctions.AddToCollection</c>.
    /// Adds Item to col keyed by Item if not already present. Returns true if added.
    /// Legacy: Public Function AddToCollection(ByRef col As OrderedDictionary, ByVal Item As String) As Boolean
    /// </summary>
    public async Task<bool> AddToCollectionAsync(OrderedDictionary col, string Item, CancellationToken ct = default)
    {
        await Task.CompletedTask;
        bool result = false;
        if (!Exists(col, Item))
        {
            col.Add(Item, Item);
            result = true;
        }
        return result;
    }

    /// <summary>
    /// Port of <c>modFunctions.UpCase</c>.
    /// Converts lowercase ASCII (97-122) to uppercase (65-90). Other codes pass through.
    /// Legacy: Public Function UpCase(ByVal KeyAscii As Integer) As Integer
    /// </summary>
    public async Task<int> UpCaseAsync(int KeyAscii, CancellationToken ct = default)
    {
        await Task.CompletedTask;
        // Legacy: If KeyAscii >= 97 And KeyAscii <= 122 Then UpCase = KeyAscii - 32
        if (KeyAscii >= 97 && KeyAscii <= 122)
        {
            return KeyAscii - 32;
        }
        return KeyAscii;
    }

    /// <summary>
    /// Port of <c>modFunctions.RcrdId</c>.
    /// Generates next record ID by querying max of FldNo in Table, incrementing by 1.
    /// If Identifier is non-empty, returns "{Identifier}{RcrdNo}{MM}".
    /// Otherwise returns "{RcrdNo}".
    /// Legacy: Public Function RcrdId(ByVal Table As String, ByVal Identifier As String, ByVal FldNo As String) As String
    /// </summary>
    public async Task<string?> RcrdIdAsync(string Table, string Identifier, string FldNo, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        int rcrdNo = 0;

        // The legacy code does: SELECT * FROM {Table} ORDER BY {FldNo} ASC, then MoveLast
        // to get the max value of FldNo, then adds 1.
        decimal? maxValue = await GetMaxFieldValueAsync(db, Table, FldNo, ct);

        if (maxValue.HasValue)
        {
            rcrdNo = (int)(maxValue.Value + 1);
        }
        else
        {
            rcrdNo = 1;
        }

        // Legacy: If Identifier is not empty, return "{Identifier}{RcrdNo}{MM}"
        if (!string.IsNullOrEmpty(Identifier))
        {
            return $"{Identifier}{rcrdNo}{DateTime.Today.ToString("MM", CultureInfo.InvariantCulture)}";
        }
        else
        {
            return rcrdNo.ToString(CultureInfo.InvariantCulture);
        }
    }

    /// <summary>
    /// Port of <c>modFunctions.ExecErr</c>.
    /// Builds error message with related records from Table where RcrdFld = RcrdStr.
    /// The PromptFld column values are concatenated with "; " separator.
    /// If Table is empty, returns plain Prompt.
    /// Legacy: Public Function ExecErr(ByVal Prompt As String, ByVal PromptFld As String, ByVal Table As String, ByVal RcrdFld As String, ByVal RcrdStr As String) As String
    /// </summary>
    public async Task<string?> ExecErrAsync(string Prompt, string PromptFld, string Table, string RcrdFld, string RcrdStr, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(Table))
        {
            return Prompt;
        }

        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        // Query related records from the specified table where RcrdFld = RcrdStr,
        // then concatenate the PromptFld values.
        var relatedValues = await GetRelatedFieldValuesAsync(db, Table, RcrdFld, RcrdStr, PromptFld, ct);

        var rcrds = new StringBuilder();
        foreach (var val in relatedValues)
        {
            rcrds.Append($"{val}; ");
        }

        if (rcrds.Length > 0)
        {
            return $"Error: {Prompt}{Environment.NewLine}{Environment.NewLine}" +
                   $"Related Records: {rcrds}";
        }
        else
        {
            return $"Error: {Prompt}";
        }
    }

    /// <summary>
    /// Port of <c>modFunctions.AppendAND</c>.
    /// Appends " AND " to the criteria string if it already has content.
    /// Legacy: Public Function AppendAND(ByVal criteria As String) As String
    ///   If Len(criteria) &gt; 0 Then AppendAND = criteria &amp; " AND " Else AppendAND = ""
    /// </summary>
    public string AppendAND(string criteria)
    {
        if (!string.IsNullOrEmpty(criteria))
        {
            return criteria + " AND ";
        }
        return string.Empty;
    }

    /// <summary>
    /// Port of <c>modFunctions.Exists</c>.
    /// Checks if the given key Index exists in the OrderedDictionary.
    /// Legacy: Public Function Exists(ByRef col As OrderedDictionary, ByVal Index As String) As Boolean
    ///   tries col[Index], if null then false
    /// </summary>
    public bool Exists(OrderedDictionary col, string Index)
    {
        if (col == null) return false;
        try
        {
            object? o = col[Index];
            return o is not null;
        }
        catch
        {
            // Legacy code swallows this exception
            return false;
        }
    }

    /// <summary>
    /// Port of <c>modFunctions.DoubleValue</c>.
    /// Parses string to double; returns 0 if empty or invalid.
    /// Legacy: Public Function DoubleValue(ByVal strValue As String) As Double
    ///   If Len(strValue) &lt;&gt; 0 Then DoubleValue = CDbl(strValue) Else DoubleValue = 0
    /// </summary>
    public double DoubleValue(string strValue)
    {
        if (!string.IsNullOrEmpty(strValue))
        {
            if (double.TryParse(strValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed))
            {
                return parsed;
            }
        }
        return 0d;
    }

    /// <summary>
    /// Port of <c>modFunctions.ValidateTextBoxDouble</c>.
    /// Validates that a text value is a valid double (or empty). Returns true if valid.
    /// Legacy: Public Function ValidateTextBoxDouble(ByVal text As String) As Boolean
    ///   If Len(text) = 0 Then return True; try CDbl(text), if ok return True else False
    /// </summary>
    public bool ValidateTextBoxDouble(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return true;
        }
        return double.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out _);
    }

    /// <summary>
    /// Port of <c>modFunctions.ValidateTextDouble</c>.
    /// Validates that a text value is a valid double. Returns true if valid, false if not parseable.
    /// Legacy: Public Function ValidateTextDouble(ByVal text As String) As Boolean
    ///   try CDbl(text), if ok return True else False
    /// </summary>
    public bool ValidateTextDouble(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return false;
        }
        return double.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out _);
    }

    /// <summary>
    /// Port of <c>modFunctions.SelectAll</c>.
    /// Returns "SELECT * FROM {table}" string used by callers that build dynamic criteria.
    /// Legacy: Public Function SelectAll(ByVal table As String) As String
    ///   SelectAll = "SELECT * FROM " &amp; table
    /// </summary>
    public string SelectAll(string table)
    {
        return $"SELECT * FROM {table}";
    }

    /// <summary>
    /// Port of <c>modFunctions.LoadCombo</c>.
    /// Loads distinct values of a field from a table into a list for combo/select population.
    /// Legacy: Public Sub LoadCombo(ByRef cmb As ComboBox, ByVal table As String, ByVal field As String)
    ///   Opens recordset "SELECT DISTINCT {field} FROM {table} ORDER BY {field}"
    ///   Iterates records and adds each to combo
    /// </summary>
    public async Task<List<string>> LoadComboAsync(string table, string field, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var tableLower = table.Trim().ToLowerInvariant();
        var fieldLower = field.Trim().ToLowerInvariant();
        var result = new List<string>();

        // Map known tables to EF Core queries, selecting distinct values of the specified field
        switch (tableLower)
        {
            case "categories":
            {
                var entities = await db.Categories.AsNoTracking().ToListAsync(ct);
                result = ProjectDistinctField(entities, field);
                break;
            }
            case "customers":
            {
                var entities = await db.Customers.AsNoTracking().ToListAsync(ct);
                result = ProjectDistinctField(entities, field);
                break;
            }
            case "products":
            {
                var entities = await db.Products.AsNoTracking().ToListAsync(ct);
                result = ProjectDistinctField(entities, field);
                break;
            }
            case "providers":
            {
                var entities = await db.Providers.AsNoTracking().ToListAsync(ct);
                result = ProjectDistinctField(entities, field);
                break;
            }
            case "levels":
            {
                var entities = await db.Levels.AsNoTracking().ToListAsync(ct);
                result = ProjectDistinctField(entities, field);
                break;
            }
            case "users":
            {
                var entities = await db.Users.AsNoTracking().ToListAsync(ct);
                result = ProjectDistinctField(entities, field);
                break;
            }
            case "warehouses":
            {
                var entities = await db.Warehouses.AsNoTracking().ToListAsync(ct);
                result = ProjectDistinctField(entities, field);
                break;
            }
            case "stocks":
            {
                var entities = await db.Stocks.AsNoTracking().ToListAsync(ct);
                result = ProjectDistinctField(entities, field);
                break;
            }
            case "invoices":
            {
                var entities = await db.Invoices.AsNoTracking().ToListAsync(ct);
                result = ProjectDistinctField(entities, field);
                break;
            }
            case "orderreceptions":
            {
                var entities = await db.OrderReceptions.AsNoTracking().ToListAsync(ct);
                result = ProjectDistinctField(entities, field);
                break;
            }
            case "orderrequests":
            {
                var entities = await db.OrderRequests.AsNoTracking().ToListAsync(ct);
                result = ProjectDistinctField(entities, field);
                break;
            }
            case "manualstocks":
            {
                var entities = await db.ManualStocks.AsNoTracking().ToListAsync(ct);
                result = ProjectDistinctField(entities, field);
                break;
            }
            default:
                _logger.LogWarning("LoadCombo called with unknown table '{Table}'. Returning empty list.", table);
                break;
        }

        result.Sort(StringComparer.OrdinalIgnoreCase);
        return result;
    }

    /// <summary>
    /// Port of <c>modFunctions.ComboEmpty</c>.
    /// Checks if a combo selection text is empty or null.
    /// Legacy: Public Function ComboEmpty(ByVal cmbText As String) As Boolean
    ///   ComboEmpty = (Len(Trim(cmbText)) = 0)
    /// </summary>
    public bool ComboEmpty(string? comboText)
    {
        return string.IsNullOrWhiteSpace(comboText);
    }

    /// <summary>
    /// Port of <c>modFunctions.NoRecords</c>.
    /// Returns a standard "No records found" message.
    /// Legacy: Public Function NoRecords() As String
    ///   NoRecords = "No records found."
    /// </summary>
    public string NoRecords()
    {
        return "No records found.";
    }

    /// <summary>
    /// Port of <c>modFunctions.SearchShow</c>.
    /// Builds a search criteria SQL fragment for the given field, value, and search type.
    /// Legacy: Public Function SearchShow(ByVal field As String, ByVal value As String, ByVal searchType As String) As String
    ///   Select Case searchType:
    ///     "Equals" → "{field} = '{value}'"
    ///     "Starts With" → "{field} LIKE '{value}%'"
    ///     "Contains" → "{field} LIKE '%{value}%'"
    /// </summary>
    public string SearchShow(string field, string value, string searchType)
    {
        if (string.IsNullOrEmpty(field) || string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        // Sanitize value to prevent SQL injection (escape single quotes)
        var safeValue = value.Replace("'", "''");

        return searchType switch
        {
            "Equals" => $"{field} = '{safeValue}'",
            "Starts With" => $"{field} LIKE '{safeValue}%'",
            "Contains" => $"{field} LIKE '%{safeValue}%'",
            _ => $"{field} LIKE '%{safeValue}%'"
        };
    }

    /// <summary>
    /// Port of <c>modFunctions.ValBox</c>.
    /// Validates that a text value is not empty. Returns error message if empty, or empty string if valid.
    /// Legacy: Public Function ValBox(ByVal text As String, ByVal fieldName As String) As String
    ///   If Len(Trim(text)) = 0 Then ValBox = fieldName &amp; " is required." Else ValBox = ""
    /// </summary>
    public string ValBox(string text, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return $"{fieldName} is required.";
        }
        return string.Empty;
    }

    /// <summary>
    /// Port of <c>modFunctions.TextBoxEmpty</c>.
    /// Checks if a text field is empty or whitespace.
    /// Legacy: Public Function TextBoxEmpty(ByVal text As String) As Boolean
    ///   TextBoxEmpty = (Len(Trim(text)) = 0)
    /// </summary>
    public bool TextBoxEmpty(string? text)
    {
        return string.IsNullOrWhiteSpace(text);
    }

    /// <summary>
    /// Port of <c>modFunctions.TextBoxNumberEmpty</c>.
    /// Checks if a numeric text field is empty, whitespace, or zero.
    /// Legacy: Public Function TextBoxNumberEmpty(ByVal text As String) As Boolean
    ///   If Len(Trim(text)) = 0 Then return True
    ///   If CDbl(text) = 0 Then return True Else return False
    /// </summary>
    public bool TextBoxNumberEmpty(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return true;
        }
        if (double.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out var val))
        {
            return val == 0d;
        }
        return true;
    }

    // ─── Dynamic table query helpers ───────────────────────────────────────
    // The legacy code used raw SQL with dynamic table/column names.
    // Since user decision says "no raw SQL", we map known table names to EF Core queries.
    // This is a finite set — the application only references these tables.

    /// <summary>
    /// Gets the maximum numeric value of the specified field from the specified table.
    /// Replicates legacy: SELECT * FROM {Table} ORDER BY {FldNo} ASC; MoveLast → rs[FldNo]
    /// </summary>
    private async Task<decimal?> GetMaxFieldValueAsync(UpgradedDbContext db, string table, string fldNo, CancellationToken ct)
    {
        var tableLower = table.Trim().ToLowerInvariant();

        switch (tableLower)
        {
            case "categories":
            {
                var maxVal = await db.Categories.AsNoTracking()
                    .Select(e => (decimal?)e.CategoryID)
                    .MaxAsync(ct);
                return maxVal;
            }
            case "customers":
            {
                var maxVal = await db.Customers.AsNoTracking()
                    .Select(e => (decimal?)e.CustomerID)
                    .MaxAsync(ct);
                return maxVal;
            }
            case "invoicedetails":
            {
                var maxVal = await db.InvoiceDetails.AsNoTracking()
                    .Select(e => (decimal?)e.InvoiceDetailID)
                    .MaxAsync(ct);
                return maxVal;
            }
            case "invoices":
            {
                var maxVal = await db.Invoices.AsNoTracking()
                    .Select(e => (decimal?)e.InvoiceID)
                    .MaxAsync(ct);
                return maxVal;
            }
            case "manualstocks":
            {
                var maxVal = await db.ManualStocks.AsNoTracking()
                    .Select(e => (decimal?)e.ManualID)
                    .MaxAsync(ct);
                return maxVal;
            }
            case "orderreceptiondetails":
            {
                var maxVal = await db.OrderReceptionDetails.AsNoTracking()
                    .Select(e => (decimal?)e.OrderDetailID)
                    .MaxAsync(ct);
                return maxVal;
            }
            case "orderreceptions":
            {
                var maxVal = await db.OrderReceptions.AsNoTracking()
                    .Select(e => (decimal?)e.OrderID)
                    .MaxAsync(ct);
                return maxVal;
            }
            case "orderrequestdetails":
            {
                var maxVal = await db.OrderRequestDetails.AsNoTracking()
                    .Select(e => (decimal?)e.OrderDetailID)
                    .MaxAsync(ct);
                return maxVal;
            }
            case "orderrequests":
            {
                var maxVal = await db.OrderRequests.AsNoTracking()
                    .Select(e => (decimal?)e.OrderID)
                    .MaxAsync(ct);
                return maxVal;
            }
            case "products":
            {
                // ProductID is string — legacy code did Convert.ToDouble(rs[FldNo]) + 1
                // We parse each ProductID as decimal, take max
                var allIds = await db.Products.AsNoTracking()
                    .Select(e => e.ProductID)
                    .ToListAsync(ct);
                decimal? maxVal = null;
                foreach (var id in allIds)
                {
                    if (!string.IsNullOrEmpty(id) &&
                        decimal.TryParse(id, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed))
                    {
                        if (!maxVal.HasValue || parsed > maxVal.Value)
                        {
                            maxVal = parsed;
                        }
                    }
                }
                return maxVal;
            }
            case "productsbycustomer":
            {
                var maxVal = await db.ProductsByCustomers.AsNoTracking()
                    .Select(e => (decimal?)e.ID)
                    .MaxAsync(ct);
                return maxVal;
            }
            case "productsbyprovider":
            {
                var maxVal = await db.ProductsByProviders.AsNoTracking()
                    .Select(e => (decimal?)e.ID)
                    .MaxAsync(ct);
                return maxVal;
            }
            case "providers":
            {
                var maxVal = await db.Providers.AsNoTracking()
                    .Select(e => (decimal?)e.ProviderID)
                    .MaxAsync(ct);
                return maxVal;
            }
            case "stocklog":
            {
                var maxVal = await db.StockLogs.AsNoTracking()
                    .Select(e => (decimal?)e.ID)
                    .MaxAsync(ct);
                return maxVal;
            }
            case "stocks":
            {
                var maxVal = await db.Stocks.AsNoTracking()
                    .Select(e => (decimal?)e.StockID)
                    .MaxAsync(ct);
                return maxVal;
            }
            case "users":
            {
                // Username is string PK — not numeric. Legacy code would fail here too.
                _logger.LogWarning("RcrdId called on Users table which has non-numeric PK (Username). Returning null.");
                return null;
            }
            case "warehouses":
            {
                var maxVal = await db.Warehouses.AsNoTracking()
                    .Select(e => (decimal?)e.WarehouseID)
                    .MaxAsync(ct);
                return maxVal;
            }
            case "levels":
            {
                // Level is string PK — not numeric.
                _logger.LogWarning("RcrdId called on Levels table which has non-numeric PK (Level). Returning null.");
                return null;
            }
            default:
                _logger.LogWarning("RcrdId called with unknown table '{Table}'. Returning null.", table);
                return null;
        }
    }

    /// <summary>
    /// Gets values of PromptFld from Table where RcrdFld = RcrdStr.
    /// Replicates legacy: SELECT * FROM {Table} WHERE {RcrdFld} = '{RcrdStr}'
    /// then iterating rs[PromptFld].
    /// </summary>
    private async Task<List<string>> GetRelatedFieldValuesAsync(
        UpgradedDbContext db, string table, string rcrdFld, string rcrdStr,
        string promptFld, CancellationToken ct)
    {
        var tableLower = table.Trim().ToLowerInvariant();
        var result = new List<string>();

        switch (tableLower)
        {
            case "categories":
            {
                var entities = await db.Categories.AsNoTracking().ToListAsync(ct);
                result = FilterAndProject(entities, rcrdFld, rcrdStr, promptFld);
                break;
            }
            case "customers":
            {
                var entities = await db.Customers.AsNoTracking().ToListAsync(ct);
                result = FilterAndProject(entities, rcrdFld, rcrdStr, promptFld);
                break;
            }
            case "invoicedetails":
            {
                var entities = await db.InvoiceDetails.AsNoTracking().ToListAsync(ct);
                result = FilterAndProject(entities, rcrdFld, rcrdStr, promptFld);
                break;
            }
            case "invoices":
            {
                var entities = await db.Invoices.AsNoTracking().ToListAsync(ct);
                result = FilterAndProject(entities, rcrdFld, rcrdStr, promptFld);
                break;
            }
            case "manualstocks":
            {
                var entities = await db.ManualStocks.AsNoTracking().ToListAsync(ct);
                result = FilterAndProject(entities, rcrdFld, rcrdStr, promptFld);
                break;
            }
            case "orderreceptiondetails":
            {
                var entities = await db.OrderReceptionDetails.AsNoTracking().ToListAsync(ct);
                result = FilterAndProject(entities, rcrdFld, rcrdStr, promptFld);
                break;
            }
            case "orderreceptions":
            {
                var entities = await db.OrderReceptions.AsNoTracking().ToListAsync(ct);
                result = FilterAndProject(entities, rcrdFld, rcrdStr, promptFld);
                break;
            }
            case "orderrequestdetails":
            {
                var entities = await db.OrderRequestDetails.AsNoTracking().ToListAsync(ct);
                result = FilterAndProject(entities, rcrdFld, rcrdStr, promptFld);
                break;
            }
            case "orderrequests":
            {
                var entities = await db.OrderRequests.AsNoTracking().ToListAsync(ct);
                result = FilterAndProject(entities, rcrdFld, rcrdStr, promptFld);
                break;
            }
            case "products":
            {
                var entities = await db.Products.AsNoTracking().ToListAsync(ct);
                result = FilterAndProject(entities, rcrdFld, rcrdStr, promptFld);
                break;
            }
            case "productsbycustomer":
            {
                var entities = await db.ProductsByCustomers.AsNoTracking().ToListAsync(ct);
                result = FilterAndProject(entities, rcrdFld, rcrdStr, promptFld);
                break;
            }
            case "productsbyprovider":
            {
                var entities = await db.ProductsByProviders.AsNoTracking().ToListAsync(ct);
                result = FilterAndProject(entities, rcrdFld, rcrdStr, promptFld);
                break;
            }
            case "providers":
            {
                var entities = await db.Providers.AsNoTracking().ToListAsync(ct);
                result = FilterAndProject(entities, rcrdFld, rcrdStr, promptFld);
                break;
            }
            case "stocklog":
            {
                var entities = await db.StockLogs.AsNoTracking().ToListAsync(ct);
                result = FilterAndProject(entities, rcrdFld, rcrdStr, promptFld);
                break;
            }
            case "stocks":
            {
                var entities = await db.Stocks.AsNoTracking().ToListAsync(ct);
                result = FilterAndProject(entities, rcrdFld, rcrdStr, promptFld);
                break;
            }
            case "users":
            {
                var entities = await db.Users.AsNoTracking().ToListAsync(ct);
                result = FilterAndProject(entities, rcrdFld, rcrdStr, promptFld);
                break;
            }
            case "warehouses":
            {
                var entities = await db.Warehouses.AsNoTracking().ToListAsync(ct);
                result = FilterAndProject(entities, rcrdFld, rcrdStr, promptFld);
                break;
            }
            case "levels":
            {
                var entities = await db.Levels.AsNoTracking().ToListAsync(ct);
                result = FilterAndProject(entities, rcrdFld, rcrdStr, promptFld);
                break;
            }
            default:
                _logger.LogWarning("ExecErr called with unknown table '{Table}'. Returning empty related records.", table);
                break;
        }

        return result;
    }

    /// <summary>
    /// Reflects on entity instances to filter by a column value and project another column.
    /// The legacy code used rs[fieldName] with dynamic field access — this replicates that
    /// using reflection since the caller passes column names as strings.
    /// </summary>
    private static List<string> FilterAndProject<TEntity>(
        List<TEntity> entities, string filterField, string filterValue, string projectField)
    {
        var result = new List<string>();
        if (entities.Count == 0) return result;

        var entityType = typeof(TEntity);

        // Find properties matching the field names (case-insensitive).
        var filterProp = entityType.GetProperties()
            .FirstOrDefault(p => string.Equals(p.Name, filterField, StringComparison.OrdinalIgnoreCase));
        var projectProp = entityType.GetProperties()
            .FirstOrDefault(p => string.Equals(p.Name, projectField, StringComparison.OrdinalIgnoreCase));

        if (filterProp is null || projectProp is null)
        {
            // Also try matching against DB column names which may differ
            // (e.g., "Country/Region" → property CountryRegion).
            static string Normalize(string s) => s.Replace("/", "").Replace(" ", "").ToLowerInvariant();

            filterProp ??= entityType.GetProperties()
                .FirstOrDefault(p => Normalize(p.Name) == Normalize(filterField));
            projectProp ??= entityType.GetProperties()
                .FirstOrDefault(p => Normalize(p.Name) == Normalize(projectField));
        }

        if (filterProp is null || projectProp is null)
        {
            return result;
        }

        foreach (var entity in entities)
        {
            var fieldVal = filterProp.GetValue(entity);
            var fieldStr = Convert.ToString(fieldVal, CultureInfo.InvariantCulture) ?? string.Empty;

            if (string.Equals(fieldStr, filterValue, StringComparison.OrdinalIgnoreCase))
            {
                var projVal = projectProp.GetValue(entity);
                var projStr = Convert.ToString(projVal, CultureInfo.InvariantCulture) ?? string.Empty;
                result.Add(projStr);
            }
        }

        return result;
    }

    /// <summary>
    /// Projects distinct non-empty values of a named field from a list of entities using reflection.
    /// Used by LoadComboAsync to replicate legacy "SELECT DISTINCT {field} FROM {table}".
    /// </summary>
    private static List<string> ProjectDistinctField<TEntity>(List<TEntity> entities, string field)
    {
        var result = new List<string>();
        if (entities.Count == 0) return result;

        var entityType = typeof(TEntity);

        var prop = entityType.GetProperties()
            .FirstOrDefault(p => string.Equals(p.Name, field, StringComparison.OrdinalIgnoreCase));

        if (prop is null)
        {
            // Try normalized matching for "Country/Region" style columns
            static string Normalize(string s) => s.Replace("/", "").Replace(" ", "").ToLowerInvariant();
            prop = entityType.GetProperties()
                .FirstOrDefault(p => Normalize(p.Name) == Normalize(field));
        }

        if (prop is null)
        {
            return result;
        }

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var entity in entities)
        {
            var val = prop.GetValue(entity);
            var str = Convert.ToString(val, CultureInfo.InvariantCulture) ?? string.Empty;
            if (!string.IsNullOrEmpty(str) && seen.Add(str))
            {
                result.Add(str);
            }
        }

        return result;
    }
}
