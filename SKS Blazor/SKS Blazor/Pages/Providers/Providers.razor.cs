using Microsoft.AspNetCore.Components;
using MudBlazor;
using SKS_Blazor.Application.DTOs.Core;
using SKS_Blazor.Application.DTOs.Providers;
using SKS_Blazor.Application.DTOs.Search;
using SKS_Blazor.Application.Services.Functions;
using SKS_Blazor.Application.Services.Providers;
using SKS_Blazor.Application.Services.Search;
using SKS_Blazor.Infrastructure;
using SKS_Blazor.Shared.Dialogs;

namespace SKS_Blazor.Pages.Providers;

/// <summary>Port of <c>frmProviders</c> (WinForms provider management form).</summary>
public partial class Providers : ComponentBase, IAsyncDisposable
{
    [Inject] private IProvidersService ProvidersService { get; set; } = default!;
    [Inject] private ISearchService SearchService { get; set; } = default!;
    [Inject] private IFunctionsService FunctionsService { get; set; } = default!;
    [Inject] private IDialogService DialogService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private AppState AppState { get; set; } = default!;
    [Inject] private ILogger<Providers> Logger { get; set; } = default!;

    private ProvidersStateDto? _state;
    private List<ProvidersDto> _providersList = new();
    private ProvidersDto? _selectedGridProvider;
    private ProvidersDto? _currentProvider;
    private ProvidersDto? _originalProvider;
    private bool _isLoading;
    private int _currentProviderID;

    /// <summary>Port of <c>IsEditable</c> — true when NewMode or EditMode is active.</summary>
    private bool IsEditable => _state?.NewMode == true || _state?.EditMode == true;

    protected override async Task OnInitializedAsync()
    {
        _state = new ProvidersStateDto
        {
            NewMode = false,
            EditMode = false,
            CancellingMode = false
        };

        // Port of Form_Load — initialize modes and load provider data
        await LoadProvidersAsync();

        AppState.OnCommand += HandleCommandAsync;
    }

    /// <summary>Port of <c>Form_Load</c> — loads provider list from database.</summary>
    private async Task LoadProvidersAsync()
    {
        _isLoading = true;
        StateHasChanged();
        try
        {
            _providersList = await ProvidersService.GetAllProvidersAsync();

            // Select first provider if available
            if (_providersList.Count > 0 && _currentProvider == null)
            {
                _selectedGridProvider = _providersList[0];
                SelectProvider(_selectedGridProvider);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading providers");
            Snackbar.Add("Error loading providers", Severity.Error);
        }
        finally
        {
            _isLoading = false;
            StateHasChanged();
        }
    }

    /// <summary>Selects a provider from the grid and populates the detail fields.</summary>
    // ── Grid view and record navigation (port of the dcProviders data control at 8,408) ──

    /// <summary>Grid view shows every record at once; the record navigator is hidden while it is on.</summary>
    private bool _gridView;

    private int CurrentIndex =>
        _currentProvider is null ? -1 : _providersList.FindIndex(p => p.ProviderID == _currentProvider.ProviderID);

    /// <summary>
    /// Toggles the grid below the Supplier information panel. Refuses to leave the form mid-edit,
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
        if (index >= 0 && index < _providersList.Count)
        {
            SelectProvider(_providersList[index]);
        }
    }

    private void OnFirstRecord() => MoveTo(0);
    private void OnPreviousRecord() => MoveTo(CurrentIndex - 1);
    private void OnNextRecord() => MoveTo(CurrentIndex + 1);
    private void OnLastRecord() => MoveTo(_providersList.Count - 1);

    private void OnGridRowClick(DataGridRowClickEventArgs<ProvidersDto> args)
    {
        if (args.Item is not null)
        {
            SelectProvider(args.Item);
        }
    }

    private void SelectProvider(ProvidersDto provider)
    {
        _currentProvider = new ProvidersDto
        {
            ProviderID = provider.ProviderID,
            ProviderName = provider.ProviderName,
            ContactFirstName = provider.ContactFirstName,
            ContactLastName = provider.ContactLastName,
            ContactTitle = provider.ContactTitle,
            Address = provider.Address,
            City = provider.City,
            StateOrProvince = provider.StateOrProvince,
            PostalCode = provider.PostalCode,
            CountryRegion = provider.CountryRegion,
            PhoneNumber = provider.PhoneNumber,
            Extension = provider.Extension,
            FaxNumber = provider.FaxNumber,
            EmailAddress = provider.EmailAddress,
            PaymentTerms = provider.PaymentTerms,
            Notes = provider.Notes
        };

        // Store original for cancel/revert
        _originalProvider = new ProvidersDto
        {
            ProviderID = provider.ProviderID,
            ProviderName = provider.ProviderName,
            ContactFirstName = provider.ContactFirstName,
            ContactLastName = provider.ContactLastName,
            ContactTitle = provider.ContactTitle,
            Address = provider.Address,
            City = provider.City,
            StateOrProvince = provider.StateOrProvince,
            PostalCode = provider.PostalCode,
            CountryRegion = provider.CountryRegion,
            PhoneNumber = provider.PhoneNumber,
            Extension = provider.Extension,
            FaxNumber = provider.FaxNumber,
            EmailAddress = provider.EmailAddress,
            PaymentTerms = provider.PaymentTerms,
            Notes = provider.Notes
        };

        if (_state != null)
        {
            _state.NewMode = false;
            _state.EditMode = false;
            _state.CancellingMode = false;
        }

        _currentProviderID = provider.ProviderID;
    }

    /// <summary>Grid selection changed handler.</summary>
    private void OnProviderSelected(ProvidersDto? provider)
    {
        if (provider == null) return;
        _selectedGridProvider = provider;
        SelectProvider(provider);
    }

    /// <summary>Port of <c>txtField_TextChanged</c> — marks EditMode when user types in any of the 14 text fields.
    /// Legacy: if (!CancellingMode) { EditMode = true; }
    /// This single handler covers all txtField[0] through txtField[14] change events.</summary>
    private Task OnTxtFieldChanged()
    {
        if (_state != null && !_state.CancellingMode)
        {
            _state.EditMode = true;
        }
        return Task.CompletedTask;
    }

    /// <summary>Port of <c>Toolbar1_ButtonClick</c> case "Add" — adds new record.
    /// Legacy: NewMode = true; dcProviders.Recordset.AddNew();</summary>
    private void OnToolbarAdd()
    {
        if (_state == null) return;

        _state.NewMode = true;
        _state.EditMode = false;
        _state.CancellingMode = false;

        _currentProvider = new ProvidersDto
        {
            ProviderID = 0,
            ProviderName = string.Empty,
            ContactFirstName = string.Empty,
            ContactLastName = string.Empty,
            ContactTitle = string.Empty,
            Address = string.Empty,
            City = string.Empty,
            StateOrProvince = string.Empty,
            PostalCode = string.Empty,
            CountryRegion = string.Empty,
            PhoneNumber = string.Empty,
            Extension = string.Empty,
            FaxNumber = string.Empty,
            EmailAddress = string.Empty,
            PaymentTerms = string.Empty,
            Notes = string.Empty
        };

        _originalProvider = null;
        _selectedGridProvider = null;
    }

    /// <summary>Port of <c>Toolbar1_ButtonClick</c> case "Edit" — enters edit mode.
    /// Legacy: EditMode = true;</summary>
    private void OnToolbarEdit()
    {
        if (_state == null || _currentProvider == null) return;
        _state.EditMode = true;
    }

    /// <summary>Port of <c>Toolbar1_ButtonClick</c> case "Save" — calls Save().
    /// Legacy: Save();</summary>
    private async Task OnToolbarSave()
    {
        await SaveAsync();
    }

    /// <summary>Port of <c>Toolbar1_ButtonClick</c> case "Delete" — confirms and deletes record.
    /// Legacy: MessageBox.Show("Are you sure..."); dcProviders.Recordset.Delete(); dcProviders.Recordset.Requery();</summary>
    private async Task OnToolbarDelete()
    {
        if (_currentProvider == null || _currentProvider.ProviderID == 0) return;

        var confirmed = await DialogService.ShowMessageBox(
            "Delete record",
            "Are you sure you want to delete this record?",
            yesText: "Yes",
            noText: "No");

        if (confirmed != true) return;

        _isLoading = true;
        StateHasChanged();
        try
        {
            await ProvidersService.DeleteProviderAsync(_currentProvider.ProviderID);

            Snackbar.Add("Provider deleted successfully", Severity.Success);
            _currentProvider = null;
            _originalProvider = null;
            _selectedGridProvider = null;
            await LoadProvidersAsync();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error deleting provider");
            Snackbar.Add("Cannot delete provider — it may be referenced by other records", Severity.Error);
        }
        finally
        {
            _isLoading = false;
            StateHasChanged();
        }
    }

    /// <summary>Port of <c>Toolbar1_ButtonClick</c> case "Search" — opens search dialog.
    /// Legacy: modFunctions.SearchShow("Providers", "ProviderName", "Provider");</summary>
    private async Task OnToolbarSearch()
    {
        var parameters = new DialogParameters<SearchInputDialog>
        {
            { x => x.Title, "Search Providers" },
            { x => x.FieldLabel, "Provider name" }
        };

        var options = new DialogOptions
        {
            MaxWidth = MaxWidth.Small,
            FullWidth = true,
            CloseOnEscapeKey = true
        };

        var dialogRef = await DialogService.ShowAsync<SearchInputDialog>("Search Providers", parameters, options);
        var result = await dialogRef.Result;

        if (result != null && !result.Canceled && result.Data is string searchValue && !string.IsNullOrWhiteSpace(searchValue))
        {
            _isLoading = true;
            StateHasChanged();
            try
            {
                var searchState = new SearchStateDto
                {
                    SearchTable = "Providers",
                    CboSrchByText = "ProviderName",
                    LblSrchByText = "Provider"
                };

                searchState = await SearchService.SearchAsync(searchState, "Providers", "ProviderName", searchValue);

                var filteredEntities = await ProvidersService.SearchProvidersByNameAsync(searchValue);

                if (filteredEntities.Count == 0)
                {
                    Snackbar.Add("There are no records with the selected criteria", Severity.Info);
                }
                else
                {
                    AppState.LogStatus($"There are {filteredEntities.Count} that meet with the selected criteria");

                    _providersList = filteredEntities;

                    if (_providersList.Count > 0)
                    {
                        _selectedGridProvider = _providersList[0];
                        SelectProvider(_selectedGridProvider);
                    }

                    Snackbar.Add($"Found {filteredEntities.Count} provider(s)", Severity.Success);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error searching providers");
                Snackbar.Add("Error searching providers", Severity.Error);
            }
            finally
            {
                _isLoading = false;
                StateHasChanged();
            }
        }
    }

    /// <summary>Port of <c>Toolbar1_ButtonClick</c> case "Cancel" — cancels edits, reverts, and closes.
    /// Legacy: CancellingMode = true; EditMode = false; NewMode = false; dcProviders.Recordset.CancelUpdate();
    /// dcProviders.Recordset.Requery(); CancellingMode = false; this.Close();</summary>
    private async Task OnToolbarCancel()
    {
        if (_state == null) return;

        _state.CancellingMode = true;

        // Cancel edited changes
        _state.EditMode = false;
        _state.NewMode = false;

        // Revert to original data (port of dcProviders.Recordset.CancelUpdate)
        if (_originalProvider != null)
        {
            SelectProvider(_originalProvider);
        }
        else
        {
            _currentProvider = null;
        }

        // Reload full list (port of dcProviders.Recordset.Requery)
        await LoadProvidersAsync();

        _state.CancellingMode = false;

        // Port of this.Close() — navigate back
        NavigationManager.NavigateTo("/");
    }

    /// <summary>Port of <c>Save</c> — validates required fields and persists provider data.
    /// Preserves legacy validation of txtField[0,1,2,4,5,6,7,7,10,11,12,14].</summary>
    private async Task SaveAsync()
    {
        if (_state == null || _currentProvider == null) return;

        _isLoading = true;
        StateHasChanged();
        try
        {
            // PRE-SAVE VALIDATION — port of legacy Save() field-by-field validation
            // txtField[0] = ProviderName
            if (string.IsNullOrWhiteSpace(_currentProvider.ProviderName))
            {
                Snackbar.Add("Provider Name is required", Severity.Warning);
                return;
            }
            // txtField[1] = ContactFirstName
            if (string.IsNullOrWhiteSpace(_currentProvider.ContactFirstName))
            {
                Snackbar.Add("Contact First Name is required", Severity.Warning);
                return;
            }
            // txtField[2] = ContactLastName
            if (string.IsNullOrWhiteSpace(_currentProvider.ContactLastName))
            {
                Snackbar.Add("Contact Last Name is required", Severity.Warning);
                return;
            }
            // txtField[4] = Address
            if (string.IsNullOrWhiteSpace(_currentProvider.Address))
            {
                Snackbar.Add("Address is required", Severity.Warning);
                return;
            }
            // txtField[5] = City
            if (string.IsNullOrWhiteSpace(_currentProvider.City))
            {
                Snackbar.Add("City is required", Severity.Warning);
                return;
            }
            // txtField[6] = StateOrProvince
            if (string.IsNullOrWhiteSpace(_currentProvider.StateOrProvince))
            {
                Snackbar.Add("State/Province is required", Severity.Warning);
                return;
            }
            // txtField[7] = PostalCode (validated twice in legacy — same check)
            if (string.IsNullOrWhiteSpace(_currentProvider.PostalCode))
            {
                Snackbar.Add("Postal Code is required", Severity.Warning);
                return;
            }
            // txtField[10] = Extension
            if (string.IsNullOrWhiteSpace(_currentProvider.Extension))
            {
                Snackbar.Add("Extension is required", Severity.Warning);
                return;
            }
            // txtField[11] = FaxNumber
            if (string.IsNullOrWhiteSpace(_currentProvider.FaxNumber))
            {
                Snackbar.Add("Fax Number is required", Severity.Warning);
                return;
            }
            // txtField[12] = EmailAddress
            if (string.IsNullOrWhiteSpace(_currentProvider.EmailAddress))
            {
                Snackbar.Add("Email Address is required", Severity.Warning);
                return;
            }
            // txtField[14] = Notes
            if (string.IsNullOrWhiteSpace(_currentProvider.Notes))
            {
                Snackbar.Add("Notes is required", Severity.Warning);
                return;
            }

            // ENTITY PREPARATION — delegate save to service
            if (_state.NewMode)
            {
                var result = await ProvidersService.CreateProviderAsync(_currentProvider);
                _currentProvider.ProviderID = result.ProviderID;
                _currentProviderID = result.ProviderID;
            }
            else
            {
                await ProvidersService.UpdateProviderAsync(_currentProvider);
            }

            // Port of legacy: EditMode = false; NewMode = false;
            _state.EditMode = false;
            _state.NewMode = false;

            // Also call service SaveAsync to maintain state consistency
            _state = await ProvidersService.SaveAsync(_state);

            Snackbar.Add("Provider saved successfully", Severity.Success);

            // Reload list (port of dcProviders.Recordset.Requery)
            await LoadProvidersAsync();

            // Reselect the saved provider
            var savedProvider = _providersList.FirstOrDefault(p => p.ProviderID == _currentProviderID);
            if (savedProvider != null)
            {
                _selectedGridProvider = savedProvider;
                SelectProvider(savedProvider);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Unexpected error saving provider");
            Snackbar.Add("Unexpected error saving provider", Severity.Error);
        }
        finally
        {
            _isLoading = false;
            StateHasChanged();
        }
    }

    /// <summary>Port of <c>Form_Closed</c> — stores current provider ID.
    /// Legacy: CurrentProviderID = Convert.ToInt32(dcProviders.Recordset["ProviderId"])
    /// In Blazor, _currentProviderID is kept up to date during selection.</summary>

    /// <summary>Handle layout commands dispatched via AppState.</summary>
    private Task HandleCommandAsync(string command)
    {
        return command switch
        {
            "refresh" => LoadProvidersAsync(),
            _ => Task.CompletedTask
        };
    }

    async ValueTask IAsyncDisposable.DisposeAsync()
    {
        if (AppState != null)
        {
            AppState.OnCommand -= HandleCommandAsync;
        }
        await ValueTask.CompletedTask;
    }
}
