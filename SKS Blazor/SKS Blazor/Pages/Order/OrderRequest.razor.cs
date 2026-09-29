using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.EntityFrameworkCore;
using MudBlazor;
using SKS_Blazor.Application.DTOs.Core;
using SKS_Blazor.Application.DTOs.Order;
using SKS_Blazor.Application.DTOs.Product;
using SKS_Blazor.Application.Services.Order;
using SKS_Blazor.Application.Services.Functions;
using SKS_Blazor.Infrastructure;
using SKS_Blazor.Domain.Entities.Core;
using SKS_Blazor.Domain.Entities.Products;
using SKS_Blazor.Domain.Entities.Order;

namespace SKS_Blazor.Pages.Order;

/// <summary>Port of <c>frmOrderRequest</c>.</summary>
public partial class OrderRequest : ComponentBase, IDisposable
{
    [Inject] private IOrderRequestService OrderRequestService { get; set; } = default!;
    [Inject] private IOrderReceptionService OrderReceptionService { get; set; } = default!;
    [Inject] private IFunctionsService FunctionsService { get; set; } = default!;
    [Inject] private IDialogService DialogService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private AppState AppState { get; set; } = default!;

    // --- Shared state (single instance for page lifetime) ---
    private OrderRequestStateDto? _state;

    // --- UI-bound fields ---
    private List<CustomersDto> _customers = new();
    private List<OrderRequestProductLineDto> _productLines = new();

    // Filter fields for customer search
    private string _companyNameFilter = string.Empty;
    private string _contactNameFilter = string.Empty;
    private string _contactLastNameFilter = string.Empty;

    // Freight / tax text fields (mirror _state but allow user editing)
    private string _freightChargeText = string.Empty;
    private string _salesTaxText = string.Empty;

    // Date pickers
    private DateTime? _dtRequired;
    private DateTime? _dtPromised;

    // Control enable/disable
    private bool _cmdSaveEnabled;
    private bool _cmdAddProductsEnabled;
    private bool _dtRequiredEnabled;
    private bool _dtPromisedEnabled;

    // Editing tracking
    private bool _editingData;
    private bool _editingQuantity;
    private bool _isLoading;

    // Selected customer row
    private CustomersDto? _selectedCustomer;

    /// <summary>Port of <c>Form_Load</c>.</summary>
    protected override async Task OnInitializedAsync()
    {
        _state = new OrderRequestStateDto
        {
            CurrentIdCustomer = 0,
            EditingData = false,
            CurrentSubTotal = 0m,
            CurrentTotal = 0m,
            CurrentTax = 0m,
            CurrentFreightCharge = 0m,
            CurrentTotalTax = 0m,
            EditingQuantity = false,
            TxtSubTotalText = string.Empty,
            TxtTotalText = string.Empty,
            TxtTotalTaxText = string.Empty,
            TxtFreightChargeText = string.Empty,
            TxtSalesTaxText = string.Empty,
            TxtEntryText = string.Empty,
            TxtContactLastNameText = string.Empty,
            TxtContactNameText = string.Empty,
            TxtCompanyNameText = string.Empty,
            TxtCustomerContactText = string.Empty,
            TxtCustomerCompanyText = string.Empty
        };

        _editingData = false;
        ClearFields();

        await Task.CompletedTask;
    }

    // =====================================================================
    // Customer Search — txtCompanyName_TextChanged, txtContactName_TextChanged, txtContactLastName_TextChanged
    // =====================================================================

    /// <summary>Port of <c>txtCompanyName_TextChanged</c>.</summary>
    private async Task OnCompanyNameChanged()
    {
        if (_state == null) return;
        _state.TxtCompanyNameText = _companyNameFilter;
        await DoSearchCustomerAsync();
    }

    /// <summary>Port of <c>txtContactName_TextChanged</c>.</summary>
    private async Task OnContactNameChanged()
    {
        if (_state == null) return;
        _state.TxtContactNameText = _contactNameFilter;
        await DoSearchCustomerAsync();
    }

    /// <summary>Port of <c>txtContactLastName_TextChanged</c>.</summary>
    private async Task OnContactLastNameChanged()
    {
        if (_state == null) return;
        _state.TxtContactLastNameText = _contactLastNameFilter;
        await DoSearchCustomerAsync();
    }

    /// <summary>Port of <c>DoSearchCustomer</c> — delegates to service.
    /// Handles the filter construction and result display from the legacy
    /// method that built WHERE clauses from txtCompanyName, txtContactName,
    /// txtContactLastName text fields.</summary>
    private async Task DoSearchCustomerAsync(string id = "")
    {
        if (_state == null) return;
        _isLoading = true;
        StateHasChanged();
        try
        {
            _customers = await OrderRequestService.DoSearchCustomerAsync(_state, id);
            if (_customers.Count == 0 && string.IsNullOrEmpty(id))
            {
                AppState.LogStatus("There are no records with the selected criteria");
            }
            else if (_customers.Count == 1)
            {
                _selectedCustomer = _customers[0];
                await RetrieveDataCustomerAsync(_selectedCustomer);
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error searching customers: {ex.Message}", Severity.Error);
        }
        finally
        {
            _isLoading = false;
            StateHasChanged();
        }
    }

    // =====================================================================
    // Customer Selection — lvCustomers_ItemClick, RetrieveDataCustomer
    // =====================================================================

    /// <summary>Port of <c>lvCustomers_ItemClick</c>.</summary>
    private async Task OnCustomerRowClick(DataGridRowClickEventArgs<CustomersDto> args)
    {
        _selectedCustomer = args.Item;
        await RetrieveDataCustomerAsync(_selectedCustomer);
    }

    /// <summary>Port of <c>lvCustomers_ItemClick</c> (alternate path from SelectedItemChanged).</summary>
    private async Task OnCustomerSelected(CustomersDto? item)
    {
        if (item == null) return;
        _selectedCustomer = item;
        await RetrieveDataCustomerAsync(_selectedCustomer);
    }

    /// <summary>Port of <c>RetrieveDataCustomer</c>.</summary>
    private async Task RetrieveDataCustomerAsync(CustomersDto customer)
    {
        if (_state == null) return;

        if (_editingData)
        {
            var confirmResult = await DialogService.ShowMessageBox(
                "Data edition",
                "Do you want to cancel previous edited data?",
                yesText: "Yes", noText: "No");
            if (confirmResult != true)
            {
                return;
            }
        }

        _state.CurrentIdCustomer = customer.CustomerID;
        var companyName = customer.CompanyName ?? string.Empty;
        var contactName = $"{customer.ContactFirstName ?? string.Empty} {customer.ContactLastName ?? string.Empty}".Trim();
        _state.TxtCustomerCompanyText = companyName;
        _state.TxtCustomerContactText = contactName;
        _editingData = false;
        _state.EditingData = false;

        await LoadProductsByIdAsync();

        _cmdSaveEnabled = true;
        _cmdAddProductsEnabled = true;
        _dtRequiredEnabled = true;
        _dtPromisedEnabled = true;

        StateHasChanged();
    }

    /// <summary>Port of <c>LoadProductsById</c> — delegates to service.
    /// The legacy code built a FlexGrid with columns:
    /// Quantity(editable=0), Code, Product, UnitPrice, Price(=0), Existence, Ordered, QuantityPerUnit.
    /// Each row iterated through rs records populating column values.</summary>
    private async Task LoadProductsByIdAsync()
    {
        if (_state == null) return;
        _isLoading = true;
        StateHasChanged();
        try
        {
            var products = await OrderRequestService.LoadProductsByIdAsync(_state);

            _productLines = products.Select(p => new OrderRequestProductLineDto
            {
                Quantity = 0m,
                ProductCode = p.ProductID ?? string.Empty,
                ProductName = p.ProductName ?? string.Empty,
                UnitPrice = p.UnitPrice ?? 0m,
                LinePrice = 0m,
                UnitsInStock = p.UnitsInStock ?? 0,
                UnitsOnOrder = p.UnitsOnOrder ?? 0,
                QuantityPerUnit = $"{(p.QuantityPerUnit?.ToString() ?? string.Empty)}{p.Unit ?? string.Empty}"
            }).ToList();
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

    // =====================================================================
    // Customers Dialog — cmdCustomers_Click
    // =====================================================================

    /// <summary>Port of <c>cmdCustomers_Click</c>.
    /// Legacy: frmCustomers.DefInstance.ShowDialog(), then grab CurrentCustomerID,
    /// clear search fields, DoSearchCustomer with returned ID.</summary>
    private async Task OnCustomersClick()
    {
        var parameters = new DialogParameters();
        var options = new DialogOptions { MaxWidth = MaxWidth.Large, FullWidth = true, CloseOnEscapeKey = true };
        var dialog = await DialogService.ShowAsync<SKS_Blazor.Pages.Customers.Customers>("Customers", parameters, options);
        var result = await dialog.Result;

        _companyNameFilter = string.Empty;
        _contactLastNameFilter = string.Empty;
        _contactNameFilter = string.Empty;

        if (_state != null)
        {
            _state.TxtCompanyNameText = string.Empty;
            _state.TxtContactLastNameText = string.Empty;
            _state.TxtContactNameText = string.Empty;
        }

        if (result != null && !result.Canceled && result.Data is string customerId && !string.IsNullOrEmpty(customerId))
        {
            await DoSearchCustomerAsync(customerId);
        }

        StateHasChanged();
    }

    // =====================================================================
    // Add Products Dialog — cmdAddProducts_Click
    // =====================================================================

    /// <summary>Port of <c>cmdAddProducts_Click</c>.
    /// Legacy: set frmAddProductTo.DefInstance properties (Id, ObjectReferred, Table, ColumnName),
    /// call LoadData, ShowDialog, then if SavedChanges call LoadProductsById.</summary>
    private async Task OnAddProductsClick()
    {
        if (_state == null) return;

        var parameters = new DialogParameters
        {
            { "Id", _state.CurrentIdCustomer },
            { "ObjectReferred", $"Customer {_state.TxtCustomerCompanyText}|{_state.TxtCustomerContactText}" },
            { "Table", "ProductsByCustomer" },
            { "ColumnName", "CustomerId" }
        };
        var options = new DialogOptions { MaxWidth = MaxWidth.Large, FullWidth = true, CloseOnEscapeKey = true };
        var dialog = await DialogService.ShowAsync<SKS_Blazor.Pages.Product.AddProductTo>("Add Products", parameters, options);
        var result = await dialog.Result;

        if (result != null && !result.Canceled)
        {
            await LoadProductsByIdAsync();
        }

        StateHasChanged();
    }

    // =====================================================================
    // Date Pickers — dtPromised_ValueChanged, dtRequired_ValueChanged
    // =====================================================================

    /// <summary>Port of <c>dtPromised_ValueChanged</c>.
    /// Legacy: editingData = true.</summary>
    private Task OnPromisedDateChanged()
    {
        _editingData = true;
        if (_state != null) _state.EditingData = true;
        return Task.CompletedTask;
    }

    /// <summary>Port of <c>dtRequired_ValueChanged</c>.
    /// Legacy: editingData = true.</summary>
    private Task OnRequiredDateChanged()
    {
        _editingData = true;
        if (_state != null) _state.EditingData = true;
        return Task.CompletedTask;
    }

    // =====================================================================
    // Grid Cell Editing — fgProducts_Click, fgProducts_EnterCell,
    // fgProducts_KeyPress, fgProducts_CellLeave, txtEntry_Leave, SaveEdits
    // =====================================================================

    /// <summary>Port of <c>fgProducts_Click</c> and <c>MakeTextBoxVisible</c>.
    /// Legacy: if CurrentColumnIndex != 0, return; else MakeTextBoxVisible(txtEntry, fgProducts); SelectAll(txtEntry).
    /// In Blazor MudDataGrid EditMode=Cell, clicking Quantity column enters edit mode automatically.
    /// fgProducts_KeyPress legacy checked for digits/period only — MudNumericField handles this natively.</summary>
    private void OnProductRowClick(DataGridRowClickEventArgs<OrderRequestProductLineDto> args)
    {
        _editingQuantity = true;
        if (_state != null) _state.EditingQuantity = true;
    }

    /// <summary>Port of <c>fgProducts_CellLeave</c> and <c>txtEntry_Leave</c>.
    /// Legacy: if editingQuantity then SaveEdits().
    /// Called when the user cancels editing on a cell.</summary>
    private void OnCanceledEditingItem(OrderRequestProductLineDto item)
    {
        if (_editingQuantity)
        {
            SaveEditsForLine(item);
        }
    }

    /// <summary>Port of <c>fgProducts_EnterCell</c> / CommittedItemChanges.
    /// Called when the user commits an inline cell edit. Recalculate line price and totals.
    /// This is the primary handler for <c>SaveEdits</c> logic.</summary>
    private void OnCommittedItemChanges(OrderRequestProductLineDto item)
    {
        SaveEditsForLine(item);
    }

    /// <summary>Port of <c>SaveEdits</c>.
    /// Legacy logic:
    /// 1. if !editingQuantity or !ValidateTextBoxDouble(txtEntry) or !ValidateTextDouble(fgProducts[row,4]), return.
    /// 2. previousLinePrice = DoubleValue(fgProducts[row, 4])
    /// 3. fgProducts[row, currentCol] = txtEntry.Text  (quantity)
    /// 4. lineQuantity = DoubleValue(txtEntry.Text)
    /// 5. lineUnitPrice = DoubleValue(fgProducts[row, 3])
    /// 6. linePrice = lineQuantity * lineUnitPrice
    /// 7. fgProducts[row, 4] = linePrice
    /// 8. ReCalculateTotals(previousLinePrice, linePrice)
    /// 9. editingQuantity = false; txtEntry.Enabled = false; txtEntry.Text = ""; editingData = true.</summary>
    private void SaveEditsForLine(OrderRequestProductLineDto line)
    {
        if (!_editingQuantity) return;

        if (line.Quantity < 0)
        {
            Snackbar.Add("Invalid quantity value", Severity.Warning);
            return;
        }

        decimal previousLinePrice = line.LinePrice;
        decimal linePrice = line.Quantity * line.UnitPrice;
        line.LinePrice = linePrice;

        ReCalculateTotals(previousLinePrice, linePrice);

        _editingQuantity = false;
        if (_state != null) _state.EditingQuantity = false;
        _editingData = true;
        if (_state != null) _state.EditingData = true;

        StateHasChanged();
    }

    // =====================================================================
    // Totals — ReCalculateTotals, FreightCharge, SalesTax
    // =====================================================================

    /// <summary>Port of <c>ReCalculateTotals</c>.
    /// Legacy:
    ///   currentSubTotal = currentSubTotal - previous + current
    ///   currentTotalTax = currentSubTotal * currentTax * 0.01
    ///   currentTotal = currentFreightCharge + currentSubTotal + currentTotalTax
    ///   txtSubTotal.Text = Format(currentSubTotal, "#,##0.00")
    ///   txtTotalTax.Text = Format(currentTotalTax, "#,##0.00")
    ///   txtTotal.Text = Format(currentTotal, "#,##0.00")</summary>
    private void ReCalculateTotals(decimal previous, decimal current)
    {
        if (_state == null) return;

        var currentSubTotal = _state.CurrentSubTotal - previous + current;
        var currentTax = _state.CurrentTax;
        var currentFreightCharge = _state.CurrentFreightCharge;
        var currentTotalTax = currentSubTotal * currentTax * 0.01m;
        var currentTotal = currentFreightCharge + currentSubTotal + currentTotalTax;

        _state.CurrentSubTotal = currentSubTotal;
        _state.CurrentTotalTax = currentTotalTax;
        _state.CurrentTotal = currentTotal;

        _state.TxtSubTotalText = currentSubTotal.ToString("#,##0.00");
        _state.TxtTotalTaxText = currentTotalTax.ToString("#,##0.00");
        _state.TxtTotalText = currentTotal.ToString("#,##0.00");
    }

    /// <summary>Port of <c>txtFreightCharge_TextChanged</c>.
    /// Legacy: currentFreightCharge = DoubleValue(txtFreightCharge.Text); ReCalculateTotals(0, 0); editingData = true.
    /// Port of <c>txtFreightCharge_KeyPress</c>: numeric-only enforcement handled by InputType.Number.</summary>
    private async Task OnFreightChargeChanged()
    {
        if (_state == null) return;
        var freightValue = FunctionsService.DoubleValue(_freightChargeText ?? string.Empty);
        _state.CurrentFreightCharge = (decimal)freightValue;
        _state.TxtFreightChargeText = _freightChargeText ?? string.Empty;
        ReCalculateTotals(0m, 0m);
        _editingData = true;
        _state.EditingData = true;
        StateHasChanged();
    }

    /// <summary>Port of <c>txtSalesTax_TextChanged</c>.
    /// Legacy: currentTax = DoubleValue(txtSalesTax.Text); ReCalculateTotals(0, 0); editingData = true.
    /// Port of <c>txtSalesTax_KeyPress</c>: numeric-only enforcement handled by InputType.Number.</summary>
    private async Task OnSalesTaxChanged()
    {
        if (_state == null) return;
        var taxValue = FunctionsService.DoubleValue(_salesTaxText ?? string.Empty);
        _state.CurrentTax = (decimal)taxValue;
        _state.TxtSalesTaxText = _salesTaxText ?? string.Empty;
        ReCalculateTotals(0m, 0m);
        _editingData = true;
        _state.EditingData = true;
        StateHasChanged();
    }

    // =====================================================================
    // Save — cmdSave_Click
    // =====================================================================

    /// <summary>Port of <c>cmdSave_Click</c>.
    /// Legacy:
    /// 1. ExecuteSql("Select * from OrderRequests"); rs.AddNew()
    /// 2. Set CustomerId, EmployeeId, OrderDate=Today, RequiredByDate, PromisedByDate, FreightCharge, SalesTaxRate=currentTax*0.01, Status="REQUESTED"
    /// 3. rs.Update()
    /// 4. SELECT last_insert_rowid() to get newOrderId
    /// 5. PARAMETERITERATION: For i=1 to fgProducts.RowsCount-1, if Quantity != "0":
    ///    Insert into OrderRequestDetails(OrderID, ProductID, DateSold, Quantity, UnitPrice, SalePrice, LineTotal)
    ///    Update Products Set UnitsOnOrder = UnitsOnOrder + Quantity Where ProductId = ...
    /// 6. editingData = false
    /// 7. Ask user "Would you like to add a new order?" Yes=>ClearFields, No=>Close
    /// TRANSACTIONBOUNDARY: the service wraps all inserts/updates in a transaction.</summary>
    private async Task OnSaveClick()
    {
        if (_state == null) return;

        _isLoading = true;
        StateHasChanged();
        try
        {
            if (_state.CurrentIdCustomer <= 0)
            {
                Snackbar.Add("Please select a customer before saving.", Severity.Warning);
                return;
            }

            bool hasLines = _productLines.Any(p => p.Quantity > 0);
            if (!hasLines)
            {
                Snackbar.Add("Please set quantities for at least one product.", Severity.Warning);
                return;
            }

            _state.TxtFreightChargeText = _freightChargeText ?? string.Empty;
            _state.TxtSalesTaxText = _salesTaxText ?? string.Empty;
            _state.TxtEntryText = string.Empty;

            // Pass product lines to the service via a transient field on state
            _state.ProductLines = _productLines;
            _state.RequiredByDate = _dtRequired;
            _state.PromisedByDate = _dtPromised;
            _state.CurrentUserId = AppState.UserId;

            _state = await OrderRequestService.SaveAsync(_state, this);

            _editingData = false;
            _state.EditingData = false;

            var addAnother = await DialogService.ShowMessageBox(
                "New data",
                "Order added successfully\nWould you like to add a new order?",
                yesText: "Yes", noText: "No");

            if (addAnother == true)
            {
                ClearFields();
            }
            else
            {
                NavigationManager.NavigateTo("/");
            }
        }
        catch (DbUpdateConcurrencyException)
        {
            try
            {
                _state!.ProductLines = _productLines;
                _state.RequiredByDate = _dtRequired;
                _state.PromisedByDate = _dtPromised;
                _state = await OrderRequestService.SaveAsync(_state, this);
                _editingData = false;
                Snackbar.Add("Order saved successfully after retry.", Severity.Success);
            }
            catch (Exception retryEx)
            {
                Snackbar.Add($"Error saving order after retry: {retryEx.Message}", Severity.Error);
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add($"An error has occurred adding the data.\nError: {ex.Message}", Severity.Error);
        }
        finally
        {
            _isLoading = false;
            StateHasChanged();
        }
    }

    // =====================================================================
    // Close — cmdClose_Click, Form_FormClosing
    // =====================================================================

    /// <summary>Port of <c>cmdClose_Click</c> and <c>Form_FormClosing</c>.
    /// Legacy Form_FormClosing:
    ///   if editingData then ask "Do you want to save the edited data?" Yes/No/Cancel
    ///   Yes => cmdSave_Click; No => close; Cancel => stay.</summary>
    private async Task OnCloseClick()
    {
        if (_editingData)
        {
            var result = await DialogService.ShowMessageBox(
                "Save data",
                "Do you want to save the edited data?",
                yesText: "Yes", noText: "No", cancelText: "Cancel");

            if (result == true)
            {
                await OnSaveClick();
                return;
            }
            else if (result == null)
            {
                return;
            }
        }

        NavigationManager.NavigateTo("/");
    }

    // =====================================================================
    // ClearFields
    // =====================================================================

    /// <summary>Port of <c>ClearFields</c>.
    /// Legacy: zero out fgProducts, all totals, all text fields, disable buttons/dates,
    /// call ReCalculateTotals(0, 0), editingData = false.</summary>
    private void ClearFields()
    {
        _productLines = new List<OrderRequestProductLineDto>();

        if (_state != null)
        {
            _state.CurrentSubTotal = 0m;
            _state.CurrentTotal = 0m;
            _state.CurrentTax = 0m;
            _state.CurrentTotalTax = 0m;
            _state.CurrentFreightCharge = 0m;

            _state.TxtSubTotalText = string.Empty;
            _state.TxtTotalText = string.Empty;
            _state.TxtTotalTaxText = string.Empty;
            _state.TxtFreightChargeText = string.Empty;
            _state.TxtSalesTaxText = string.Empty;

            _state.TxtCompanyNameText = string.Empty;
            _state.TxtContactLastNameText = string.Empty;
            _state.TxtContactNameText = string.Empty;
            _state.TxtCustomerContactText = string.Empty;
            _state.TxtCustomerCompanyText = string.Empty;
            _state.EditingData = false;
        }

        _companyNameFilter = string.Empty;
        _contactNameFilter = string.Empty;
        _contactLastNameFilter = string.Empty;
        _freightChargeText = string.Empty;
        _salesTaxText = string.Empty;

        _cmdSaveEnabled = false;
        _cmdAddProductsEnabled = false;
        _dtRequiredEnabled = false;
        _dtPromisedEnabled = false;

        _dtRequired = null;
        _dtPromised = null;
        _customers = new List<CustomersDto>();
        _selectedCustomer = null;

        ReCalculateTotals(0m, 0m);
        _editingData = false;
        _editingQuantity = false;

        StateHasChanged();
    }

    /// <summary>Port of <c>Form_Closed</c> — empty in legacy, cleanup handled by Dispose.</summary>
    public void Dispose()
    {
        // Port of Form_Closed — legacy was empty.
    }
}
