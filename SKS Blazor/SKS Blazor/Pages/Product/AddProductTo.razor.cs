using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;
using SKS_Blazor.Application.DTOs.Core;
using SKS_Blazor.Application.DTOs.Product;
using SKS_Blazor.Application.DTOs.Products;
using SKS_Blazor.Application.Services.Product;
using SKS_Blazor.Application.Services.Functions;
using SKS_Blazor.Application.Services.Products;
using SKS_Blazor.Infrastructure;
using SKS_Blazor.Domain.Entities.Products;
using SKS_Blazor.Domain.Entities.Core;

namespace SKS_Blazor.Pages.Product;

/// <summary>Port of <c>frmAddProductTo</c>. Manages adding/removing product associations
/// to a parent entity (Customer or Provider).</summary>
public partial class AddProductTo : ComponentBase, IDisposable
{
    [Inject] private IAddProductToService AddProductToService { get; set; } = default!;
    [Inject] private IFunctionsService FunctionsService { get; set; } = default!;
    [Inject] private IProductsService ProductsService { get; set; } = default!;
    [Inject] private IDialogService DialogService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private AppState AppState { get; set; } = default!;

    // --- Parameters passed from parent (caller sets these before navigation/dialog open) ---
    /// <summary>The ID of the parent record (CustomerID or ProviderID).</summary>
    [Parameter]
    [SupplyParameterFromQuery(Name = "id")]
    public int Id { get; set; }

    /// <summary>Display name for the parent object, e.g. "Provider ACME".</summary>
    [Parameter]
    [SupplyParameterFromQuery(Name = "objectReferred")]
    public string ObjectReferred { get; set; } = "";

    /// <summary>Table name: "ProductsByCustomer" or "ProductsByProvider".</summary>
    [Parameter]
    [SupplyParameterFromQuery(Name = "table")]
    public string Table { get; set; } = "";

    /// <summary>Column name in the association table: "CustomerID" or "ProviderID".</summary>
    [Parameter]
    [SupplyParameterFromQuery(Name = "columnName")]
    public string ColumnName { get; set; } = "";

    // --- State ---
    private AddProductToStateDto _state = new();
    private List<ProductsDto> _searchResults = new();
    private List<AssociatedProductRow> _associatedProducts = new();
    private ProductsDto? _selectedProduct;
    private bool _checkAll;
    private string _statusMessage = "";
    private string _pageTitle = "Add Product To";
    private string _productsRelatedLabel = "Products related to";
    private bool _isLoading;

    // Query parameter tracking for OnParametersSetAsync
    private int _previousId;
    private string _previousObjectReferred = "";
    private string _previousTable = "";
    private string _previousColumnName = "";

    /// <summary>Local view-model row for the associated-products grid (lvProductsBy).
    /// Adds a checkbox selection flag on top of product data.</summary>
    public class AssociatedProductRow
    {
        public string ProductID { get; set; } = "";
        public string ProductName { get; set; } = "";
        public decimal? UnitPrice { get; set; }
        public string QtyPerUnit { get; set; } = "";
        public bool IsChecked { get; set; }
    }

    // ────────────────────── Lifecycle ──────────────────────

    /// <summary>Port of <c>frmAddProductTo_Activated</c> + <c>LoadData</c>.
    /// Initialize state; subscriptions.</summary>
    protected override async Task OnInitializedAsync()
    {
        _state = new AddProductToStateDto
        {
            ProductsStored = new OrderedDictionary(StringComparer.OrdinalIgnoreCase),
            ProductsToDelete = new OrderedDictionary(StringComparer.OrdinalIgnoreCase),
            ProductsToAdd = new OrderedDictionary(StringComparer.OrdinalIgnoreCase),
            EditingData = false,
            CurrentIdProduct = "",
            TxtCodeText = "",
            TxtNameText = ""
        };

        _pageTitle = $"Add product(s) to {ObjectReferred}";
        _productsRelatedLabel = $"Products related to {ObjectReferred}";

        await Task.CompletedTask;
    }

    /// <summary>Called when route parameters change; Blazor reuses component instance when
    /// query string changes, so this fires instead of OnInitializedAsync. Load data when
    /// Id, Table, or ColumnName parameters differ from the last load.</summary>
    protected override async Task OnParametersSetAsync()
    {
        // Guard: only reload if query parameters actually changed
        if (_previousId == Id
            && _previousObjectReferred == ObjectReferred
            && _previousTable == Table
            && _previousColumnName == ColumnName)
        {
            return; // No parameter change; skip reload
        }

        _previousId = Id;
        _previousObjectReferred = ObjectReferred;
        _previousTable = Table;
        _previousColumnName = ColumnName;

        // Update page title/label for new parameters
        _pageTitle = $"Add product(s) to {ObjectReferred}";
        _productsRelatedLabel = $"Products related to {ObjectReferred}";

        // Reset state for new context
        _state = new AddProductToStateDto
        {
            ProductsStored = new OrderedDictionary(StringComparer.OrdinalIgnoreCase),
            ProductsToDelete = new OrderedDictionary(StringComparer.OrdinalIgnoreCase),
            ProductsToAdd = new OrderedDictionary(StringComparer.OrdinalIgnoreCase),
            EditingData = false,
            CurrentIdProduct = "",
            TxtCodeText = "",
            TxtNameText = ""
        };

        _searchResults.Clear();
        _associatedProducts.Clear();
        _selectedProduct = null;
        _checkAll = false;
        _statusMessage = "";

        // Legacy frmAddProductTo received Table/ColumnName/Id/ObjectReferred as public fields;
        // the service reads them from the context.
        _state.Context = new AddProductToContextDto
        {
            Table = Table,
            ColumnName = ColumnName,
            Id = Id,
            ObjectReferred = ObjectReferred
        };

        // LoadData → delegates to service which queries existing associations
        _state = await AddProductToService.LoadDataAsync(_state);

        // LoadProductsById — build the associated grid from state
        await LoadProductsByIdAsync();
    }

    /// <summary>Port of <c>Form_Closed</c> — cleanup.</summary>
    public void Dispose()
    {
        // Form_Closed was empty in legacy; nothing specific to clean up
    }

    // ────────────────────── Event Handlers ──────────────────────

    /// <summary>Port of <c>txtCode_TextChanged</c> — triggers product search on code change.</summary>
    private async Task OnTxtCodeChanged()
    {
        await DoSearchProductAsync();

        // Port of txtCode_Leave: if exactly one result, auto-add it
        if (_searchResults.Count == 1)
        {
            _selectedProduct = _searchResults[0];
            _state.CurrentIdProduct = _selectedProduct.ProductID ?? "";
            await AddProductToSetAsync();
        }
    }

    /// <summary>Port of <c>txtName_TextChanged</c> — triggers product search on name change.</summary>
    private async Task OnTxtNameChanged()
    {
        await DoSearchProductAsync();
    }

    /// <summary>Port of <c>chkAll_CheckStateChanged</c> — select/deselect all associated products.
    /// Legacy iterates all lvProductsBy items setting Checked = check.</summary>
    private void OnChkAllChanged(bool newValue)
    {
        _checkAll = newValue;
        foreach (var item in _associatedProducts)
        {
            item.IsChecked = _checkAll;
        }
    }

    /// <summary>Port of <c>lvProducts_ItemClick</c> — clicking a search result adds it to associations.</summary>
    private async Task OnLvProductsRowClick(DataGridRowClickEventArgs<ProductsDto> args)
    {
        _selectedProduct = args.Item;
        _state.CurrentIdProduct = args.Item.ProductID ?? "";
        await AddProductToSetAsync();
    }

    /// <summary>Tracks selected product in the search grid.</summary>
    private void OnSelectedProductChanged(ProductsDto? item)
    {
        _selectedProduct = item;
        if (item != null)
        {
            _state.CurrentIdProduct = item.ProductID ?? "";
        }
    }

    /// <summary>Checkbox change handler for individual associated product rows.</summary>
    private void OnAssociatedProductChecked(AssociatedProductRow row, bool isChecked)
    {
        row.IsChecked = isChecked;
        // Update the "check all" state to reflect whether all are checked
        _checkAll = _associatedProducts.Count > 0 && _associatedProducts.All(p => p.IsChecked);
    }

    /// <summary>Port of <c>cmdProducts_Click</c> — opens the Products dialog to pick a product.
    /// Legacy: frmProducts.DefInstance.ShowDialog(); txtCode.Text = frmProducts.DefInstance.CurrentProductID;</summary>
    private async Task OnCmdProductsClick()
    {
        var parameters = new DialogParameters<SKS_Blazor.Pages.Products.Products>();
        var options = new DialogOptions
        {
            MaxWidth = MaxWidth.Large,
            FullWidth = true,
            CloseOnEscapeKey = true,
            CloseButton = true
        };

        var dialog = await DialogService.ShowAsync<SKS_Blazor.Pages.Products.Products>(
            "Products", parameters, options);
        var result = await dialog.Result;

        if (result != null && !result.Canceled && result.Data is string selectedProductId)
        {
            _state.TxtCodeText = selectedProductId;
            _state.TxtNameText = "";
            await DoSearchProductAsync();
        }
    }

    /// <summary>Port of <c>cmdRemove_Click</c> — removes checked items from the associated products list.
    /// Legacy iterates in reverse: for i = lvProductsBy.Items.Count downto 1, checks Checked flag,
    /// tracks in productsStored/productsToAdd/productsToDelete, removes from list.</summary>
    private async Task OnCmdRemoveClick()
    {
        _isLoading = true;
        try
        {
            // Build list of checked items first (iterate in reverse for safe removal — legacy pattern)
            var checkedItems = new List<AssociatedProductRow>();
            for (int i = _associatedProducts.Count - 1; i >= 0; i--)
            {
                if (_associatedProducts[i].IsChecked)
                {
                    checkedItems.Add(_associatedProducts[i]);
                }
            }

            if (checkedItems.Count == 0)
            {
                Snackbar.Add("No products selected for removal.", Severity.Info);
                return;
            }

            foreach (var item in checkedItems)
            {
                string productIdToDelete = item.ProductID;

                // Mirror the legacy cmdRemove_Click logic exactly:
                bool existsInStored = FunctionsService.Exists(
                    _state.ProductsStored, productIdToDelete);

                if (existsInStored)
                {
                    // Product was originally stored in DB
                    bool existsInToAdd = FunctionsService.Exists(
                        _state.ProductsToAdd, productIdToDelete);
                    if (existsInToAdd)
                    {
                        // Legacy: productsToDelete.Remove(productIdToDelete)
                        _state.ProductsToDelete.Remove(productIdToDelete);
                    }
                    else
                    {
                        // Mark for deletion: AddToCollection(productsToDelete, productIdToDelete)
                        await FunctionsService.AddToCollectionAsync(
                            _state.ProductsToDelete, productIdToDelete);
                    }
                }
                else
                {
                    // Not in stored — was a new addition; remove from toAdd using currentIdProduct
                    bool existsInToAdd = FunctionsService.Exists(
                        _state.ProductsToAdd, productIdToDelete);
                    if (existsInToAdd)
                    {
                        _state.ProductsToAdd.Remove(productIdToDelete);
                    }
                }

                _associatedProducts.Remove(item);
            }

            _state.EditingData = true;
            _checkAll = false;
            _statusMessage = $"Removed {checkedItems.Count} product(s).";
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error removing products: {ex.Message}", Severity.Error);
        }
        finally
        {
            _isLoading = false;
        }
    }

    /// <summary>Port of <c>cmdSave_Click</c> — persists additions and deletions to the database.
    /// Legacy iterates productsToAdd.Values and productsToDelete.Values, executing INSERT/DELETE for each.</summary>
    private async Task OnCmdSaveClick()
    {
        _isLoading = true;
        try
        {
            // Pre-save validation: check if there are changes to save
            // Legacy: if productsToAdd.Count == 0 && productsToDelete.Count == 0 → show msg, close
            if (_state.ProductsToAdd.Count == 0 && _state.ProductsToDelete.Count == 0)
            {
                _state.EditingData = false;
                Snackbar.Add("No data to be saved.", Severity.Info);
                NavigationManager.NavigateTo("/");
                return;
            }

            // Delegate save to service — it handles the iteration over productsToAdd
            // and productsToDelete, inserting/deleting rows in the association table
            _state = await AddProductToService.SaveAsync(_state, this);

            _state.EditingData = false;
            Snackbar.Add("Data was successfully saved.", Severity.Success);
            NavigationManager.NavigateTo("/");
        }
        // Blazor signals a redirect by throwing NavigationException; catching
        // everything turns that redirect into an error and the page stays put.
        catch (Exception ex) when (ex is not NavigationException)
        {
            Snackbar.Add($"An error has occurred adding the data.\nError: {ex.Message}",
                Severity.Error);
        }
        finally
        {
            _isLoading = false;
        }
    }

    /// <summary>Port of <c>cmdClose_Click</c> + <c>Form_FormClosing</c> —
    /// Legacy: if editingData, prompt Yes/No/Cancel. Yes → save. No → close without saving. Cancel → stay.
    /// </summary>
    private async Task OnCmdCloseClick()
    {
        if (_state.EditingData)
        {
            // Form_FormClosing: prompt user to save
            var result = await DialogService.ShowMessageBox(
                "Save data",
                "Do you want to save the edited data?",
                yesText: "Yes",
                noText: "No",
                cancelText: "Cancel");

            if (result == true)
            {
                // User chose Yes — save then close
                await OnCmdSaveClick();
                return; // OnCmdSaveClick navigates away on success
            }
            else if (result == null)
            {
                // User chose Cancel — stay on page
                return;
            }
            // User chose No — fall through to navigate away without saving
        }

        NavigationManager.NavigateTo("/");
    }

    // ────────────────────── Business Logic Methods ──────────────────────

    /// <summary>Port of <c>DoSearchProduct</c> — queries products by code/name filter.
    /// Legacy builds a WHERE clause with ProductId LIKE and ProductName LIKE, then iterates rs rows.</summary>
    private async Task DoSearchProductAsync()
    {
        try
        {
            _searchResults = await AddProductToService.DoSearchProductAsync(_state);

            _statusMessage = $"There are {_searchResults.Count} records with the selected criteria";

            // Legacy: if exactly one result, auto-select it
            if (_searchResults.Count == 1)
            {
                _selectedProduct = _searchResults[0];
                _state.CurrentIdProduct = _selectedProduct.ProductID ?? "";
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Search error: {ex.Message}", Severity.Error);
        }
    }

    /// <summary>Port of <c>LoadProductsById</c> — loads existing product associations from DB
    /// and populates the associated-products grid. Legacy iterates rs rows, adds each productCode
    /// to productsStored via AddToCollection, and populates lvProductsBy with sub-items.</summary>
    private async Task LoadProductsByIdAsync()
    {
        try
        {
            _associatedProducts.Clear();

            if (_state.ProductsStored != null && _state.ProductsStored.Count > 0)
            {
                // Iterate each stored product code (PARAMETER ITERATION — legacy while(!rs.EOF) loop)
                foreach (string productCode in _state.ProductsStored.Values)
                {
                    // Look up product details
                    var tempState = new AddProductToStateDto
                    {
                        TxtCodeText = productCode,
                        TxtNameText = "",
                        ProductsStored = _state.ProductsStored,
                        ProductsToAdd = _state.ProductsToAdd,
                        ProductsToDelete = _state.ProductsToDelete,
                        EditingData = false,
                        CurrentIdProduct = productCode
                    };

                    var products = await AddProductToService.DoSearchProductAsync(tempState);
                    var product = products.FirstOrDefault(p =>
                        string.Equals(p.ProductID, productCode, StringComparison.OrdinalIgnoreCase));

                    // Legacy sub-items: [0]=ProductID, [1]=ProductName, [2]=UnitPrice, [3]=QuantityPerUnit+Unit
                    _associatedProducts.Add(new AssociatedProductRow
                    {
                        ProductID = productCode,
                        ProductName = product?.ProductName ?? "",
                        UnitPrice = product?.UnitPrice,
                        QtyPerUnit = $"{product?.QuantityPerUnit}{product?.Unit}",
                        IsChecked = false
                    });
                }
            }

            _statusMessage = $"There are {_associatedProducts.Count} associated products";
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error loading associated products: {ex.Message}", Severity.Error);
        }
    }

    /// <summary>Port of <c>AddProductToSet</c> — adds the selected search-result product
    /// to the associated-products list, maintaining sorted order and collection tracking.
    /// Legacy iterates lvProductsBy to find duplicate or sorted insert position, then
    /// checks productsStored/productsToDelete/productsToAdd for collection tracking.</summary>
    private async Task AddProductToSetAsync()
    {
        if (_selectedProduct == null)
            return;

        string currentId = _selectedProduct.ProductID ?? "";
        _state.CurrentIdProduct = currentId;

        // Check if already in associated list — iterate to find position (legacy sorted insert)
        bool found = false;
        int insertIndex = _associatedProducts.Count; // default: append at end

        for (int i = 0; i < _associatedProducts.Count; i++)
        {
            if (string.Equals(_associatedProducts[i].ProductID, currentId, StringComparison.OrdinalIgnoreCase))
            {
                // Already exists — just highlight it (legacy: set Selected = true)
                found = true;
                break;
            }
            else if (string.CompareOrdinal(_associatedProducts[i].ProductID, currentId) > 0)
            {
                // Found the sorted insert position
                insertIndex = i;
                break;
            }
        }

        if (!found)
        {
            _state.EditingData = true;

            // Track in collections — mirror legacy AddProductToSet logic exactly
            bool existsInStored = FunctionsService.Exists(
                _state.ProductsStored, currentId);

            if (!existsInStored)
            {
                // Not originally in DB
                bool existsInToDelete = FunctionsService.Exists(
                    _state.ProductsToDelete, currentId);
                if (existsInToDelete)
                {
                    // Was marked for deletion — unmark it
                    _state.ProductsToDelete.Remove(currentId);
                }
                else
                {
                    // Brand new addition
                    await FunctionsService.AddToCollectionAsync(
                        _state.ProductsToAdd, currentId);
                }
            }
            else
            {
                // Was originally in DB — check if marked for deletion
                bool existsInToDelete = FunctionsService.Exists(
                    _state.ProductsToDelete, currentId);
                if (existsInToDelete)
                {
                    // Restore it — remove from delete list
                    _state.ProductsToDelete.Remove(currentId);
                }
            }

            // Build the row from the selected search result
            // Legacy maps: sub-item 1 = ProductName, sub-item 2 = UnitPrice,
            //              sub-item 3 = QuantityPerUnit + Unit (from search grid cols 5+6)
            var newRow = new AssociatedProductRow
            {
                ProductID = currentId,
                ProductName = _selectedProduct.ProductName ?? "",
                UnitPrice = _selectedProduct.UnitPrice,
                QtyPerUnit = $"{_selectedProduct.QuantityPerUnit}{_selectedProduct.Unit}",
                IsChecked = false
            };

            // Insert at sorted position
            if (insertIndex >= _associatedProducts.Count)
                _associatedProducts.Add(newRow);
            else
                _associatedProducts.Insert(insertIndex, newRow);
        }
        else
        {
            Snackbar.Add($"Product {currentId} is already in the list.", Severity.Info);
        }
    }
}
