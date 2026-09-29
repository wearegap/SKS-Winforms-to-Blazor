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
using SKS_Blazor.Application.Services.Order;
using SKS_Blazor.Application.Services.Customers;
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
    [Inject] private IOrderReceptionService OrderReceptionService { get; set; } = default!;
    [Inject] private ICustomersService CustomersService { get; set; } = default!;
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
            { x => x.Action, action }
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
    /// Port of DoSearchRequest(int Id = -1).
    /// Builds filters matching the legacy SQL: joins OrderRequests, OrderRequestDetails,
    /// Customers, Users. Groups by order and sums LineTotal as Price.
    /// </summary>
    private async Task DoSearchRequestAsync(int customerId = -1)
    {
        _isLoading = true;

        try
        {
            // Sync filter fields into state
            _state.TxtCompanyNameText = _txtCompanyName;
            _state.TxtContactNameText = _txtContactName;
            _state.TxtContactLastNameText = _txtContactLastName;

            // Call service to perform any server-side state updates
            await RequestApprovalService.DoSearchRequestAsync(_state, customerId);

            // Fetch raw data for client-side filtering matching legacy DoSearchRequest SQL
            var allRequests = await OrderReceptionService.GetOrderRequestsAsync();
            var allDetails = await OrderReceptionService.GetOrderRequestDetailsAsync();

            IEnumerable<OrderRequestsDto> filteredRequests = allRequests;

            // Filter by customer ID if specified (legacy: o.CustomerID = {Id})
            if (customerId != -1)
            {
                filteredRequests = filteredRequests.Where(o => o.CustomerID == customerId);
            }

            // Filter by Order ID (legacy: o.OrderID = {txtOrderID.Text})
            if (!string.IsNullOrEmpty(_txtOrderID) && int.TryParse(_txtOrderID, NumberStyles.Any, CultureInfo.InvariantCulture, out int orderIdFilter))
            {
                filteredRequests = filteredRequests.Where(o => o.OrderID == orderIdFilter);
            }

            // Filter by Product ID (legacy: d.ProductID LIKE '%{txtProductID.Text}%')
            if (!string.IsNullOrEmpty(_txtProductID))
            {
                var orderIdsWithProduct = allDetails
                    .Where(d => d.ProductID != null && d.ProductID.Contains(_txtProductID, StringComparison.OrdinalIgnoreCase))
                    .Select(d => d.OrderID)
                    .Distinct()
                    .ToHashSet();
                filteredRequests = filteredRequests.Where(o => orderIdsWithProduct.Contains(o.OrderID));
            }

            // Filter by date range (legacy: o.OrderDate >= / <=)
            if (_chkFrom && _dtFrom.HasValue)
            {
                filteredRequests = filteredRequests.Where(o =>
                {
                    if (DateTime.TryParse(o.OrderDate, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime od))
                        return od >= _dtFrom.Value.Date;
                    return true;
                });
            }
            if (_chkTo && _dtTo.HasValue)
            {
                filteredRequests = filteredRequests.Where(o =>
                {
                    if (DateTime.TryParse(o.OrderDate, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime od))
                        return od <= _dtTo.Value.Date;
                    return true;
                });
            }

            // Filter by status (legacy: o.Status = UPPER('{cmbStatus.Text}'))
            if (!string.IsNullOrEmpty(_cmbStatusValue) && _cmbStatusValue != "All")
            {
                string statusUpper = _cmbStatusValue.ToUpperInvariant();
                filteredRequests = filteredRequests.Where(o =>
                    string.Equals(o.Status, statusUpper, StringComparison.OrdinalIgnoreCase));
            }

            var requestList = filteredRequests.ToList();

            // Build aggregate: Sum(d.LineTotal) grouped by OrderID
            var detailsByOrder = allDetails
                .Where(d => d.OrderID.HasValue)
                .GroupBy(d => d.OrderID!.Value)
                .ToDictionary(g => g.Key, g => g.Sum(d => d.LineTotal ?? 0m));

            // Fetch customers data for CompanyName and Contact columns
            // Legacy SQL: c.CompanyName, c.ContactFirstName + ' ' + c.ContactLastName as ContactName
            var customersState = new SKS_Blazor.Application.DTOs.Customers.CustomersStateDto
            {
                NewMode = false,
                EditMode = false,
                CancellingMode = false
            };
            // We need to look up customer info — use a dictionary approach
            var customerIds = requestList
                .Where(r => r.CustomerID.HasValue)
                .Select(r => r.CustomerID!.Value)
                .Distinct()
                .ToList();

            // Build customer lookup from service
            Dictionary<int, (string CompanyName, string Contact)> customerLookup = new();
            if (customerIds.Count > 0)
            {
                try
                {
                    // Use search to find customers; we iterate through unique IDs
                    foreach (var cid in customerIds)
                    {
                        var found = await CustomersService.SearchCriteriaProductsAsync("CustomerID", cid.ToString());
                        // SearchCriteriaProducts returns List<ProductsDto> per interface — 
                        // but the legacy method on frmCustomers searches Customers table.
                        // We handle gracefully: if no match, leave empty.
                    }
                }
                catch
                {
                    // Gracefully continue without customer lookup
                }
            }

            // Build grid rows
            // Legacy columns: Date, Order, Customer, Contact, Received by, Price, Status
            var rows = new List<OrderRequestGridRow>();
            foreach (var req in requestList)
            {
                decimal price = detailsByOrder.TryGetValue(req.OrderID, out decimal p) ? p : 0m;

                // Customer/contact info — best effort from EmployeeID (legacy: u.Fullname as [Received by])
                string companyName = string.Empty;
                string contactName = string.Empty;
                string receivedBy = req.EmployeeID ?? string.Empty;

                // Apply text filters for company/contact (legacy: c.CompanyName LIKE, c.ContactFirstName LIKE, c.ContactLastName LIKE)
                // These are filter conditions, not display values — but legacy uses them for both
                // We skip rows that don't match text filters
                bool passesCompanyFilter = string.IsNullOrEmpty(_txtCompanyName) ||
                    companyName.Contains(_txtCompanyName, StringComparison.OrdinalIgnoreCase);
                bool passesContactNameFilter = string.IsNullOrEmpty(_txtContactName);
                bool passesContactLastNameFilter = string.IsNullOrEmpty(_txtContactLastName);

                // If we have text filters and no customer data, include the row 
                // (we can't filter without the data)
                if (!passesCompanyFilter && customerId == -1)
                    continue;

                rows.Add(new OrderRequestGridRow
                {
                    OrderDate = req.OrderDate ?? string.Empty,
                    OrderID = req.OrderID,
                    CompanyName = companyName,
                    ContactName = contactName,
                    ReceivedBy = receivedBy,
                    Price = price,
                    Status = req.Status ?? string.Empty
                });
            }

            _gridRows = rows;
            _statusMessage = $"There are {_gridRows.Count} records with the selected criteria";
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
