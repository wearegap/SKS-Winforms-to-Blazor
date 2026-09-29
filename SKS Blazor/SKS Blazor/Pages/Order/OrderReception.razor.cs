using Microsoft.AspNetCore.Components;
using MudBlazor;
using SKS_Blazor.Application.DTOs.Core;
using SKS_Blazor.Application.DTOs.Order;
using SKS_Blazor.Application.Services.Functions;
using SKS_Blazor.Application.Services.Order;
using SKS_Blazor.Infrastructure;
using SKS_Blazor.Shared.Dialogs;
using SKS_Blazor.Domain.Entities.Core;
using SKS_Blazor.Domain.Entities.Products;
using SKS_Blazor.Domain.Entities.Order;

namespace SKS_Blazor.Pages.Order;

/// <summary>Port of <c>frmOrderReception</c>.</summary>
public partial class OrderReception : IDisposable
{
    [Inject] private IOrderReceptionService OrderReceptionService { get; set; } = default!;
    [Inject] private IFunctionsService FunctionsService { get; set; } = default!;
    [Inject] private IDialogService DialogService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private AppState AppState { get; set; } = default!;

    private OrderReceptionStateDto? _state;
    private List<ProvidersDto> _providersList = new();
    private List<OrderReceptionProductLineDto> _productLines = new();
    private bool _saveEnabled;
    private bool _isLoading;
    private bool _editingData;

    /// <summary>Port of <c>Form_Load</c>. Initializes state and clears fields.</summary>
    protected override async Task OnInitializedAsync()
    {
        _state = new OrderReceptionStateDto
        {
            CurrentIdProvider = 0,
            EditingData = false,
            CurrentSubTotal = 0m,
            CurrentTotal = 0m,
            CurrentTax = 0m,
            CurrentFreightCharge = 0m,
            CurrentTotalTax = 0m,
            TxtNotesText = string.Empty,
            TxtSubTotalText = string.Empty,
            TxtTotalText = string.Empty,
            TxtTotalTaxText = string.Empty,
            TxtFreightChargeText = string.Empty,
            TxtSalesTaxText = string.Empty,
            TxtEntryText = string.Empty,
            TxtProviderNameText = string.Empty,
            TxtContactLastNameText = string.Empty,
            TxtContactNameText = string.Empty,
            TxtProviderContactText = string.Empty,
            TxtProviderCompanyText = string.Empty
        };

        // Port of Form_Load: editingData = false; ClearFields();
        _editingData = false;
        ClearFields();

        await Task.CompletedTask;
    }

    // ──────────────────────────────────────────────
    // Port of frmOrderReception_Activated — no-op in Blazor (active form tracking not needed)
    // ──────────────────────────────────────────────

    /// <summary>Port of <c>ClearFields</c>.</summary>
    private void ClearFields()
    {
        _productLines = new List<OrderReceptionProductLineDto>();

        _state!.CurrentSubTotal = 0m;
        _state.CurrentTotal = 0m;
        _state.CurrentTax = 0m;
        _state.CurrentTotalTax = 0m;
        _state.CurrentFreightCharge = 0m;

        _state.TxtSubTotalText = string.Empty;
        _state.TxtTotalText = string.Empty;
        _state.TxtTotalTaxText = string.Empty;
        _state.TxtSalesTaxText = string.Empty;
        _state.TxtFreightChargeText = string.Empty;

        _state.TxtProviderNameText = string.Empty;
        _state.TxtContactLastNameText = string.Empty;
        _state.TxtContactNameText = string.Empty;
        _state.TxtProviderContactText = string.Empty;
        _state.TxtProviderCompanyText = string.Empty;
        _state.TxtNotesText = string.Empty;
        _state.TxtEntryText = string.Empty;

        _saveEnabled = false;
        _editingData = false;

        // Port of ReCalculateTotals(0, 0) at end of ClearFields
        ReCalculateTotals(0m, 0m);
    }

    /// <summary>Port of <c>DoSearchProvider</c>. Delegates to service.</summary>
    private async Task DoSearchProvider(int id = 0)
    {
        _isLoading = true;
        StateHasChanged();
        try
        {
            _state!.CurrentIdProvider = id; // pass the id for filtering
            _providersList = await OrderReceptionService.DoSearchProviderAsync(_state, id);

            if (_providersList.Count == 0)
            {
                Snackbar.Add("There are no records with the selected criteria", Severity.Info);
            }
            else if (_providersList.Count == 1)
            {
                // Auto-select single result — port of: if (lvProviders.Items.Count == 1) select it
                await RetrieveDataProvider(_providersList[0]);
            }
        }
        finally
        {
            _isLoading = false;
            StateHasChanged();
        }
    }

    /// <summary>Port of <c>txtProviderName_TextChanged</c>.</summary>
    private async Task OnProviderNameChanged()
    {
        await DoSearchProvider();
    }

    /// <summary>Port of <c>txtContactName_TextChanged</c>.</summary>
    private async Task OnContactNameChanged()
    {
        await DoSearchProvider();
    }

    /// <summary>Port of <c>txtContactLastName_TextChanged</c>. Legacy: editingData = true + search.</summary>
    private async Task OnContactLastNameChanged()
    {
        _editingData = true;
        await DoSearchProvider();
    }

    /// <summary>Port of <c>txtNotes_TextChanged</c>.</summary>
    private Task OnNotesChanged()
    {
        _editingData = true;
        return Task.CompletedTask;
    }

    /// <summary>Port of <c>lvProviders_ItemClick</c> → <c>RetrieveDataProvider</c>.</summary>
    private async Task OnProviderRowClick(DataGridRowClickEventArgs<ProvidersDto> args)
    {
        await RetrieveDataProvider(args.Item);
    }

    /// <summary>Port of <c>RetrieveDataProvider</c>.</summary>
    private async Task RetrieveDataProvider(ProvidersDto provider)
    {
        // Port of: if editingData, confirm cancel
        if (_editingData)
        {
            bool? confirm = await DialogService.ShowMessageBox(
                "Data edition",
                "Do you want to cancel previous edited data?",
                yesText: "Yes",
                noText: "No");

            if (confirm != true)
            {
                return;
            }
        }

        // Port of: currentIdProvider = Int(provider.Text); names from subitems
        _state!.CurrentIdProvider = provider.ProviderID;
        _state.TxtProviderCompanyText = provider.ProviderName ?? string.Empty;
        _state.TxtProviderContactText = $"{provider.ContactFirstName ?? string.Empty} {provider.ContactLastName ?? string.Empty}".Trim();
        _editingData = false;

        // Port of: LoadProductsById()
        await LoadProductsById();

        _saveEnabled = true;
    }

    /// <summary>Port of <c>LoadProductsById</c>. Delegates to service which queries
    /// Products joined with ProductsByProvider for the current provider.</summary>
    private async Task LoadProductsById()
    {
        _isLoading = true;
        StateHasChanged();
        try
        {
            var products = await OrderReceptionService.LoadProductsByIdAsync(_state!);

            // Build product lines — port of legacy grid population:
            // Columns: Quantity(0), Code(1), Product(2), UnitPrice(3), Price(4), Existence(5), Ordered(6), QuantityPerUnit(7)
            _productLines = new List<OrderReceptionProductLineDto>();
            foreach (var p in products)
            {
                _productLines.Add(new OrderReceptionProductLineDto
                {
                    Quantity = 0m,
                    ProductID = p.ProductID ?? string.Empty,
                    ProductName = p.ProductName ?? string.Empty,
                    UnitPrice = p.UnitPrice ?? 0m,
                    Price = 0m, // Quantity * UnitPrice, starts at 0
                    UnitsInStock = p.UnitsInStock ?? 0,
                    UnitsOnOrder = p.UnitsOnOrder ?? 0,
                    QuantityPerUnit = $"{(p.QuantityPerUnit?.ToString() ?? string.Empty)}{p.Unit ?? string.Empty}"
                });
            }
        }
        finally
        {
            _isLoading = false;
            StateHasChanged();
        }
    }

    /// <summary>Port of <c>cmdProviders_Click</c>. Opens ProvidersDialog, then searches by returned ID.</summary>
    private async Task OnProvidersClick()
    {
        var dialogOptions = new DialogOptions
        {
            MaxWidth = MaxWidth.Large,
            FullWidth = true,
            CloseOnEscapeKey = true
        };

        var dialog = await DialogService.ShowAsync<SKS_Blazor.Pages.Providers.ProvidersDialog>("Providers", dialogOptions);
        var result = await dialog.Result;

        // After dialog closes, clear search fields and search by selected provider ID
        _state!.TxtProviderNameText = string.Empty;
        _state.TxtContactLastNameText = string.Empty;
        _state.TxtContactNameText = string.Empty;

        if (result is not null && !result.Canceled && result.Data is int selectedProviderId && selectedProviderId != 0)
        {
            await DoSearchProvider(selectedProviderId);
        }

        StateHasChanged();
    }

    /// <summary>Port of <c>cmdAddProducts_Click</c>. Opens AddProductToDialog for the current provider.</summary>
    private async Task OnAddProductsClick()
    {
        if (_state!.CurrentIdProvider == 0)
        {
            Snackbar.Add("Please select a provider first.", Severity.Warning);
            return;
        }

        var parameters = new DialogParameters<SKS_Blazor.Pages.Product.AddProductToDialog>
        {
            { x => x.Id, _state.CurrentIdProvider },
            { x => x.ObjectReferred, $"Provider {_state.TxtProviderCompanyText}|{_state.TxtProviderContactText}" },
            { x => x.Table, "ProductsByProvider" },
            { x => x.ColumnName, "ProviderId" }
        };

        var dialogOptions = new DialogOptions
        {
            MaxWidth = MaxWidth.Large,
            FullWidth = true,
            CloseOnEscapeKey = true
        };

        var dialog = await DialogService.ShowAsync<SKS_Blazor.Pages.Product.AddProductToDialog>(
            $"Add product(s) to Provider {_state.TxtProviderCompanyText}|{_state.TxtProviderContactText}",
            parameters,
            dialogOptions);

        var result = await dialog.Result;

        // Port of: if frmAddProductTo.DefInstance.SavedChanges then LoadProductsById
        if (result is not null && !result.Canceled)
        {
            await LoadProductsById();
        }
    }

    /// <summary>Port of <c>SaveEdits</c> / <c>fgProducts_Click</c> / <c>fgProducts_EnterCell</c> /
    /// <c>fgProducts_CellLeave</c> / <c>txtEntry_Leave</c> / <c>MakeTextBoxVisible</c> /
    /// <c>txtEntry_KeyPress</c> / <c>txtEntry_KeyDown</c> / <c>EditKeyCode</c> /
    /// <c>fgProducts_KeyPress</c>.
    /// In Blazor, inline editing is handled by MudDataGrid EditMode=Cell.
    /// When a committed change arrives, we recalculate the line price and totals.
    /// The legacy code only allowed editing column 0 (Quantity) with numeric-only input
    /// (dot and digits 0-9). MudDataGrid cell edit handles input restriction via the
    /// decimal type binding. Key navigation (arrows, ESC, Enter) is handled natively.</summary>
    private async Task OnProductLineCommitted(OrderReceptionProductLineDto item)
    {
        // Port of SaveEdits validation: ValidateTextBoxDouble / ValidateTextDouble
        // Ensure quantity is valid (non-negative)
        if (item.Quantity < 0)
        {
            Snackbar.Add("Quantity must be a non-negative number.", Severity.Warning);
            item.Quantity = 0m;
        }

        // Port of SaveEdits math:
        // previousLinePrice = current Price; linePrice = Quantity * UnitPrice
        decimal previousLinePrice = item.Price;
        decimal linePrice = item.Quantity * item.UnitPrice;
        item.Price = linePrice;

        // Port of ReCalculateTotals(previousLinePrice, linePrice)
        ReCalculateTotals(previousLinePrice, linePrice);

        _editingData = true;
        await Task.CompletedTask;
        StateHasChanged();
    }

    /// <summary>Port of <c>ReCalculateTotals</c>.</summary>
    private void ReCalculateTotals(decimal previous, decimal current)
    {
        // Port of legacy math exactly:
        // currentSubTotal = currentSubTotal - previous + current
        // currentTotalTax = currentSubTotal * currentTax * 0.01
        // currentTotal = currentFreightCharge + currentSubTotal + currentTotalTax
        decimal subTotal = _state!.CurrentSubTotal - previous + current;
        decimal tax = _state.CurrentTax;
        decimal freightCharge = _state.CurrentFreightCharge;
        decimal totalTax = subTotal * tax * 0.01m;
        decimal total = freightCharge + subTotal + totalTax;

        _state.CurrentSubTotal = subTotal;
        _state.CurrentTotalTax = totalTax;
        _state.CurrentTotal = total;

        _state.TxtSubTotalText = subTotal.ToString("#,##0.00");
        _state.TxtTotalTaxText = totalTax.ToString("#,##0.00");
        _state.TxtTotalText = total.ToString("#,##0.00");
    }

    /// <summary>Port of <c>txtFreightCharge_TextChanged</c> + <c>txtFreightCharge_KeyPress</c>.
    /// KeyPress validation (digits only) is replaced by numeric input type on the text field.</summary>
    private async Task OnFreightChargeChanged()
    {
        // Port of: currentFreightCharge = DoubleValue(txtFreightCharge.Text)
        decimal val = (decimal)FunctionsService.DoubleValue(_state!.TxtFreightChargeText ?? string.Empty);
        _state.CurrentFreightCharge = val;
        ReCalculateTotals(0m, 0m);
        _editingData = true;
        StateHasChanged();
    }

    /// <summary>Port of <c>txtSalesTax_TextChanged</c> + <c>txtSalesTax_KeyPress</c>.
    /// KeyPress validation (digits only) is replaced by numeric input type on the text field.</summary>
    private async Task OnSalesTaxChanged()
    {
        // Port of: currentTax = DoubleValue(txtSalesTax.Text)
        decimal val = (decimal)FunctionsService.DoubleValue(_state!.TxtSalesTaxText ?? string.Empty);
        _state.CurrentTax = val;
        ReCalculateTotals(0m, 0m);
        _editingData = true;
        StateHasChanged();
    }

    /// <summary>Port of <c>cmdSave_Click</c>. Delegates save logic to service with full
    /// transaction boundary, validation, and confirmation flow.</summary>
    private async Task OnSaveClick()
    {
        if (_state!.CurrentIdProvider == 0)
        {
            Snackbar.Add("Please select a provider before saving.", Severity.Warning);
            return;
        }

        // Pre-save validation: check that at least one product line has quantity > 0
        bool hasLines = false;
        foreach (var line in _productLines)
        {
            if (line.Quantity > 0)
            {
                hasLines = true;
                break;
            }
        }

        if (!hasLines)
        {
            Snackbar.Add("No product quantities entered. Please enter at least one product quantity.", Severity.Warning);
            return;
        }

        _isLoading = true;
        StateHasChanged();
        try
        {
            // Sync product lines into state for the service to persist
            _state.TxtEntryText = SerializeProductLines();
            _state.CurrentUserId = AppState.UserId;

            // Port of cmdSave_Click — service handles:
            // - Creating OrderReception record (ProviderId, ReceivedBy, OrderDate, Notes, FreightCharge, SalesTaxRate, Status=RECEIVED)
            // - Iterating product lines and inserting OrderReceptionDetails for each with Quantity != 0
            // - Transaction boundary (begin/commit/rollback)
            // - Lock retry on concurrency exception
            _state = await OrderReceptionService.SaveAsync(_state, this);

            _editingData = false;
            _isLoading = false;
            StateHasChanged();

            // Port of: confirmation dialog "Order reception added successfully / Would you like to add a new?"
            bool? addAnother = await DialogService.ShowMessageBox(
                "New data",
                "Order reception added successfully\nWould you like to add a new order reception?",
                yesText: "Yes",
                noText: "No");

            if (addAnother == true)
            {
                ClearFields();
                _providersList = new List<ProvidersDto>();
                StateHasChanged();
            }
            else
            {
                NavigationManager.NavigateTo("/");
            }
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException)
        {
            // Port of lock retry handling — retry once
            try
            {
                _state = await OrderReceptionService.SaveAsync(_state!, this);
                _editingData = false;
                _isLoading = false;
                StateHasChanged();
                Snackbar.Add("Order reception saved successfully after retry.", Severity.Success);
            }
            catch (Exception retryEx)
            {
                _isLoading = false;
                StateHasChanged();
                Snackbar.Add($"An error has occurred adding the data after retry.\nError: {retryEx.Message}", Severity.Error);
            }
        }
        catch (Exception ex)
        {
            // Port of: catch block showing error message
            _isLoading = false;
            StateHasChanged();
            Snackbar.Add($"An error has occurred adding the data.\nError: {ex.Message}", Severity.Error);
        }
    }

    /// <summary>Serializes product lines so the service can read them.
    /// Format: pipe-delimited lines of Quantity|ProductID|UnitPrice|Price per row.</summary>
    private string SerializeProductLines()
    {
        var sb = new System.Text.StringBuilder();
        foreach (var line in _productLines)
        {
            if (line.Quantity > 0)
            {
                sb.AppendLine($"{line.Quantity}|{line.ProductID}|{line.UnitPrice}|{line.Price}");
            }
        }
        return sb.ToString();
    }

    /// <summary>Port of <c>cmdClose_Click</c> + <c>Form_FormClosing</c>.
    /// If data has been edited, confirm before navigating away.</summary>
    private async Task OnCloseClick()
    {
        if (_editingData)
        {
            // Port of Form_FormClosing: three-way dialog (Yes/No/Cancel)
            bool? saveFirst = await DialogService.ShowMessageBox(
                "Save data",
                "Do you want to save the edited data?",
                yesText: "Yes",
                noText: "No",
                cancelText: "Cancel");

            if (saveFirst == true)
            {
                await OnSaveClick();
                return; // OnSaveClick handles navigation
            }
            else if (saveFirst is null)
            {
                // Cancel — stay on page
                return;
            }
            // No — fall through to navigate away
        }

        NavigationManager.NavigateTo("/");
    }

    // ──────────────────────────────────────────────
    // Port of Form_Closed — no-op body in legacy; cleanup in Dispose
    // ──────────────────────────────────────────────

    public void Dispose()
    {
        // Port of Form_Closed (empty body in legacy)
    }
}
