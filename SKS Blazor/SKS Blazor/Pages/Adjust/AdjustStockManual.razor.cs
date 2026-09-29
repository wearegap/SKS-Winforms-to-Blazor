using Microsoft.AspNetCore.Components;
using MudBlazor;
using SKS_Blazor.Application.DTOs.Adjust;
using SKS_Blazor.Application.DTOs.Core;
using SKS_Blazor.Application.Services.Adjust;
using SKS_Blazor.Application.Services.Functions;
using SKS_Blazor.Infrastructure;
using System.Globalization;
using SKS_Blazor.Domain.Entities.Core;
using SKS_Blazor.Domain.Entities.Manual;
using SKS_Blazor.Domain.Entities.Stock;

namespace SKS_Blazor.Pages.Adjust;

/// <summary>Port of <c>frmAdjustStockManual</c>.</summary>
public partial class AdjustStockManual : ComponentBase, IDisposable
{
    [Inject] private IAdjustStockManualService AdjustStockManualService { get; set; } = default!;
    [Inject] private IFunctionsService FunctionsService { get; set; } = default!;
    [Inject] private IDialogService DialogService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private AppState AppState { get; set; } = default!;

    // --- State (single instance for the page's lifetime) ---
    private AdjustStockManualStateDto? _state;

    // --- Grid data ---
    private List<ProductsDto> _products = new();
    private List<StocksDto> _stocks = new();

    // --- Selected items ---
    private ProductsDto? _selectedProduct;
    private StocksDto? _selectedStock;

    // --- Value text fields (mapped to txtValues[0] and txtValues[1]) ---
    private string _txtValuePrice = string.Empty;
    private string _txtValueQuantity = string.Empty;
    private bool _valuesReadOnly = true;

    // --- UI state ---
    private string _statusMessage = string.Empty;
    private bool _isLoading;

    // --- Local state mirroring legacy local vars for quantity display ---
    private string _currentQuantityPerUnit = "0";
    private string _currentUnit = string.Empty;

    protected override async Task OnInitializedAsync()
    {
        _state = new AdjustStockManualStateDto
        {
            EditingData = false,
            CurrentIdProduct = string.Empty,
            CurrentIdStock = 0,
            CurrentStockPrice = 0m,
            CurrentStock = 0m,
            ChangedStockPrice = 0m,
            ChangedStock = 0m,
            CodeGeneratedChange = false,
            TxtStockIDText = string.Empty,
            TxtOriginalPriceText = string.Empty,
            TxtQuantityPerUnitText = string.Empty,
            TxtProductNameText = string.Empty,
            TxtUnitText = string.Empty,
            TxtOriginalQuantityText = string.Empty,
            TxtCodeText = string.Empty,
            TxtNameText = string.Empty,
            LblNewQuantityText = string.Empty,
            LblCurrentQuantityText = string.Empty
        };

        await Task.CompletedTask;
    }

    // -----------------------------------------------------------------------
    // cmdProducts_Click — open Products dialog, get selected product ID, search
    // Port of legacy: frmProducts.DefInstance.ShowDialog(); txtCode.Text = ...
    // -----------------------------------------------------------------------
    private async Task OnCmdProductsClick()
    {
        var dialog = await DialogService.ShowAsync<SKS_Blazor.Pages.Products.Products>(
            "Products",
            new DialogOptions { MaxWidth = MaxWidth.Large, FullWidth = true, CloseButton = true });

        var result = await dialog.Result;

        if (result is not null && !result.Canceled && result.Data is string selectedProductId)
        {
            _state!.TxtCodeText = selectedProductId;
            _state.TxtNameText = string.Empty;
            await DoSearchProduct();
        }
    }

    // -----------------------------------------------------------------------
    // txtCode_TextChanged → DoSearchProduct
    // Port of legacy: private void txtCode_TextChanged(...) => DoSearchProduct();
    // -----------------------------------------------------------------------
    private async Task OnTxtCodeChanged()
    {
        await DoSearchProduct();
    }

    // -----------------------------------------------------------------------
    // txtName_TextChanged → DoSearchProduct
    // Port of legacy: private void txtName_TextChanged(...) => DoSearchProduct();
    // -----------------------------------------------------------------------
    private async Task OnTxtNameChanged()
    {
        await DoSearchProduct();
    }

    // -----------------------------------------------------------------------
    // DoSearchProduct — delegates to service
    // Port of legacy: private void DoSearchProduct() { ... ExecuteSql ... }
    // -----------------------------------------------------------------------
    private async Task DoSearchProduct()
    {
        _isLoading = true;
        StateHasChanged();
        try
        {
            _products = await AdjustStockManualService.DoSearchProductAsync(_state!);

            if (_products.Count == 0)
            {
                _statusMessage = "There are no records with the selected criteria";
                _stocks.Clear();
                _selectedProduct = null;
                _selectedStock = null;
            }
            else
            {
                _statusMessage = string.Empty;
                if (_products.Count == 1)
                {
                    _selectedProduct = _products[0];
                    await OnProductSelected(_selectedProduct);
                }
                else
                {
                    _selectedProduct = null;
                }
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error searching products: {ex.Message}", Severity.Error);
        }
        finally
        {
            _isLoading = false;
            StateHasChanged();
        }
    }

    // -----------------------------------------------------------------------
    // lvProducts_ItemClick → DoSearchStocks
    // Port of legacy: private void lvProducts_ItemClick(ListViewItem Item) => DoSearchStocks();
    // -----------------------------------------------------------------------
    private async Task OnProductSelected(ProductsDto? product)
    {
        if (product is null) return;
        _selectedProduct = product;

        // Guard: if editing, confirm cancel (legacy DoSearchStocks guard)
        if (_state!.EditingData)
        {
            bool? confirmCancel = await DialogService.ShowMessageBox(
                "Data edition",
                "Do you want to cancel previous edited data?",
                yesText: "Yes", noText: "No");

            if (confirmCancel != true) return;
        }

        _state.CurrentIdProduct = product.ProductID ?? string.Empty;

        // Extract QuantityPerUnit and Unit from the selected product
        _currentQuantityPerUnit = product.QuantityPerUnit?.ToString(CultureInfo.InvariantCulture) ?? "0";
        _currentUnit = product.Unit ?? string.Empty;

        _state.TxtProductNameText = product.ProductName ?? string.Empty;
        _state.TxtUnitText = _currentUnit;
        _state.TxtQuantityPerUnitText = _currentQuantityPerUnit;

        await DoSearchStocks();
    }

    // -----------------------------------------------------------------------
    // DoSearchStocks — delegates to service
    // Port of legacy: private void DoSearchStocks() { ... ExecuteSql ... }
    // -----------------------------------------------------------------------
    private async Task DoSearchStocks()
    {
        _isLoading = true;
        StateHasChanged();
        try
        {
            _stocks = await AdjustStockManualService.DoSearchStocksAsync(_state!);

            if (_stocks.Count == 0)
            {
                _statusMessage = $"There are no stock records of the product {_state!.TxtProductNameText}";
                _selectedStock = null;
                RetrieveDataProductEmpty();
            }
            else
            {
                _statusMessage = string.Empty;
                if (_stocks.Count == 1)
                {
                    _selectedStock = _stocks[0];
                    await OnStockSelected(_selectedStock);
                }
                else
                {
                    _selectedStock = null;
                }
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error searching stocks: {ex.Message}", Severity.Error);
        }
        finally
        {
            _isLoading = false;
            StateHasChanged();
        }
    }

    // -----------------------------------------------------------------------
    // lvStocks_ItemClick → RetrieveDataProduct
    // Port of legacy: private void lvStocks_ItemClick(ListViewItem Item) => RetrieveDataProduct();
    // -----------------------------------------------------------------------
    private async Task OnStockSelected(StocksDto? stock)
    {
        if (stock is null) return;
        _selectedStock = stock;
        await RetrieveDataProduct();
    }

    // -----------------------------------------------------------------------
    // RetrieveDataProduct — ported from legacy exactly
    // Port of legacy: private void RetrieveDataProduct() { ... }
    // -----------------------------------------------------------------------
    private async Task RetrieveDataProduct()
    {
        if (_state!.EditingData)
        {
            bool? confirmCancel = await DialogService.ShowMessageBox(
                "Data edition",
                "Do you want to cancel previous edited data?",
                yesText: "Yes", noText: "No");

            if (confirmCancel != true) return;
        }

        bool setEmpty = true;

        if (_selectedStock is not null)
        {
            _state.CurrentIdStock = _selectedStock.StockID;
            _state.CurrentStock = _selectedStock.Stock ?? 0m;
            _state.CurrentStockPrice = _selectedStock.StockPrice ?? 0m;

            _state.CodeGeneratedChange = true;
            _state.TxtOriginalQuantityText = _state.CurrentStock.ToString(CultureInfo.InvariantCulture);
            _state.TxtOriginalPriceText = _state.CurrentStockPrice.ToString(CultureInfo.InvariantCulture);
            _state.TxtStockIDText = _state.CurrentIdStock.ToString(CultureInfo.InvariantCulture);
            _txtValuePrice = _state.CurrentStockPrice.ToString(CultureInfo.InvariantCulture);
            _txtValueQuantity = _state.CurrentStock.ToString(CultureInfo.InvariantCulture);

            // Compute formatted quantity labels
            decimal.TryParse(_currentQuantityPerUnit, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal qtyPerUnit);
            decimal currentStockD = _state.CurrentStock;

            _state.LblNewQuantityText = $"{(currentStockD * qtyPerUnit):##,###.00}{_currentUnit}";
            _state.LblCurrentQuantityText = $"{(currentStockD * qtyPerUnit):##,###.00}{_currentUnit}";

            _state.CodeGeneratedChange = false;
            setEmpty = false;
            _valuesReadOnly = false;
        }

        if (setEmpty)
        {
            RetrieveDataProductEmpty();
        }

        _state.EditingData = false;
        StateHasChanged();
    }

    /// <summary>Helper to clear stock editing fields (the "setEmpty" branch of RetrieveDataProduct).</summary>
    private void RetrieveDataProductEmpty()
    {
        _state!.CodeGeneratedChange = true;
        _state.TxtOriginalQuantityText = string.Empty;
        _state.TxtOriginalPriceText = string.Empty;
        _state.TxtStockIDText = string.Empty;
        _txtValuePrice = string.Empty;
        _txtValueQuantity = string.Empty;
        _state.LblNewQuantityText = string.Empty;
        _state.LblCurrentQuantityText = string.Empty;
        _state.CodeGeneratedChange = false;
        _valuesReadOnly = true;
    }

    // -----------------------------------------------------------------------
    // txtValues_Enter — Port of legacy: modFunctions.SelectAll(txtValues[Index])
    // In Blazor, text selection on focus is browser-native; this is a no-op placeholder.
    // -----------------------------------------------------------------------
    private void OnTxtValuesEnter(int index)
    {
        // Legacy: modFunctions.SelectAll(txtValues[Index])
        // Browser handles text selection on focus natively for input type=number.
    }

    // -----------------------------------------------------------------------
    // txtValues_TextChanged — handles both txtValues[0] (price) and txtValues[1] (quantity)
    // Port of legacy: private void txtValues_TextChanged(Object eventSender, EventArgs eventArgs)
    // Also incorporates txtValues_KeyPress (number-only input handled by InputType.Number on MudTextField)
    // -----------------------------------------------------------------------
    private async Task OnTxtValueChanged(int index)
    {
        if (_state!.CodeGeneratedChange) return;

        _state.EditingData = true;
        _state.CodeGeneratedChange = true;

        // Parse the current text values
        if (!string.IsNullOrEmpty(_txtValuePrice))
        {
            if (decimal.TryParse(_txtValuePrice, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal parsedPrice))
                _state.ChangedStockPrice = parsedPrice;
        }

        if (!string.IsNullOrEmpty(_txtValueQuantity))
        {
            if (decimal.TryParse(_txtValueQuantity, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal parsedQty))
                _state.ChangedStock = parsedQty;
        }

        // Boundary check: cannot exceed original stock (legacy switch case 1)
        if (index == 1)
        {
            if (_state.ChangedStock > _state.CurrentStock)
            {
                _state.ChangedStock = _state.CurrentStock;
                _statusMessage = "Cannot pass the original stock, to add more, add a new stock manually";
                _txtValueQuantity = _state.ChangedStock.ToString(CultureInfo.InvariantCulture);
            }
        }

        // Delegate to service for any additional value computation
        _state = await AdjustStockManualService.ValuesAsync(_state, this);

        // Recompute formatted quantity labels
        decimal.TryParse(_currentQuantityPerUnit, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal qtyPerUnit);
        decimal changedStockD = _state.ChangedStock;
        decimal currentStockD = _state.CurrentStock;

        _state.LblNewQuantityText = $"{(changedStockD * qtyPerUnit):##,###.00}{_currentUnit}";
        _state.LblCurrentQuantityText = $"{(currentStockD * qtyPerUnit):##,###.00}{_currentUnit}";

        _state.CodeGeneratedChange = false;
        StateHasChanged();
    }

    // -----------------------------------------------------------------------
    // cmdSave_Click — delegates save to service with transaction handling
    // Port of legacy: private void cmdSave_Click(Object eventSender, EventArgs eventArgs)
    // Handles: UPDATE Stocks, INSERT ManualStocks, INSERT StockLog, UPDATE Products
    // -----------------------------------------------------------------------
    private async Task OnCmdSaveClick()
    {
        if (_state is null) return;

        _isLoading = true;
        StateHasChanged();
        try
        {
            // PRE-SAVE VALIDATION: update changed values from text fields
            if (string.IsNullOrWhiteSpace(_txtValuePrice) || string.IsNullOrWhiteSpace(_txtValueQuantity))
            {
                Snackbar.Add("Please enter both stock price and quantity values.", Severity.Warning);
                return;
            }

            if (!decimal.TryParse(_txtValuePrice, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal parsedPrice))
            {
                Snackbar.Add("Invalid stock price value.", Severity.Warning);
                return;
            }
            if (!decimal.TryParse(_txtValueQuantity, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal parsedQty))
            {
                Snackbar.Add("Invalid stock quantity value.", Severity.Warning);
                return;
            }

            _state.ChangedStockPrice = parsedPrice;
            _state.ChangedStock = parsedQty;

            // Check for no-change scenario (legacy: deltaStockPrice == 0 && deltaStock == 0)
            decimal deltaStockPrice = _state.ChangedStockPrice - _state.CurrentStockPrice;
            decimal deltaStock = _state.ChangedStock - _state.CurrentStock;

            if (deltaStockPrice == 0m && deltaStock == 0m)
            {
                _statusMessage = "There is no modification of the Stock, the data won't be saved";
                Snackbar.Add(_statusMessage, Severity.Info);
                return;
            }

            // Delegate to service — handles UPDATE Stocks, INSERT ManualStocks, INSERT StockLog, UPDATE Products
            // Transaction + lock retry + typed exceptions are handled inside the service
            _state = await AdjustStockManualService.SaveAsync(_state, AppState.UserId);
            _state.EditingData = false;

            // Ask if user wants to modify another stock (legacy: MessageBox.Show with YesNo)
            bool? continueEditing = await DialogService.ShowMessageBox(
                "Modify data",
                $"Data modified successfully{Environment.NewLine}Would you like to modify another stock manually?",
                yesText: "Yes", noText: "No");

            if (continueEditing == true)
            {
                ClearFields();
            }
            else
            {
                NavigationManager.NavigateTo("/");
            }
        }
        // Blazor signals a redirect by throwing NavigationException; catching
        // everything turns that redirect into an error and the page stays put.
        catch (Exception ex) when (ex is not NavigationException)
        {
            Snackbar.Add($"An error has occurred adding the data.{Environment.NewLine}Error: {ex.Message}", Severity.Error);
        }
        finally
        {
            _isLoading = false;
            StateHasChanged();
        }
    }

    // -----------------------------------------------------------------------
    // cmdClose_Click — with Form_FormClosing unsaved-data guard
    // Port of legacy: private void cmdClose_Click(...) => this.Close();
    // and Form_FormClosing(...)
    // -----------------------------------------------------------------------
    private async Task OnCmdCloseClick()
    {
        // Form_FormClosing: if editing, prompt to save
        if (_state is not null && _state.EditingData)
        {
            bool? saveResult = await DialogService.ShowMessageBox(
                "Save data",
                "Do you want to save the edited data?",
                yesText: "Yes", noText: "No", cancelText: "Cancel");

            if (saveResult == true)
            {
                await OnCmdSaveClick();
                return; // OnCmdSaveClick handles navigation after save
            }
            else if (saveResult is null)
            {
                // Cancel — stay on page
                return;
            }
            // else No — proceed to close
        }

        // Form_Closed — legacy was empty, just navigate away
        NavigationManager.NavigateTo("/");
    }

    // -----------------------------------------------------------------------
    // ClearFields — reset all form fields to initial state
    // Port of legacy: private void ClearFields() { ... }
    // -----------------------------------------------------------------------
    private void ClearFields()
    {
        _state!.CodeGeneratedChange = true;
        _txtValuePrice = string.Empty;
        _txtValueQuantity = string.Empty;
        _valuesReadOnly = true;
        _state.TxtCodeText = string.Empty;
        _state.TxtNameText = string.Empty;
        _state.TxtUnitText = string.Empty;
        _state.TxtStockIDText = string.Empty;
        _state.TxtOriginalPriceText = string.Empty;
        _state.TxtOriginalQuantityText = string.Empty;
        _state.TxtProductNameText = string.Empty;
        _state.TxtQuantityPerUnitText = string.Empty;
        _products.Clear();
        _stocks.Clear();
        _selectedProduct = null;
        _selectedStock = null;
        _state.LblCurrentQuantityText = string.Empty;
        _state.LblNewQuantityText = string.Empty;
        _state.CurrentIdProduct = string.Empty;
        _state.CurrentIdStock = 0;
        _state.CurrentStock = 0m;
        _state.CurrentStockPrice = 0m;
        _state.ChangedStock = 0m;
        _state.ChangedStockPrice = 0m;
        _state.EditingData = false;
        _state.CodeGeneratedChange = false;
        _statusMessage = string.Empty;
        _currentQuantityPerUnit = "0";
        _currentUnit = string.Empty;
        StateHasChanged();
    }

    // -----------------------------------------------------------------------
    // IDisposable — Form_Closed (legacy was empty)
    // -----------------------------------------------------------------------
    public void Dispose()
    {
        // Form_Closed — legacy was empty; no resources to clean up
    }
}
