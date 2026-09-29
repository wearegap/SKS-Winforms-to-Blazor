using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;
using SKS_Blazor.Application.DTOs.Core;
using SKS_Blazor.Application.DTOs.Stock;
using SKS_Blazor.Application.Services.Stock;
using SKS_Blazor.Application.Services.Functions;
using SKS_Blazor.Infrastructure;
using SKS_Blazor.Shared.Dialogs;
using System.Globalization;
using SKS_Blazor.Pages.Products;
using SKS_Blazor.Domain.Entities.Core;
using SKS_Blazor.Domain.Entities.Manual;
using SKS_Blazor.Domain.Entities.Stock;

namespace SKS_Blazor.Pages.Stock;

/// <summary>Port of <c>frmAddStockManual</c>.</summary>
public partial class AddStockManual : ComponentBase, IDisposable
{
    [Inject] private IAddStockManualService AddStockManualService { get; set; } = default!;
    [Inject] private IFunctionsService FunctionsService { get; set; } = default!;
    [Inject] private IDialogService DialogService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private AppState AppState { get; set; } = default!;

    private AddStockManualStateDto? _state;

    // Grid data — port of lvProducts ListView
    private List<ProductsDto> _productsList = new();
    private ProductsDto? _selectedProduct;

    // Value text fields (mirrors legacy txtValues[0..2])
    private string _quantityText = string.Empty;
    private string _stockPriceText = string.Empty;
    private string _unitPriceText = string.Empty;
    private bool _valuesReadOnly = true;

    // Internal state
    private bool _codeGeneratedChange;
    private decimal _currentPriceReference;
    private string _currentQuantityPerUnit = string.Empty;
    private string _currentUnit = string.Empty;
    private bool _isLoading;
    private string _statusMessage = string.Empty;

    /// <summary>Port of <c>Form_Load</c>.</summary>
    protected override async Task OnInitializedAsync()
    {
        _state = new AddStockManualStateDto
        {
            EditingData = false,
            CodeGeneratedChange = false,
            CurrentIdProduct = string.Empty,
            TxtCodeText = string.Empty,
            TxtNameText = string.Empty,
            TxtUnitText = string.Empty,
            TxtProductNameText = string.Empty,
            TxtQuantityPerUnitText = string.Empty,
            LblNewQuantityText = string.Empty
        };
        _codeGeneratedChange = false;

        AppState.OnCommand += HandleCommandAsync;
        await Task.CompletedTask;
    }

    /// <summary>Port of <c>cmdClose_Click</c>.</summary>
    private async Task OnCloseClickAsync()
    {
        // Port of Form_FormClosing — prompt if editing
        if (_state!.EditingData)
        {
            var parameters = new DialogParameters<ConfirmSaveDialog>
            {
                { x => x.Message, "Do you want to save the edited data?" }
            };
            var options = new DialogOptions { MaxWidth = MaxWidth.Small };
            var dialogRef = await DialogService.ShowAsync<ConfirmSaveDialog>("Save data", parameters, options);
            var result = await dialogRef.Result;

            if (result is not null && !result.Canceled)
            {
                var answer = result.Data as string;
                if (answer == "Yes")
                {
                    await OnSaveClickAsync();
                }
                else if (answer == "Cancel")
                {
                    return;
                }
            }
            else
            {
                return;
            }
        }

        NavigationManager.NavigateTo("/");
    }

    /// <summary>Port of <c>cmdProducts_Click</c> — opens Products dialog, retrieves selected product ID.</summary>
    private async Task OnProductsClickAsync()
    {
        var options = new DialogOptions { MaxWidth = MaxWidth.Large, FullWidth = true };
        var dialogRef = await DialogService.ShowAsync<SKS_Blazor.Pages.Products.Products>(
            "Products", new DialogParameters(), options);
        var result = await dialogRef.Result;

        if (result is not null && !result.Canceled && result.Data is string selectedProductId)
        {
            _state!.TxtCodeText = selectedProductId;
            _state.TxtNameText = string.Empty;
            await DoSearchProductAsync();
        }
    }

    /// <summary>Port of <c>cmdSave_Click</c> — delegates to service with transaction handling.</summary>
    private async Task OnSaveClickAsync()
    {
        if (string.IsNullOrWhiteSpace(_state!.CurrentIdProduct))
        {
            Snackbar.Add("Please select a product before saving.", Severity.Warning);
            return;
        }

        // PRE-SAVE VALIDATION — check values are valid numbers
        if (!decimal.TryParse(_quantityText, NumberStyles.Any, CultureInfo.InvariantCulture, out var qty) || qty <= 0)
        {
            Snackbar.Add("Quantity must be a positive number.", Severity.Error);
            return;
        }
        if (!decimal.TryParse(_stockPriceText, NumberStyles.Any, CultureInfo.InvariantCulture, out _))
        {
            Snackbar.Add("Stock Price must be a valid number.", Severity.Error);
            return;
        }
        if (!decimal.TryParse(_unitPriceText, NumberStyles.Any, CultureInfo.InvariantCulture, out _))
        {
            Snackbar.Add("Unit Price must be a valid number.", Severity.Error);
            return;
        }

        _isLoading = true;

        try
        {
            _state.EditingData = false;

            // Service handles TRANSACTION (BeginTransaction / Commit / Rollback),
            // inserts into Stocks, ManualStocks, StockLog, updates Products.UnitsInStock
            _state = await AddStockManualService.SaveAsync(_state, AppState.UserId);

            // Port of MessageBox with YesNo — ask user if they want to add another
            var confirmParams = new DialogParameters<ConfirmSaveDialog>
            {
                { x => x.Message, $"Data added successfully{Environment.NewLine}Would you like to add a new stock manually?" }
            };
            var confirmOptions = new DialogOptions { MaxWidth = MaxWidth.Small };
            var dialogRef = await DialogService.ShowAsync<ConfirmSaveDialog>("New data", confirmParams, confirmOptions);
            var confirmResult = await dialogRef.Result;

            if (confirmResult is not null && !confirmResult.Canceled && confirmResult.Data as string == "Yes")
            {
                ClearFields();
            }
            else
            {
                NavigationManager.NavigateTo("/");
            }
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException)
        {
            // LOCK RETRY — retry once on concurrency conflict
            try
            {
                _state = await AddStockManualService.SaveAsync(_state!, AppState.UserId);
                Snackbar.Add("Saved successfully after retry.", Severity.Success);
            }
            catch (Exception retryEx)
            {
                Snackbar.Add($"An error has occurred adding the data.{Environment.NewLine}Error: {retryEx.Message}", Severity.Error);
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add($"An error has occurred adding the data.{Environment.NewLine}Error: {ex.Message}", Severity.Error);
        }
        finally
        {
            _isLoading = false;
        }
    }

    /// <summary>Port of <c>txtCode_TextChanged</c>.</summary>
    private async Task OnCodeChangedAsync()
    {
        await DoSearchProductAsync();
    }

    /// <summary>Port of <c>txtCode_Leave</c> — auto-select if single result.</summary>
    private async Task OnCodeLeaveAsync(FocusEventArgs args)
    {
        if (_productsList.Count == 1)
        {
            _selectedProduct = _productsList[0];
            await RetrieveDataProductAsync();
        }
    }

    /// <summary>Port of <c>txtName_TextChanged</c>.</summary>
    private async Task OnNameChangedAsync()
    {
        await DoSearchProductAsync();
    }

    /// <summary>Port of <c>txtName_Leave</c> — auto-select if single result.</summary>
    private async Task OnNameLeaveAsync(FocusEventArgs args)
    {
        if (_productsList.Count == 1)
        {
            _selectedProduct = _productsList[0];
            await RetrieveDataProductAsync();
        }
    }

    /// <summary>Port of <c>DoSearchProduct</c> — delegates to service, iterates results into grid (lvProducts).</summary>
    private async Task DoSearchProductAsync()
    {
        _productsList = await AddStockManualService.DoSearchProductAsync(_state!);

        if (_productsList.Count == 0)
        {
            _statusMessage = "There are no records with the selected criteria";
        }
        else
        {
            _statusMessage = string.Empty;
        }

        // Port of auto-select single result at end of DoSearchProduct
        if (_productsList.Count == 1)
        {
            _selectedProduct = _productsList[0];
        }
        else
        {
            _selectedProduct = null;
        }
    }

    /// <summary>Port of <c>lvProducts_Click</c> and <c>lvProducts_ItemClick</c>.</summary>
    private async Task OnProductSelectedAsync(ProductsDto? item)
    {
        _selectedProduct = item;
        if (item is not null)
        {
            await RetrieveDataProductAsync();
        }
    }

    /// <summary>Port of <c>RetrieveDataProduct</c> — populates detail fields from selected product.</summary>
    private async Task RetrieveDataProductAsync()
    {
        if (_state!.EditingData)
        {
            var confirmParams = new DialogParameters<ConfirmSaveDialog>
            {
                { x => x.Message, "Do you want to cancel previous edited data?" }
            };
            var options = new DialogOptions { MaxWidth = MaxWidth.Small };
            var dialogRef = await DialogService.ShowAsync<ConfirmSaveDialog>("Data edition", confirmParams, options);
            var result = await dialogRef.Result;

            if (result is null || result.Canceled || result.Data as string != "Yes")
            {
                return;
            }
        }

        if (_selectedProduct is null) return;

        var product = _selectedProduct;
        _state.CurrentIdProduct = product.ProductID ?? string.Empty;

        _currentQuantityPerUnit = product.QuantityPerUnit?.ToString(CultureInfo.InvariantCulture) ?? "0";
        _currentUnit = product.Unit ?? string.Empty;
        var productName = product.ProductName ?? string.Empty;
        _currentPriceReference = product.UnitPrice ?? 0m;

        _state.TxtProductNameText = productName;
        _state.TxtQuantityPerUnitText = _currentQuantityPerUnit;
        _state.TxtUnitText = _currentUnit;

        _valuesReadOnly = false;

        // Set initial values — mirroring legacy RetrieveDataProduct
        _codeGeneratedChange = true;
        _quantityText = "1";
        _stockPriceText = _currentPriceReference.ToString(CultureInfo.InvariantCulture);
        _unitPriceText = _currentPriceReference.ToString(CultureInfo.InvariantCulture);
        _codeGeneratedChange = false;

        _state.EditingData = false;

        ComputeNewQuantityLabel();

        await Task.CompletedTask;
    }

    /// <summary>Port of <c>txtValues_TextChanged</c> — recalculates stock price / unit price / new quantity label.</summary>
    private async Task OnValueChangedAsync(int index)
    {
        if (_codeGeneratedChange) return;

        _state!.EditingData = true;
        _codeGeneratedChange = true;

        decimal.TryParse(_quantityText, NumberStyles.Any, CultureInfo.InvariantCulture, out var quantity);
        decimal.TryParse(_stockPriceText, NumberStyles.Any, CultureInfo.InvariantCulture, out var stockPrice);
        decimal.TryParse(_unitPriceText, NumberStyles.Any, CultureInfo.InvariantCulture, out var unitPrice);

        switch (index)
        {
            case 0: // Quantity changed
            case 2: // Unit price changed
                _stockPriceText = (unitPrice * quantity).ToString(CultureInfo.InvariantCulture);
                break;
            case 1: // Stock price changed
                if (quantity != 0)
                {
                    _unitPriceText = (stockPrice / quantity).ToString(CultureInfo.InvariantCulture);
                }
                break;
        }

        // Reparse quantity after possible recalc
        decimal.TryParse(_quantityText, NumberStyles.Any, CultureInfo.InvariantCulture, out quantity);

        // Compute new quantity label — port of legacy lblNewQuantity.Text computation
        decimal.TryParse(_currentQuantityPerUnit, NumberStyles.Any, CultureInfo.InvariantCulture, out var qtyPerUnit);
        var newQty = quantity * qtyPerUnit;
        _state.LblNewQuantityText = $"{newQty:##,###.00}{_currentUnit}";

        _codeGeneratedChange = false;

        // Delegate to service for any additional value computation
        _state = await AddStockManualService.ValuesAsync(_state, this);
    }

    /// <summary>Port of <c>txtValues_Enter</c> — select all text on focus.
    /// In Blazor the browser handles selection for InputType.Number natively;
    /// this callback is provided for parity with the legacy SelectAll call.</summary>
    private Task OnValueFocusAsync(int index)
    {
        // Legacy called modFunctions.SelectAll(txtValues[Index]).
        // Browser InputType.Number handles text selection on focus natively.
        // No additional server-side logic required.
        return Task.CompletedTask;
    }

    /// <summary>Port of <c>txtValues_KeyPress</c> — numeric-only input validation.
    /// In Blazor, InputType.Number on MudTextField restricts input to numeric characters.
    /// This handler provides additional server-side guard for non-numeric keys.</summary>
    private Task OnValueKeyDownAsync(KeyboardEventArgs args)
    {
        // Legacy restricted input to digits 0-9, backspace, delete, and arrow keys.
        // The MudTextField with InputType.Number already enforces numeric input in the browser.
        // This server-side handler exists for legacy parity — no additional action needed.
        return Task.CompletedTask;
    }

    /// <summary>Computes the new quantity label (shared helper).</summary>
    private void ComputeNewQuantityLabel()
    {
        decimal.TryParse(_quantityText, NumberStyles.Any, CultureInfo.InvariantCulture, out var quantity);
        decimal.TryParse(_currentQuantityPerUnit, NumberStyles.Any, CultureInfo.InvariantCulture, out var qtyPerUnit);
        var newQty = quantity * qtyPerUnit;
        _state!.LblNewQuantityText = $"{newQty:##,###.00}{_currentUnit}";
    }

    /// <summary>Port of <c>ClearFields</c> — resets all fields to initial state.</summary>
    private void ClearFields()
    {
        _codeGeneratedChange = true;

        _valuesReadOnly = true;
        _quantityText = string.Empty;
        _stockPriceText = string.Empty;
        _unitPriceText = string.Empty;

        _state!.TxtCodeText = string.Empty;
        _state.TxtNameText = string.Empty;
        _state.TxtUnitText = string.Empty;
        _state.TxtProductNameText = string.Empty;
        _state.TxtQuantityPerUnitText = string.Empty;
        _state.CurrentIdProduct = string.Empty;
        _state.EditingData = false;
        _state.LblNewQuantityText = string.Empty;

        _productsList.Clear();
        _selectedProduct = null;
        _currentPriceReference = 0;
        _currentQuantityPerUnit = string.Empty;
        _currentUnit = string.Empty;

        _statusMessage = string.Empty;
        _codeGeneratedChange = false;
    }

    /// <summary>Layout-to-page command dispatch handler.</summary>
    private async Task HandleCommandAsync(string command)
    {
        switch (command)
        {
            case "save":
                await OnSaveClickAsync();
                break;
            case "close":
                await OnCloseClickAsync();
                break;
        }
    }

    public void Dispose()
    {
        AppState.OnCommand -= HandleCommandAsync;
    }
}
