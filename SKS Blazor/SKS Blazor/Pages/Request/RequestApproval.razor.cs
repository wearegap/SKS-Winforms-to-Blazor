using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using SKS_Blazor.Application.DTOs.Order;
using SKS_Blazor.Application.DTOs.Request;
using SKS_Blazor.Application.Services.Request;
using SKS_Blazor.Application.Services.Functions;
using SKS_Blazor.Infrastructure;
using SKS_Blazor.Pages.Action;
using SKS_Blazor.Pages.Customers;
using SKS_Blazor.Domain.Entities.Order;
using SKS_Blazor.Domain.Entities.Core;
using SKS_Blazor.Application.DTOs.Customers;
using SKS_Blazor.Application.DTOs.Core;

namespace SKS_Blazor.Pages.Request;

/// <summary>Port of <c>frmRequestApproval</c> (frmRequestAproval.cs).</summary>
public partial class RequestApproval : ComponentBase, IDisposable
{
    [Inject] private IRequestApprovalService RequestApprovalService { get; set; } = default!;
    [Inject] private IFunctionsService FunctionsService { get; set; } = default!;
    [Inject] private IDialogService DialogService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private AppState AppState { get; set; } = default!;

    // --- Shared state (single instance for page lifetime) ---
    private RequestApprovalStateDto _state = new()
    {
        TxtContactLastNameText = string.Empty,
        TxtContactNameText = string.Empty,
        TxtCompanyNameText = string.Empty
    };

    // --- Filter fields ---
    private string _txtOrderID = string.Empty;
    private string _txtProductID = string.Empty;
    private string _txtCompanyName = string.Empty;
    private string _txtContactName = string.Empty;
    private string _txtContactLastName = string.Empty;
    private bool _chkFrom;
    private bool _chkTo;
    private DateTime? _dtFrom = DateTime.Today.AddMonths(-1);
    private DateTime? _dtTo = DateTime.Today;
    private string _cmbStatusValue = "All";

    // --- Grid data ---
    private List<OrderRequestGridRow> _gridRows = new();
    private OrderRequestGridRow? _selectedRow;

    // --- UI state ---
    private bool _isLoading;
    private string _statusMessage = string.Empty;

    /// <summary>Port of Form_Load → InitGrid, then load fgOrders once on page init with default dates.</summary>
    protected override async Task OnInitializedAsync()
    {
        // InitGrid equivalent — grid columns defined in markup
        // Load initial data per user answer: "Load fgOrders once on page init with default dates"
        await DoSearchRequestAsync();
    }

    // ──────────────── cmbStatus_SelectedIndexChanged ────────────────
    /// <summary>Port of cmbStatus_SelectedIndexChanged → DoSearchRequest().</summary>
    private async Task OnCmbStatusSelectedIndexChanged()
    {
        await DoSearchRequestAsync();
    }

    // ──────────────── cmdApprove_Click ────────────────
    /// <summary>Port of cmdApprove_Click → LoadActionOrderRequest(1).</summary>
    private async Task OnCmdApproveClick()
    {
        await LoadActionOrderRequestAsync(1);
    }

    // ──────────────── cmdCancel_Click ────────────────
    /// <summary>Port of cmdCancel_Click → LoadActionOrderRequest(2).</summary>
    private async Task OnCmdCancelClick()
    {
        await LoadActionOrderRequestAsync(2);
    }

    // ──────────────── cmdInfo_Click ────────────────
    /// <summary>Port of cmdInfo_Click → LoadActionOrderRequest(0).</summary>
    private async Task OnCmdInfoClick()
    {
        await LoadActionOrderRequestAsync(0);
    }

    // ──────────────── LoadActionOrderRequest ────────────────
    /// <summary>
    /// Port of LoadActionOrderRequest(int Action).
    /// Legacy: gets OrderId from selected grid row column 1, opens frmActionOrderRequest dialog.
    /// </summary>
    private async Task LoadActionOrderRequestAsync(int action)
    {
        if (_selectedRow is null)
        {
            Snackbar.Add("Please select an order from the grid.", Severity.Warning);
            return;
        }

        int orderId = _selectedRow.OrderID;
        if (orderId <= 0)
        {
            Snackbar.Add("Invalid order selected.", Severity.Warning);
            return;
        }

        var parameters = new DialogParameters<ActionOrderRequest>
        {
            { x => x.OrderId, orderId },
            { x => x.ActionCode, action }
        };

        var options = new DialogOptions
        {
            MaxWidth = MaxWidth.Large,
            FullWidth = true,
            CloseOnEscapeKey = true,
            CloseButton = true
        };

        var dialog = await DialogService.ShowAsync<ActionOrderRequest>(
            "Action Order Request", parameters, options);
        var result = await dialog.Result;

        // After dialog closes, refresh the grid to reflect any status changes
        await DoSearchRequestAsync();
    }

    // ──────────────── dtFrom_ValueChanged ────────────────
    /// <summary>Port of dtFrom_ValueChanged: set chkFrom checked, then DoSearchRequest.</summary>
    private async Task OnDtFromValueChanged()
    {
        _chkFrom = true;
        await DoSearchRequestAsync();
    }

    // ──────────────── dtTo_ValueChanged ────────────────
    /// <summary>Port of dtTo_ValueChanged: set chkTo checked, then DoSearchRequest.</summary>
    private async Task OnDtToValueChanged()
    {
        _chkTo = true;
        await DoSearchRequestAsync();
    }

    // ──────────────── fgOrders_DoubleClick ────────────────
    /// <summary>Port of fgOrders_DoubleClick → cmdInfo_Click.</summary>
    private async Task OnGridRowClick(DataGridRowClickEventArgs<OrderRequestGridRow> args)
    {
        _selectedRow = args.Item;

        // Double-click opens info (legacy: fgOrders_DoubleClick => cmdInfo_Click)
        if (args.MouseEventArgs.Detail >= 2)
        {
            await OnCmdInfoClick();
        }
    }

    // ──────────────── txtOrderID_TextChanged ────────────────
    private async Task OnTxtOrderIDChanged()
    {
        await DoSearchRequestAsync();
    }

    // ──────────────── txtProductID_TextChanged ────────────────
    private async Task OnTxtProductIDChanged()
    {
        await DoSearchRequestAsync();
    }

    // ──────────────── txtCompanyName_TextChanged ────────────────
    private async Task OnTxtCompanyNameChanged()
    {
        await DoSearchRequestAsync();
    }

    // ──────────────── txtContactLastName_TextChanged ────────────────
    private async Task OnTxtContactLastNameChanged()
    {
        await DoSearchRequestAsync();
    }

    // ──────────────── txtContactName_TextChanged ────────────────
    private async Task OnTxtContactNameChanged()
    {
        await DoSearchRequestAsync();
    }

    // ──────────────── cmdClose_Click ────────────────
    /// <summary>Port of cmdClose_Click → this.Close(). Navigates back.</summary>
    private void OnCmdCloseClick()
    {
        NavigationManager.NavigateTo("/");
    }

    // ──────────────── cmdCustomers_Click ────────────────
    /// <summary>
    /// Port of cmdCustomers_Click.
    /// Legacy: opens frmCustomers dialog, gets CurrentCustomerID, clears filter fields,
    /// calls DoSearchRequest with customer ID.
    /// </summary>
    private async Task OnCmdCustomersClick()
    {
        var options = new DialogOptions
        {
            MaxWidth = MaxWidth.Large,
            FullWidth = true,
            CloseOnEscapeKey = true,
            CloseButton = true
        };

        var dialog = await DialogService.ShowAsync<SKS_Blazor.Pages.Customers.Customers>(
            "Customers", options);
        var result = await dialog.Result;

        _txtCompanyName = string.Empty;
        _txtContactLastName = string.Empty;
        _txtContactName = string.Empty;

        if (result is not null && !result.Canceled && result.Data is string customerIdStr)
        {
            if (decimal.TryParse(customerIdStr, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal parsed))
            {
                int customerId = Convert.ToInt32(parsed);
                await DoSearchRequestAsync(customerId);
                return;
            }
        }

        // Dialog closed without valid selection — still refresh
        await DoSearchRequestAsync();
    }

    // ──────────────── DoSearchRequest ────────────────
    /// <summary>
    /// Port of DoSearchRequest(int Id = -1). The service applies every legacy filter,
    /// joins OrderRequests, OrderRequestDetails, Customers and Users, and sums LineTotal;
    /// the page only maps rows to the grid.
    /// </summary>
    private async Task DoSearchRequestAsync(int customerId = -1)
    {
        _isLoading = true;

        try
        {
            _state.TxtCompanyNameText = _txtCompanyName;
            _state.TxtContactNameText = _txtContactName;
            _state.TxtContactLastNameText = _txtContactLastName;

            var filter = new RequestApprovalFilterDto
            {
                CompanyNameText = _txtCompanyName,
                ContactNameText = _txtContactName,
                ContactLastNameText = _txtContactLastName,
                OrderIDText = _txtOrderID,
                ProductIDText = _txtProductID,
                StatusFilter = _cmbStatusValue,
                FromDateEnabled = _chkFrom,
                FromDate = _dtFrom,
                ToDateEnabled = _chkTo,
                ToDate = _dtTo
            };

            var result = await RequestApprovalService.DoSearchRequestAsync(filter, customerId);

            _gridRows = result.Rows.Select(r => new OrderRequestGridRow
            {
                OrderDate = r.OrderDate,
                OrderID = r.OrderID,
                CompanyName = r.CompanyName,
                ContactName = r.ContactName,
                ReceivedBy = r.ReceivedBy,
                Price = r.Price,
                Status = r.Status
            }).ToList();
            _statusMessage = result.StatusMessage;
            AppState.LogStatus(_statusMessage);
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error searching requests: {ex.Message}", Severity.Error);
            _statusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            _isLoading = false;
        }
    }

    // ──────────────── Form_Closed ────────────────
    // Legacy: empty method. Cleanup handled by Dispose.
    public void Dispose()
    {
        // Port of Form_Closed — legacy was empty, nothing to clean up.
    }
}

/// <summary>Grid row DTO for the fgOrders MudDataGrid. Matches legacy columns:
/// Date, Order, Customer, Contact, Received by, Price, Status.</summary>
public class OrderRequestGridRow
{
    public string OrderDate { get; set; } = string.Empty;
    public int OrderID { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string ContactName { get; set; } = string.Empty;
    public string ReceivedBy { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Status { get; set; } = string.Empty;
}
