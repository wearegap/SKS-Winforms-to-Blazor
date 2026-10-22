using Microsoft.AspNetCore.Components;
using MudBlazor;
using SKS_Blazor.Application.DTOs.Core;
using SKS_Blazor.Application.DTOs.Customers;
using SKS_Blazor.Application.DTOs.Search;
using SKS_Blazor.Application.Services.Customers;
using SKS_Blazor.Application.Services.Functions;
using SKS_Blazor.Application.Services.Search;
using SKS_Blazor.Infrastructure;
using SKS_Blazor.Shared.Dialogs;

namespace SKS_Blazor.Pages.Customers;

/// <summary>Port of <c>frmCustomers</c>.</summary>
public partial class Customers : ComponentBase, IDisposable
{
    [Inject] private ICustomersService CustomersService { get; set; } = default!;
    [Inject] private ISearchService SearchService { get; set; } = default!;
    [Inject] private IFunctionsService FunctionsService { get; set; } = default!;
    [Inject] private IDialogService DialogService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private AppState AppState { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private ILogger<Customers> Logger { get; set; } = default!;

    // --- State ---
    private CustomersStateDto? _state;
    private List<CustomersDto> _customers = new();
    private CustomersDto? _currentCustomer;
    private CustomersDto? _selectedGridCustomer;
    private string _statusMessage = string.Empty;
    private bool _isLoading;

    // --- Edit fields (bound to the form) ---
    private string _editCompanyName = string.Empty;
    private string _editContactTitle = string.Empty;
    private string _editContactFirstName = string.Empty;
    private string _editContactLastName = string.Empty;
    private string _editCompanyOrDepartment = string.Empty;
    private string _editBillingAddress = string.Empty;
    private string _editCity = string.Empty;
    private string _editStateOrProvince = string.Empty;
    private string _editPostalCode = string.Empty;
    private string _editCountryRegion = string.Empty;
    private string _editEmailAddress = string.Empty;
    private string _editPhoneNumber = string.Empty;
    private string _editExtension = string.Empty;
    private string _editFaxNumber = string.Empty;
    private string _editNotes = string.Empty;

    // Current customer ID preserved for Form_Closed equivalent
    private string _currentCustomerID = string.Empty;

    private bool IsReadOnly => !(_state?.EditMode == true || _state?.NewMode == true);

    // ──────────────────────────────────────────────
    // Lifecycle
    // ──────────────────────────────────────────────

    /// <summary>Port of <c>Form_Load</c> → <c>InitForm</c>.</summary>
    protected override async Task OnInitializedAsync()
    {
        AppState.OnCommand += HandleAppCommand;
        _state = new CustomersStateDto();
        _isLoading = true;

        try
        {
            // InitForm — sets mode flags and loads initial data
            _state = await CustomersService.InitFormAsync(_state);
            await LoadCustomersFromServiceAsync();
            if (_customers.Count > 0)
            {
                SelectCustomer(_customers[0]);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error initializing Customers page");
            Snackbar.Add("Failed to load customers data.", Severity.Error);
        }
        finally
        {
            _isLoading = false;
        }
    }

    /// <summary>Port of <c>Form_Closed</c>.</summary>
    public void Dispose()
    {
        // Legacy: CurrentCustomerID = Convert.ToString(dcCustomers.Recordset["CustomerId"])
        if (_currentCustomer is not null)
        {
            _currentCustomerID = _currentCustomer.CustomerID.ToString();
        }

        AppState.OnCommand -= HandleAppCommand;
    }

    private async Task HandleAppCommand(string command)
    {
        switch (command)
        {
            case "save":
                await OnSaveClick();
                break;
            case "delete":
                await OnDeleteClick();
                break;
        }
    }

    // ──────────────────────────────────────────────
    // Toolbar handlers — port of Toolbar1_ButtonClick
    // ──────────────────────────────────────────────

    /// <summary>Port of Toolbar1_ButtonClick "Add" branch — creates a new blank record.</summary>
    private Task OnAddClick()
    {
        if (_state is null) return Task.CompletedTask;

        _state.NewMode = true;
        _state.EditMode = false;
        _state.CancellingMode = false;

        // Clear fields for new entry — equivalent of dcCustomers.Recordset.AddNew()
        _currentCustomer = new CustomersDto
        {
            CustomerID = 0,
            CompanyName = string.Empty,
            ContactTitle = string.Empty,
            ContactFirstName = string.Empty,
            ContactLastName = string.Empty,
            CompanyOrDepartment = string.Empty,
            BillingAddress = string.Empty,
            City = string.Empty,
            StateOrProvince = string.Empty,
            PostalCode = string.Empty,
            CountryRegion = string.Empty,
            EmailAddress = string.Empty,
            PhoneNumber = string.Empty,
            Extension = string.Empty,
            FaxNumber = string.Empty,
            Notes = string.Empty
        };
        PopulateEditFields(_currentCustomer);
        _statusMessage = "New customer record — fill in details and click Save.";
        StateHasChanged();
        return Task.CompletedTask;
    }

    /// <summary>Port of Toolbar1_ButtonClick "Edit" branch.</summary>
    private Task OnEditClick()
    {
        if (_state is null || _currentCustomer is null) return Task.CompletedTask;

        _state.EditMode = true;
        _statusMessage = "Editing customer record.";
        StateHasChanged();
        return Task.CompletedTask;
    }

    /// <summary>Port of Toolbar1_ButtonClick "Save" branch — persists the current record.
    /// Legacy: dcCustomers.Recordset.Update(); dcCustomers.Recordset.Requery();</summary>
    private async Task OnSaveClick()
    {
        if (_state is null) return;
        if (!(_state.EditMode || _state.NewMode)) return;

        _isLoading = true;
        StateHasChanged();

        try
        {
            // Pre-save validation — company name is required
            if (string.IsNullOrWhiteSpace(_editCompanyName))
            {
                Snackbar.Add("Company Name is required.", Severity.Warning);
                return;
            }

            var dto = new CustomersDto
            {
                CustomerID = _currentCustomer?.CustomerID ?? 0,
                CompanyName = _editCompanyName,
                ContactTitle = _editContactTitle,
                ContactFirstName = _editContactFirstName,
                ContactLastName = _editContactLastName,
                CompanyOrDepartment = _editCompanyOrDepartment,
                BillingAddress = _editBillingAddress,
                City = _editCity,
                StateOrProvince = _editStateOrProvince,
                PostalCode = _editPostalCode,
                CountryRegion = _editCountryRegion,
                EmailAddress = _editEmailAddress,
                PhoneNumber = _editPhoneNumber,
                Extension = _editExtension,
                FaxNumber = _editFaxNumber,
                Notes = _editNotes
            };

            if (_state.NewMode)
            {
                await CustomersService.CreateCustomerAsync(dto);
            }
            else
            {
                await CustomersService.UpdateCustomerAsync(dto);
            }

            // Requery equivalent — reload the list
            _state.EditMode = false;
            _state.NewMode = false;

            await LoadCustomersFromServiceAsync();

            Snackbar.Add("Customer saved successfully.", Severity.Success);
            _statusMessage = "Customer saved.";
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error saving customer");
            Snackbar.Add("Error while saving customer.", Severity.Error);
        }
        finally
        {
            _isLoading = false;
            StateHasChanged();
        }
    }

    /// <summary>Port of Toolbar1_ButtonClick "Delete" branch — deletes the current record
    /// after confirmation dialog. Legacy: MessageBox.Show("Are you sure you want to delete this record?", "Delete record", ...)</summary>
    private async Task OnDeleteClick()
    {
        if (_currentCustomer is null || _state?.NewMode == true) return;

        // Confirmation dialog — port of MessageBox.Show
        var confirmed = await DialogService.ShowMessageBox(
            "Delete record",
            "Are you sure you want to delete this record?",
            yesText: "Yes", cancelText: "No");

        if (confirmed != true) return;

        _isLoading = true;
        StateHasChanged();

        try
        {
            await CustomersService.DeleteCustomerAsync(_currentCustomer.CustomerID);

            // Requery equivalent
            await LoadCustomersFromServiceAsync();
            if (_customers.Count > 0)
            {
                SelectCustomer(_customers[0]);
            }
            else
            {
                _currentCustomer = null;
                ClearEditFields();
            }

            Snackbar.Add("Customer deleted successfully.", Severity.Success);
            _statusMessage = "Customer record deleted.";
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error deleting customer");
            Snackbar.Add("Error while deleting customer.", Severity.Error);
        }
        finally
        {
            _isLoading = false;
            StateHasChanged();
        }
    }

    /// <summary>Port of Toolbar1_ButtonClick "Search" branch — opens the search dialog
    /// equivalent to modFunctions.SearchShow("Customers", "CompanyName", "customer").</summary>
    private async Task OnSearchClick()
    {
        // Prompt user for search value via dialog
        var parameters = new DialogParameters<SearchInputDialog>
        {
            { x => x.Title, "Search Customers" },
            { x => x.FieldLabel, "Company Name" }
        };
        var options = new DialogOptions { CloseOnEscapeKey = true, MaxWidth = MaxWidth.Small, FullWidth = true };
        var dialogResult = await DialogService.ShowAsync<SearchInputDialog>("Search Customers", parameters, options);
        var result = await dialogResult.Result;

        if (result is null || result.Canceled) return;

        var searchValue = result.Data?.ToString() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(searchValue)) return;

        _isLoading = true;
        StateHasChanged();

        try
        {
            // Port of SearchCriteriaProducts — calls service for customer search
            await SearchCriteriaProducts("CompanyName", searchValue);
        }
        finally
        {
            _isLoading = false;
            StateHasChanged();
        }
    }

    /// <summary>Port of Toolbar1_ButtonClick "Cancel" branch — cancels edit and closes.
    /// Legacy: CancellingMode = true; EditMode = false; NewMode = false;
    /// dcCustomers.Recordset.CancelUpdate(); dcCustomers.Recordset.Requery(); this.Close();</summary>
    private async Task OnCancelClick()
    {
        if (_state is null) return;

        _state.CancellingMode = true;

        // Cancel edited changes
        _state.EditMode = false;
        _state.NewMode = false;

        // Requery — reload from database, discarding changes
        await LoadCustomersFromServiceAsync();
        if (_customers.Count > 0)
        {
            SelectCustomer(_customers[0]);
        }
        else
        {
            _currentCustomer = null;
            ClearEditFields();
        }

        _state.CancellingMode = false;

        // Legacy: this.Close() — navigate back
        NavigationManager.NavigateTo("/");
    }

    // ──────────────────────────────────────────────
    // txtField_TextChanged equivalent
    // ──────────────────────────────────────────────

    /// <summary>Port of <c>txtField_TextChanged</c>. Sets EditMode when any field changes
    /// (unless currently in CancellingMode). This handler is called via @bind-Value:after
    /// on each MudTextField in the form. Applies to all 15 text fields:
    /// CompanyName, ContactTitle, ContactFirstName, ContactLastName, CompanyOrDepartment,
    /// BillingAddress, City, StateOrProvince, PostalCode, CountryRegion,
    /// EmailAddress, PhoneNumber, Extension, FaxNumber, Notes.</summary>
    private Task OnFieldChanged()
    {
        if (_state is not null && !_state.CancellingMode)
        {
            _state.EditMode = true;
        }
        return Task.CompletedTask;
    }

    // ──────────────────────────────────────────────
    // SearchCriteriaProducts
    // ──────────────────────────────────────────────

    /// <summary>Port of <c>SearchCriteriaProducts</c> — searches customers by field/value
    /// using the service. Legacy: Select * from Customers where {field} LIKE '{value}%'.</summary>
    private async Task SearchCriteriaProducts(string field, string value)
    {
        try
        {
            var results = await CustomersService.SearchCustomersAsync(field, value);

            if (results.Count == 0)
            {
                // Legacy: MessageBox.Show("There are no records with the selected criteria", "Search", ...)
                Snackbar.Add("There are no records with the selected criteria.", Severity.Info);
            }
            else
            {
                // Legacy: modMain.LogStatus($"There are {count} that meet with the selected criteria")
                _statusMessage = $"There are {results.Count} that meet with the selected criteria";

                _customers = results;

                if (_customers.Count > 0)
                {
                    SelectCustomer(_customers[0]);
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error searching customers by {Field}={Value}", field, value);
            Snackbar.Add("Error performing search.", Severity.Error);
        }
    }

    // ──────────────────────────────────────────────
    // Grid interaction
    // ──────────────────────────────────────────────

    private void OnRowClick(DataGridRowClickEventArgs<CustomersDto> args)
    {
        SelectCustomer(args.Item);
    }

    private void OnSelectedItemChanged(CustomersDto? item)
    {
        if (item is not null)
        {
            SelectCustomer(item);
        }
    }

    // ──────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────

    private async Task LoadCustomersFromServiceAsync()
    {
        try
        {
            _customers = await CustomersService.GetAllCustomersAsync();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading customers from service");
            _customers = new List<CustomersDto>();
        }
    }

    /// <summary>Selects a customer in the grid and populates the edit fields.
    /// Also resets mode flags — port of dcCustomers_MoveComplete (commented out in legacy
    /// but behaviorally equivalent: NewMode=false, EditMode=false, CancellingMode=false).</summary>
    // ── Grid view and record navigation (port of the dcCustomers data control at 8,400) ──

    /// <summary>Grid view shows every record at once; the record navigator is hidden while it is on.</summary>
    private bool _gridView;

    private int CurrentIndex =>
        _currentCustomer is null ? -1 : _customers.FindIndex(c => c.CustomerID == _currentCustomer.CustomerID);

    /// <summary>
    /// Toggles the grid below the Customer information panel. Refuses to leave the form mid-edit,
    /// because the grid has no Save and the pending edit would be lost.
    /// </summary>
    private void OnToggleGridView()
    {
        if (!_gridView && (_state?.EditMode == true || _state?.NewMode == true))
        {
            Snackbar.Add("Save or cancel the current record before switching to grid view.", Severity.Warning);
            return;
        }

        _gridView = !_gridView;
        StateHasChanged();
    }

    private void MoveTo(int index)
    {
        if (index >= 0 && index < _customers.Count)
        {
            SelectCustomer(_customers[index]);
        }
    }

    private void OnFirstRecord() => MoveTo(0);
    private void OnPreviousRecord() => MoveTo(CurrentIndex - 1);
    private void OnNextRecord() => MoveTo(CurrentIndex + 1);
    private void OnLastRecord() => MoveTo(_customers.Count - 1);

    private void SelectCustomer(CustomersDto customer)
    {
        _currentCustomer = customer;
        _selectedGridCustomer = customer;
        PopulateEditFields(customer);
        _currentCustomerID = customer.CustomerID.ToString();

        // Reset mode flags when navigating (port of dcCustomers_MoveComplete)
        if (_state is not null)
        {
            _state.NewMode = false;
            _state.EditMode = false;
            _state.CancellingMode = false;
        }

        StateHasChanged();
    }

    private void PopulateEditFields(CustomersDto dto)
    {
        _editCompanyName = dto.CompanyName ?? string.Empty;
        _editContactTitle = dto.ContactTitle ?? string.Empty;
        _editContactFirstName = dto.ContactFirstName ?? string.Empty;
        _editContactLastName = dto.ContactLastName ?? string.Empty;
        _editCompanyOrDepartment = dto.CompanyOrDepartment ?? string.Empty;
        _editBillingAddress = dto.BillingAddress ?? string.Empty;
        _editCity = dto.City ?? string.Empty;
        _editStateOrProvince = dto.StateOrProvince ?? string.Empty;
        _editPostalCode = dto.PostalCode ?? string.Empty;
        _editCountryRegion = dto.CountryRegion ?? string.Empty;
        _editEmailAddress = dto.EmailAddress ?? string.Empty;
        _editPhoneNumber = dto.PhoneNumber ?? string.Empty;
        _editExtension = dto.Extension ?? string.Empty;
        _editFaxNumber = dto.FaxNumber ?? string.Empty;
        _editNotes = dto.Notes ?? string.Empty;
    }

    private void ClearEditFields()
    {
        _editCompanyName = string.Empty;
        _editContactTitle = string.Empty;
        _editContactFirstName = string.Empty;
        _editContactLastName = string.Empty;
        _editCompanyOrDepartment = string.Empty;
        _editBillingAddress = string.Empty;
        _editCity = string.Empty;
        _editStateOrProvince = string.Empty;
        _editPostalCode = string.Empty;
        _editCountryRegion = string.Empty;
        _editEmailAddress = string.Empty;
        _editPhoneNumber = string.Empty;
        _editExtension = string.Empty;
        _editFaxNumber = string.Empty;
        _editNotes = string.Empty;
    }
}
