using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;
using SKS_Blazor.Application.DTOs.Search;
using SKS_Blazor.Application.DTOs.Core;
using SKS_Blazor.Application.Services.Search;
using SKS_Blazor.Infrastructure;
using SKS_Blazor.Pages.Main;
using SKS_Blazor.Pages.Customers;
using SKS_Blazor.Pages.Products;
using SKS_Blazor.Pages.Providers;

namespace SKS_Blazor.Pages.Search;

/// <summary>Port of <c>frmSearch</c> (frmSearch.cs).</summary>
public partial class Search : ComponentBase, IDisposable
{
    [Inject] private ISearchService SearchService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private AppState AppState { get; set; } = default!;
    [Inject] private IDialogService DialogService { get; set; } = default!;

    /// <summary>
    /// Parameters passed from the caller (e.g., Main page menu item).
    /// Legacy: Search(Table, fieldToSearch, itemToSearch) was called by modFunctions.SearchShow.
    /// </summary>
    [SupplyParameterFromQuery(Name = "table")]
    public string? TableParam { get; set; }

    [SupplyParameterFromQuery(Name = "field")]
    public string? FieldParam { get; set; }

    [SupplyParameterFromQuery(Name = "item")]
    public string? ItemParam { get; set; }

    // Shared state — single instance for page lifetime
    private SearchStateDto? _state;

    // UI fields
    private string _titleText = "Search";
    private string _selectedField = string.Empty;
    private string _searchString = string.Empty;
    private string _statusMessage = string.Empty;
    private Severity _statusSeverity = Severity.Info;
    private bool _isLoading;
    private List<string>? _fieldNames;

    // Track query parameter state to avoid re-loading on every render
    private string? _previousTable;
    private string? _previousField;
    private string? _previousItem;

    // ── Lifecycle ──────────────────────────────────────────────────────

    protected override Task OnInitializedAsync()
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Port of frmSearch_Activated + Form_Load + Search(Table, fieldToSearch, itemToSearch).
    /// Runs when parameters change (including query string changes).
    /// </summary>
    public override async Task SetParametersAsync(ParameterView parameters)
    {
        await base.SetParametersAsync(parameters);

        // Guard: only re-initialize if query string actually changed
        if (TableParam != _previousTable || FieldParam != _previousField || ItemParam != _previousItem)
        {
            _previousTable = TableParam;
            _previousField = FieldParam;
            _previousItem = ItemParam;

            _state = new SearchStateDto
            {
                SearchTable = TableParam ?? string.Empty,
                CboSrchByText = FieldParam ?? string.Empty,
                LblSrchByText = FieldParam ?? string.Empty
            };

            // Port of Search(Table, fieldToSearch, itemToSearch)
            await InitializeSearchAsync(
                _state.SearchTable,
                _state.CboSrchByText,
                ItemParam ?? string.Empty);
        }
    }

    /// <summary>
    /// Port of Search(string Table, string fieldToSearch, string itemToSearch).
    /// Legacy: sets title, queries table metadata to populate cboSrchBy combo items,
    /// then sets cboSrchBy.Text = fieldToSearch.
    /// </summary>
    private async Task InitializeSearchAsync(string table, string fieldToSearch, string itemToSearch)
    {
        // Legacy: If Not IsEmpty(itemToSearch) Then Label20.Text = "Search for a " & itemToSearch
        if (!string.IsNullOrEmpty(itemToSearch))
        {
            _titleText = $"Search for a {itemToSearch}";
        }

        // Delegate to service — it queries the table schema and populates state
        _state = await SearchService.SearchAsync(_state!, table, fieldToSearch, itemToSearch);

        // Build field names list from the state for the MudSelect
        _fieldNames = BuildFieldNamesForTable(table);

        // Set default selected field
        _selectedField = fieldToSearch;
        if (_state is not null)
        {
            _state.CboSrchByText = fieldToSearch;
            _state.LblSrchByText = fieldToSearch;
        }
    }

    /// <summary>
    /// Builds the list of searchable field names for the given table.
    /// Legacy code iterated rs.FieldsMetadata.Count and extracted ColumnName for each field.
    /// </summary>
    private static List<string> BuildFieldNamesForTable(string table)
    {
        return table switch
        {
            "Customers" => new List<string>
            {
                "CustomerID", "CompanyName", "ContactFirstName", "ContactLastName",
                "ContactTitle", "BillingAddress", "City", "StateOrProvince",
                "PostalCode", "Country/Region", "CompanyOrDepartment",
                "PhoneNumber", "FaxNumber", "Extension", "EmailAddress", "Notes"
            },
            "Products" => new List<string>
            {
                "ProductID", "ProductName", "ProductDescription", "SerialNumber",
                "CategoryID", "SupplierID", "QuantityPerUnit", "UnitPrice",
                "UnitsInStock", "UnitsOnOrder", "ReorderLevel", "Discontinued",
                "LeadTime", "Unit"
            },
            "Providers" => new List<string>
            {
                "ProviderID", "ProviderName", "ContactFirstName", "ContactLastName",
                "ContactTitle", "Address", "City", "StateOrProvince",
                "PostalCode", "Country/Region", "PhoneNumber", "FaxNumber",
                "Extension", "EmailAddress", "PaymentTerms", "Notes"
            },
            _ => new List<string>()
        };
    }

    // ── Event Handlers ──────────────────────────────────────────────────

    /// <summary>
    /// Port of cboSrchBy_SelectedIndexChanged (1 line).
    /// Legacy: lblSrchBy.Text = cboSrchBy.Text.
    /// </summary>
    private Task OnSearchByChanged()
    {
        if (_state is not null)
        {
            _state.LblSrchByText = _selectedField;
            _state.CboSrchByText = _selectedField;
        }
        return Task.CompletedTask;
    }

    /// <summary>
    /// Allow pressing Enter in the search textbox to trigger search.
    /// </summary>
    private async Task OnSearchKeyDown(KeyboardEventArgs args)
    {
        if (args.Key == "Enter")
        {
            await OnSearchClick();
        }
    }

    /// <summary>
    /// Port of cmdSearch_Click (29 lines of legacy logic).
    /// Legacy: sanitizes input, determines table, calls SearchCriteriaCustomers/Products/Providers.
    /// </summary>
    private async Task OnSearchClick()
    {
        if (_state is null) return;

        _isLoading = true;
        _statusMessage = string.Empty;
        StateHasChanged();

        try
        {
            // Legacy: If txtSrchStr.Text.Substring(Max(len-1, 0)) == "'" Then txtSrchStr.Text = ""
            if (!string.IsNullOrEmpty(_searchString) &&
                _searchString[^1] == '\'')
            {
                _searchString = string.Empty;
            }

            string txtToSearch;

            // Legacy: If Not IsEmpty(Trim(txtSrchStr.Text)) Then txtToSearch = txtSrchStr.Text Else txtToSearch = "%"
            if (!string.IsNullOrEmpty(_searchString?.Trim()))
            {
                txtToSearch = _searchString!;
            }
            else
            {
                txtToSearch = "%";
            }

            var field = _state.LblSrchByText ?? string.Empty;

            // Legacy dispatches by SearchTable
            if (_state.SearchTable == "Customers")
            {
                await SearchCriteriaCustomersAsync(field, txtToSearch);
            }
            else if (_state.SearchTable == "Products")
            {
                await SearchCriteriaProductsAsync(field, txtToSearch);
            }
            else if (_state.SearchTable == "Providers")
            {
                await SearchCriteriaProvidersAsync(field, txtToSearch);
            }
            else
            {
                Snackbar.Add("No search table specified.", Severity.Warning);
            }
        }
        catch (Exception ex)
        {
            _statusMessage = $"Search error: {ex.Message}";
            _statusSeverity = Severity.Error;
            Snackbar.Add(_statusMessage, Severity.Error);
        }
        finally
        {
            _isLoading = false;
            StateHasChanged();
        }
    }

    /// <summary>
    /// Port of SearchCriteriaCustomers(string field, string value).
    /// Legacy: ExecuteSql, checks RecordCount, shows message or sets dcCustomers.Recordset.
    /// </summary>
    private async Task SearchCriteriaCustomersAsync(string field, string value)
    {
        var results = await SearchService.SearchCriteriaCustomersAsync(field, value);

        if (results.Count == 0)
        {
            _statusMessage = "There are no records with the selected criteria";
            _statusSeverity = Severity.Info;
            Snackbar.Add(_statusMessage, Severity.Info);
        }
        else
        {
            _statusMessage = $"There are {results.Count} that meet with the selected criteria";
            _statusSeverity = Severity.Success;
            AppState.LogStatus(_statusMessage);

            // Legacy set frmCustomers.DefInstance.dcCustomers.Recordset = rs
            NavigationManager.NavigateTo(
                $"/upgraded/customers?searchField={Uri.EscapeDataString(field)}&searchValue={Uri.EscapeDataString(value)}",
                forceLoad: false);
        }
    }

    /// <summary>
    /// Port of SearchCriteriaProducts(string field, string value).
    /// Legacy: ExecuteSql, checks RecordCount, sets frmProducts.dcProducts.Recordset.
    /// </summary>
    private async Task SearchCriteriaProductsAsync(string field, string value)
    {
        var results = await SearchService.SearchCriteriaProductsAsync(field, value);

        if (results.Count == 0)
        {
            _statusMessage = "There are no records with the selected criteria";
            _statusSeverity = Severity.Info;
            Snackbar.Add(_statusMessage, Severity.Info);
        }
        else
        {
            // Legacy: set frmProducts.DefInstance.dcProducts.Recordset = rs (no LogStatus call)
            NavigationManager.NavigateTo(
                $"/upgraded/products?searchField={Uri.EscapeDataString(field)}&searchValue={Uri.EscapeDataString(value)}",
                forceLoad: false);
        }
    }

    /// <summary>
    /// Port of SearchCriteriaProviders(string field, string value).
    /// Legacy: ExecuteSql, checks RecordCount, shows message or sets dcProviders.Recordset.
    /// </summary>
    private async Task SearchCriteriaProvidersAsync(string field, string value)
    {
        var results = await SearchService.SearchCriteriaProvidersAsync(field, value);

        if (results.Count == 0)
        {
            _statusMessage = "There are no records with the selected criteria";
            _statusSeverity = Severity.Info;
            Snackbar.Add(_statusMessage, Severity.Info);
        }
        else
        {
            _statusMessage = $"There are {results.Count} that meet with the selected criteria";
            _statusSeverity = Severity.Success;
            AppState.LogStatus(_statusMessage);

            NavigationManager.NavigateTo(
                $"/upgraded/providers?searchField={Uri.EscapeDataString(field)}&searchValue={Uri.EscapeDataString(value)}",
                forceLoad: false);
        }
    }

    /// <summary>
    /// Port of cmdClose_Click (1 line: this.Close()).
    /// In Blazor, navigate back.
    /// </summary>
    private void OnCloseClick()
    {
        NavigationManager.NavigateTo("/", forceLoad: false);
    }

    // ── Dispose ──────────────────────────────────────────────────────

    /// <summary>
    /// Port of Form_Closed — legacy body was empty.
    /// </summary>
    public void Dispose()
    {
        // Legacy Form_Closed was empty — no cleanup needed.
    }
}
