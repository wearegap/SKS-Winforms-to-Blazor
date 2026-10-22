using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using SKS_Blazor.Application.DTOs.Core;
using SKS_Blazor.Application.DTOs.Products;
using SKS_Blazor.Application.DTOs.Search;
using SKS_Blazor.Application.Services.Products;
using SKS_Blazor.Application.Services.Search;
using SKS_Blazor.Application.Services.Functions;
using SKS_Blazor.Infrastructure;
using SKS_Blazor.Shared.Dialogs;
using SKS_Blazor.Domain.Entities.Core;

namespace SKS_Blazor.Pages.Products;

/// <summary>Port of <c>frmProducts</c> (WinForms product detail form with ADO data control navigation).</summary>
public partial class Products : ComponentBase, IDisposable
{
    [Inject] private IProductsService ProductsService { get; set; } = default!;
    [Inject] private ISearchService SearchService { get; set; } = default!;
    [Inject] private IFunctionsService FunctionsService { get; set; } = default!;
    [Inject] private IDialogService DialogService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private AppState AppState { get; set; } = default!;

    private ProductsStateDto? _state;
    private List<ProductsDto> _products = new();
    private List<CategoriesDto> _categories = new();
    private ProductsDto? _currentProduct;
    private ProductsDto? _originalProduct; // snapshot for cancel
    private int _currentIndex = -1;
    private bool _isLoading;
    private string _currentProductId = string.Empty;

    /// <summary>Grid view shows every record at once; the record navigator is hidden while it is on.</summary>
    private bool _gridView;
    private ProductsDto? _gridSelection;

    /// <summary>Port of <c>Form_Load</c>.</summary>
    protected override async Task OnInitializedAsync()
    {
        _state = new ProductsStateDto
        {
            NewMode = false,
            EditMode = false,
            CancellingMode = false,
            CmbCategorySelectedIndex = -1,
            TxtCategoryText = string.Empty
        };

        AppState.OnCommand += HandleAppCommand;

        await LoadDataAsync();
    }

    /// <summary>Port of <c>Form_Load</c> — loads categories and products on startup.</summary>
    private async Task LoadDataAsync()
    {
        _isLoading = true;
        StateHasChanged();
        try
        {
            // Port of modFunctions.LoadCombo("Categories", cmbCategory, "CategoryName", "CategoryID")
            await LoadCategoriesAsync();

            // Port of dcProducts data control — load all products
            await ReloadProductsAsync();

            if (_state != null)
            {
                _state.NewMode = false;
                _state.EditMode = false;
                _state.CancellingMode = false;
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error loading products: {ex.Message}", Severity.Error);
        }
        finally
        {
            _isLoading = false;
            StateHasChanged();
        }
    }

    /// <summary>Port of modFunctions.LoadCombo("Categories", cmbCategory, "CategoryName", "CategoryID").</summary>
    private async Task LoadCategoriesAsync()
    {
        try
        {
            // Use ProductsService.CategoryAsync to load categories into state
            _state = await ProductsService.CategoryAsync(_state!, null!, CancellationToken.None);

            // Also attempt to build a categories list from search
            var productsList = await SearchService.SearchCriteriaProductsAsync("ProductName", "", CancellationToken.None);
            if (productsList != null)
            {
                var distinctCatIds = productsList
                    .Where(p => p.CategoryID.HasValue)
                    .Select(p => p.CategoryID!.Value)
                    .Distinct()
                    .OrderBy(id => id)
                    .ToList();

                _categories = distinctCatIds.Select(id => new CategoriesDto
                {
                    CategoryID = id,
                    CategoryName = $"Category {id}"
                }).ToList();
            }
        }
        catch
        {
            _categories = new List<CategoriesDto>();
        }
    }

    /// <summary>Port of dcProducts.Recordset.Requery() — reload all products from the database.</summary>
    private async Task ReloadProductsAsync()
    {
        try
        {
            var productsList = await SearchService.SearchCriteriaProductsAsync("ProductName", "", CancellationToken.None);
            _products = productsList ?? new List<ProductsDto>();

            if (_products.Count > 0)
            {
                // Try to restore position to previously viewed product
                var restoreIndex = -1;
                if (!string.IsNullOrEmpty(_currentProductId))
                {
                    restoreIndex = _products.FindIndex(p =>
                        string.Equals(p.ProductID, _currentProductId, StringComparison.OrdinalIgnoreCase));
                }
                _currentIndex = restoreIndex >= 0 ? restoreIndex : 0;
                SetCurrentProduct(_currentIndex);
            }
            else
            {
                _currentIndex = -1;
                _currentProduct = null;
                _originalProduct = null;
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error loading product list: {ex.Message}", Severity.Error);
            _products = new List<ProductsDto>();
        }
    }

    /// <summary>Deep-copies the product at the given index into _currentProduct and _originalProduct.</summary>
    private void SetCurrentProduct(int index)
    {
        if (index >= 0 && index < _products.Count)
        {
            _currentIndex = index;
            var src = _products[index];
            _currentProduct = CloneProduct(src);
            _originalProduct = CloneProduct(src);
            _currentProductId = src.ProductID ?? string.Empty;

            // Port of txtCategory_TextChanged — sync category state
            SyncCategoryState(src.CategoryID);
        }
    }

    private static ProductsDto CloneProduct(ProductsDto src)
    {
        return new ProductsDto
        {
            ProductID = src.ProductID,
            ProductName = src.ProductName,
            ProductDescription = src.ProductDescription,
            CategoryID = src.CategoryID,
            UnitPrice = src.UnitPrice,
            UnitsInStock = src.UnitsInStock,
            UnitsOnOrder = src.UnitsOnOrder,
            Discontinued = src.Discontinued,
            SerialNumber = src.SerialNumber,
            LeadTime = src.LeadTime,
            Unit = src.Unit,
            QuantityPerUnit = src.QuantityPerUnit,
            ReorderLevel = src.ReorderLevel,
            SupplierID = src.SupplierID
        };
    }

    /// <summary>Port of <c>txtCategory_TextChanged</c> — syncs hidden txtCategory text field with combo selection.</summary>
    private void SyncCategoryState(int? categoryId)
    {
        if (_state == null) return;

        if (categoryId.HasValue)
        {
            _state.TxtCategoryText = categoryId.Value.ToString(CultureInfo.InvariantCulture);

            // Port of txtCategory_TextChanged: iterate combo items to find matching index
            // for (modMain.i = 0; modMain.i <= tempForEndVar; modMain.i++)
            //     if (cmbCategory.GetItemData(modMain.i) == StringsHelper.ToDoubleSafe(txtCategory.Text))
            int foundIndex = -1;
            for (int i = 0; i < _categories.Count; i++)
            {
                if (_categories[i].CategoryID == categoryId.Value)
                {
                    foundIndex = i;
                    break;
                }
            }
            _state.CmbCategorySelectedIndex = foundIndex;
        }
        else
        {
            // Port of: if (String.IsNullOrEmpty(txtCategory.Text)) { cmbCategory.SelectedIndex = -1; return; }
            _state.TxtCategoryText = string.Empty;
            _state.CmbCategorySelectedIndex = -1;
        }
    }

    // ── Grid view (no legacy equivalent: frmProducts only ever shows one record at a time) ──

    /// <summary>
    /// Toggles between the legacy one-record form plus navigator and a grid of every record.
    /// Refuses to leave the form mid-edit, because the grid has no Save and the edit would be lost.
    /// </summary>
    private void OnToggleGridView()
    {
        if (!_gridView && (_state?.EditMode == true || _state?.NewMode == true))
        {
            Snackbar.Add("Save or cancel the current record before switching to grid view.", Severity.Warning);
            return;
        }

        _gridView = !_gridView;
        _gridSelection = _gridView && _currentIndex >= 0 && _currentIndex < _products.Count
            ? _products[_currentIndex]
            : null;
        StateHasChanged();
    }

    /// <summary>Selecting a row makes it the current record, so switching back to the form lands on it.</summary>
    private void OnGridRowClick(DataGridRowClickEventArgs<ProductsDto> args)
    {
        var index = _products.FindIndex(p => string.Equals(p.ProductID, args.Item?.ProductID, StringComparison.Ordinal));
        if (index < 0) return;

        SetCurrentProduct(index);
        _gridSelection = _products[index];
        StateHasChanged();
    }

    /// <summary>Category name for the grid; the grid shows the name, not the raw CategoryID.</summary>
    private string CategoryName(int? categoryId)
    {
        if (categoryId is null) return string.Empty;
        return _categories.FirstOrDefault(c => c.CategoryID == categoryId.Value)?.CategoryName ?? string.Empty;
    }

    // ── Record navigation (port of dcProducts ADO data control MoveFirst/MovePrevious/MoveNext/MoveLast) ──

    private void OnFirstRecord()
    {
        if (_products.Count > 0)
        {
            SetCurrentProduct(0);
            StateHasChanged();
        }
    }

    private void OnPreviousRecord()
    {
        if (_currentIndex > 0)
        {
            SetCurrentProduct(_currentIndex - 1);
            StateHasChanged();
        }
    }

    private void OnNextRecord()
    {
        if (_currentIndex < _products.Count - 1)
        {
            SetCurrentProduct(_currentIndex + 1);
            StateHasChanged();
        }
    }

    private void OnLastRecord()
    {
        if (_products.Count > 0)
        {
            SetCurrentProduct(_products.Count - 1);
            StateHasChanged();
        }
    }

    // ── Toolbar button handlers (port of Toolbar1_ButtonClick — each case is a separate method) ──

    /// <summary>Port of <c>Toolbar1_ButtonClick</c> case "Add" — add new record.
    /// Legacy: NewMode = true; dcProducts.Recordset.AddNew(); 
    /// dcProducts.Recordset["UnitsInStock"] = 0; dcProducts.Recordset["UnitsOnOrder"] = 0; dcProducts.Recordset["Discontinued"] = 0;
    /// </summary>
    private void OnToolbarAdd()
    {
        if (_state == null) return;

        _state.NewMode = true;
        _state.EditMode = false;

        _currentProduct = new ProductsDto
        {
            ProductID = string.Empty,
            ProductName = string.Empty,
            ProductDescription = string.Empty,
            CategoryID = null,
            UnitPrice = null,
            UnitsInStock = 0,
            UnitsOnOrder = 0,
            Discontinued = 0,
            SerialNumber = string.Empty,
            LeadTime = string.Empty,
            Unit = string.Empty,
            QuantityPerUnit = null,
            ReorderLevel = null,
            SupplierID = null
        };
        _originalProduct = null;

        _state.TxtCategoryText = string.Empty;
        _state.CmbCategorySelectedIndex = -1;

        StateHasChanged();
    }

    /// <summary>Port of <c>Toolbar1_ButtonClick</c> case "Edit" — enter edit mode.
    /// Legacy: EditMode = true;
    /// </summary>
    private void OnToolbarEdit()
    {
        if (_state == null || _currentProduct == null) return;
        _state.EditMode = true;
        StateHasChanged();
    }

    /// <summary>Port of <c>Toolbar1_ButtonClick</c> case "Save" — save data.
    /// Legacy: dcProducts.Recordset.Update(); dcProducts.Recordset.Requery(); EditMode = false; NewMode = false;
    /// </summary>
    private async Task OnToolbarSave()
    {
        if (_state == null || _currentProduct == null) return;

        // PRE-SAVE VALIDATION — validate required fields before persistence
        if (string.IsNullOrWhiteSpace(_currentProduct.ProductID))
        {
            Snackbar.Add("Product ID is required.", Severity.Warning);
            return;
        }
        if (string.IsNullOrWhiteSpace(_currentProduct.ProductName))
        {
            Snackbar.Add("Product Name is required.", Severity.Warning);
            return;
        }

        _isLoading = true;
        StateHasChanged();
        try
        {
            // Port of dcProducts.Recordset.Update()
            await ProductsService.SaveProductAsync(new ProductSaveDto
            {
                ProductID = _currentProduct.ProductID,
                ProductName = _currentProduct.ProductName,
                ProductDescription = _currentProduct.ProductDescription,
                CategoryID = _currentProduct.CategoryID,
                UnitPrice = _currentProduct.UnitPrice,
                QuantityPerUnit = _currentProduct.QuantityPerUnit,
                UnitsInStock = _currentProduct.UnitsInStock,
                UnitsOnOrder = _currentProduct.UnitsOnOrder,
                Discontinued = _currentProduct.Discontinued,
                LeadTime = _currentProduct.LeadTime,
                SerialNumber = _currentProduct.SerialNumber,
                SupplierID = _currentProduct.SupplierID,
                Unit = _currentProduct.Unit,
                ReorderLevel = _currentProduct.ReorderLevel,
                IsNew = _state.NewMode
            }, CancellationToken.None);

            // Port of dcProducts.Recordset.Requery()
            _currentProductId = _currentProduct.ProductID ?? string.Empty;
            await ReloadProductsAsync();

            // Port of: EditMode = false; NewMode = false;
            _state.EditMode = false;
            _state.NewMode = false;

            // Try to navigate back to the saved product
            var savedIndex = _products.FindIndex(p =>
                string.Equals(p.ProductID, _currentProductId, StringComparison.OrdinalIgnoreCase));
            if (savedIndex >= 0)
            {
                SetCurrentProduct(savedIndex);
            }
            else if (_products.Count > 0)
            {
                SetCurrentProduct(0);
            }

            Snackbar.Add("Product saved successfully.", Severity.Success);
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error saving product: {ex.Message}", Severity.Error);
        }
        finally
        {
            _isLoading = false;
            StateHasChanged();
        }
    }

    /// <summary>Port of <c>Toolbar1_ButtonClick</c> case "Delete" — delete record.
    /// Legacy: MessageBox.Show("Are you sure you want to delete this record?", ...); 
    /// dcProducts.Recordset.Delete(); dcProducts.Recordset.Requery();
    /// </summary>
    private async Task OnToolbarDelete()
    {
        if (_state == null || _currentProduct == null) return;

        // Port of MessageBox.Show("Are you sure you want to delete this record?", "Delete record", YesNo, Question)
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
            // Port of dcProducts.Recordset.Delete()
            await ProductsService.DeleteProductAsync(_currentProduct.ProductID, CancellationToken.None);

            // Port of dcProducts.Recordset.Requery()
            await ReloadProductsAsync();

            Snackbar.Add("Product deleted successfully.", Severity.Success);
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error deleting product: {ex.Message}", Severity.Error);
        }
        finally
        {
            _isLoading = false;
            StateHasChanged();
        }
    }

    /// <summary>Port of <c>Toolbar1_ButtonClick</c> case "Search" — search for records.
    /// Legacy: modFunctions.SearchShow("Products", "ProductName", "product");
    /// </summary>
    private async Task OnToolbarSearch()
    {
        var parameters = new DialogParameters<SearchInputDialog>
        {
            { x => x.Title, "Search Products" },
            { x => x.FieldLabel, "Product name" }
        };

        var options = new DialogOptions
        {
            MaxWidth = MaxWidth.Small,
            FullWidth = true,
            CloseOnEscapeKey = true
        };

        var dialog = await DialogService.ShowAsync<SearchInputDialog>("Search Products", parameters, options);
        var result = await dialog.Result;

        if (result != null && !result.Canceled && result.Data is string searchValue && !string.IsNullOrEmpty(searchValue))
        {
            _isLoading = true;
            StateHasChanged();
            try
            {
                var searchState = new SearchStateDto
                {
                    SearchTable = "Products",
                    CboSrchByText = "ProductName",
                    LblSrchByText = string.Empty
                };

                await SearchService.SearchAsync(searchState, "Products", "ProductName", searchValue, CancellationToken.None);

                // Reload and try to find matching products
                var searchResults = await SearchService.SearchCriteriaProductsAsync("ProductName", searchValue, CancellationToken.None);
                if (searchResults != null && searchResults.Count > 0)
                {
                    _products = searchResults;
                    _currentIndex = 0;
                    SetCurrentProduct(0);
                }

                Snackbar.Add($"Found {_products.Count} product(s).", Severity.Info);
            }
            catch (Exception ex)
            {
                Snackbar.Add($"Search error: {ex.Message}", Severity.Error);
            }
            finally
            {
                _isLoading = false;
                StateHasChanged();
            }
        }
    }

    /// <summary>Port of <c>Toolbar1_ButtonClick</c> case "Cancel" — cancel edited changes.
    /// Legacy: CancellingMode = true; EditMode = false; NewMode = false; 
    /// dcProducts.Recordset.CancelUpdate(); dcProducts.Recordset.Requery(); CancellingMode = false; this.Close();
    /// </summary>
    private async Task OnToolbarCancel()
    {
        if (_state == null) return;

        _state.CancellingMode = true;

        // Port of: EditMode = false; NewMode = false;
        _state.EditMode = false;
        _state.NewMode = false;

        // Port of dcProducts.Recordset.CancelUpdate() — restore original values
        if (_originalProduct != null)
        {
            _currentProduct = CloneProduct(_originalProduct);
        }

        // Port of dcProducts.Recordset.Requery()
        await ReloadProductsAsync();

        _state.CancellingMode = false;

        // Port of this.Close() — navigate back to main page
        NavigationManager.NavigateTo("/");
    }

    // ── Category change handler (port of cmbCategory_SelectedIndexChanged + txtCategory_TextChanged) ──

    /// <summary>Port of <c>cmbCategory_SelectedIndexChanged</c>.
    /// Legacy: if (cmbCategory.Items.Count == 0 || cmbCategory.SelectedIndex == -1) return;
    /// txtCategory.Text = cmbCategory.GetItemData(cmbCategory.SelectedIndex).ToString();
    /// </summary>
    private async Task OnCategorySelectionChanged()
    {
        if (_state == null || _currentProduct == null) return;

        // Port of cmbCategory_SelectedIndexChanged:
        // Guard: if (cmbCategory.Items.Count == 0 || cmbCategory.SelectedIndex == -1) return;
        if (_categories.Count == 0) return;

        // Port: txtCategory.Text = cmbCategory.GetItemData(cmbCategory.SelectedIndex).ToString();
        // This triggers txtCategory_TextChanged which syncs the hidden category field
        SyncCategoryState(_currentProduct.CategoryID);

        // Delegate category state logic to service
        _state = await ProductsService.CategoryAsync(_state, this, CancellationToken.None);

        StateHasChanged();
    }

    /// <summary>Port of Discontinued checkbox toggle — maps bool ↔ int(0/1).</summary>
    private void OnDiscontinuedChanged(bool value)
    {
        if (_currentProduct != null)
        {
            _currentProduct.Discontinued = value ? 1 : 0;
        }
    }

    // ── Form lifecycle (port of frmProducts_Activated, Form_Closed) ──

    /// <summary>Port of <c>Form_Closed</c> — CurrentProductID = Convert.ToString(dcProducts.Recordset["ProductId"]).</summary>
    private void OnFormClosed()
    {
        if (_currentProduct != null)
        {
            _currentProductId = _currentProduct.ProductID ?? string.Empty;
        }
    }

    // ── AppState command handler (layout-to-page dispatch per rule 14) ──

    private async Task HandleAppCommand(string command)
    {
        switch (command)
        {
            case "add":
                OnToolbarAdd();
                break;
            case "edit":
                OnToolbarEdit();
                break;
            case "save":
                await OnToolbarSave();
                break;
            case "delete":
                await OnToolbarDelete();
                break;
            case "search":
                await OnToolbarSearch();
                break;
            case "cancel":
                await OnToolbarCancel();
                break;
        }
    }

    public void Dispose()
    {
        // Port of Form_Closed — persist current product ID
        OnFormClosed();

        // Unsubscribe from AppState
        if (AppState?.OnCommand != null)
        {
            AppState.OnCommand -= HandleAppCommand;
        }
    }
}
