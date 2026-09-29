using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using MudBlazor;
using SKS_Blazor.Application.DTOs.Action;
using SKS_Blazor.Application.DTOs.Order;
using SKS_Blazor.Application.Services.Action;
using SKS_Blazor.Application.Services.Order;
using SKS_Blazor.Infrastructure;
using SKS_Blazor.Domain.Entities.Order;
using SKS_Blazor.Domain.Entities.Core;

namespace SKS_Blazor.Pages.Action;

/// <summary>Port of <c>frmActionOrderRequest</c> (legacy form frmActionOrderRequest.cs).</summary>
public partial class ActionOrderRequest : ComponentBase, IDisposable
{
    [Inject] private IActionOrderRequestService ActionOrderRequestService { get; set; } = default!;
    [Inject] private IOrderReceptionService OrderReceptionService { get; set; } = default!;
    [Inject] private IDialogService DialogService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private AppState AppState { get; set; } = default!;
    [Inject] private ILogger<ActionOrderRequest> Logger { get; set; } = default!;

    /// <summary>The order ID to display/action upon. Passed as query parameter.</summary>
    [Parameter]
    [SupplyParameterFromQuery(Name = "orderId")]
    public int OrderId { get; set; }

    /// <summary>Action code: 0 = view only, 1 = auto-approve on load, 2 = auto-cancel on load.</summary>
    [Parameter]
    [SupplyParameterFromQuery(Name = "action")]
    public int ActionCode { get; set; }

    // --- Shared state (single instance for page lifetime) ---
    private ActionOrderRequestStateDto? _state;

    // --- Detail grid data ---
    private List<OrderRequestDetailLineDto> _detailLines = new();

    // --- UI fields populated from LoadData ---
    private string _txtOrderID = string.Empty;
    private string _txtReceived = string.Empty;
    private string _txtReceivedBy = string.Empty;
    private string _txtNotes = string.Empty;
    private string _txtFreightCharge = string.Empty;
    private string _txtSalesTax = string.Empty;
    private string _txtCustomerCompany = string.Empty;
    private string _txtCustomerContact = string.Empty;
    private string _txtStatus = string.Empty;
    private string _txtRequiredBy = string.Empty;
    private string _txtPromisedBy = string.Empty;
    private string _txtChanged = string.Empty;
    private string _txtChangedBy = string.Empty;

    // --- Visibility / label state ---
    private bool _showChangedFields;
    private string _lblChangedText = "Changed Date:";
    private string _lblChangedByText = "Changed By:";
    private bool _cmdApproveEnabled;
    private bool _cmdCancelEnabled;

    // --- Totals (mirrors legacy currentSubTotal, currentTotal, currentTax, currentFreightCharge, currentTotalTax) ---
    private decimal _currentSubTotal;
    private decimal _currentTotal;
    private decimal _currentTax;
    private decimal _currentFreightCharge;
    private decimal _currentTotalTax;

    // --- Loading / status ---
    private bool _isLoading;
    private string _statusMessage = string.Empty;
    private string _statusBarText = string.Empty;

    // --- Query parameter tracking to prevent re-loads on render-only passes ---
    private int _lastLoadedOrderId = -1;
    private int _lastLoadedActionCode = -999;

    /// <summary>Port of Form_Load — initializes state.</summary>
    protected override async Task OnInitializedAsync()
    {
        _state = new ActionOrderRequestStateDto();
        await Task.CompletedTask;
    }

    /// <summary>Port of query parameter handling — loads data when query string changes.
    /// Blazor reuses component instances when only query params change, so we must use OnParametersSetAsync
    /// instead of OnInitializedAsync to detect and reload on parameter changes.</summary>
    protected override async Task OnParametersSetAsync()
    {
        if (OrderId != _lastLoadedOrderId || ActionCode != _lastLoadedActionCode)
        {
            _lastLoadedOrderId = OrderId;
            _lastLoadedActionCode = ActionCode;

            await LoadDataAsync();

            // Port of Form_Load: auto-action based on Action parameter
            if (ActionCode == 1)
            {
                await OnApproveClickAsync();
            }
            else if (ActionCode == 2)
            {
                await OnCancelClickAsync();
            }
        }
    }

    /// <summary>Port of <c>LoadData</c> — loads header, details, and computes totals.
    /// Legacy SQL: Select o.OrderDate, u.Fullname, o.Status, c.CompanyName, 
    /// c.ContactFirstName + ' ' + c.ContactLastName as Contact, o.ChangedDate, o.ChangedBy,
    /// o.FreightCharge, o.SalesTaxRate, o.RequiredByDate, o.PromisedByDate, o.Notes
    /// From OrderRequests as o, Users as u, Customers as c
    /// Where o.OrderID = {OrderId} And u.Username = o.EmployeeId And c.CustomerId = o.CustomerId</summary>
    private async Task LoadDataAsync()
    {
        if (OrderId <= 0)
        {
            LogStatus("No Order ID specified.");
            return;
        }

        _isLoading = true;
        try
        {
            // Reset totals (mirrors legacy: currentSubTotal = 0; currentTotalTax = 0)
            _currentSubTotal = 0;
            _currentTotalTax = 0;

            _state!.CurrentSubTotal = 0;
            _state.CurrentTotalTax = 0;
            _state.OrderID = OrderId;

            // Load the order request header via service
            _state = await ActionOrderRequestService.LoadDataAsync(_state);

            // Get all order requests to find this one
            var orderRequests = await OrderReceptionService.GetOrderRequestsAsync();
            var order = orderRequests.FirstOrDefault(o => o.OrderID == OrderId);

            if (order == null)
            {
                LogStatus($"The order with the ID '{OrderId}' does not exist");
                return;
            }

            // Map order header fields to UI (port of legacy recordset field mapping)
            _txtOrderID = OrderId.ToString();
            _txtReceived = order.OrderDate ?? string.Empty;
            _txtReceivedBy = order.EmployeeID ?? string.Empty;
            _txtNotes = order.Notes ?? string.Empty;
            _txtFreightCharge = (order.FreightCharge ?? 0m).ToString("N2");
            _currentFreightCharge = order.FreightCharge ?? 0m;
            _txtSalesTax = (order.SalesTaxRate ?? 0m).ToString("N2");
            _currentTax = order.SalesTaxRate ?? 0m;
            _txtCustomerCompany = order.ShipName ?? string.Empty;
            _txtCustomerContact = string.Empty;
            _txtStatus = order.Status ?? string.Empty;
            _txtRequiredBy = order.RequiredByDate ?? string.Empty;
            _txtPromisedBy = order.PromisedByDate ?? string.Empty;
            _txtChanged = string.Empty;
            _txtChangedBy = string.Empty;

            if (!string.IsNullOrEmpty(order.ChangedDate))
            {
                _txtChanged = order.ChangedDate;
            }
            if (!string.IsNullOrEmpty(order.ChangedBy))
            {
                _txtChangedBy = order.ChangedBy;
            }

            // Visibility logic: if status is REQUESTED, hide changed fields
            bool isRequested = _txtStatus.Equals("REQUESTED", StringComparison.OrdinalIgnoreCase);
            _showChangedFields = !isRequested;
            _cmdApproveEnabled = true;
            _cmdCancelEnabled = true;

            if (_txtStatus.Equals("APPROVED", StringComparison.OrdinalIgnoreCase))
            {
                _lblChangedText = "Approved Date:";
                _lblChangedByText = "Approved By:";
            }
            else
            {
                _lblChangedText = "Cancelled Date:";
                _lblChangedByText = "Cancelled By:";
            }

            // Load details and compute totals
            await LoadDetailsAsync();
            DisplayTotals();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading order request data for OrderId={OrderId}", OrderId);
            Snackbar.Add($"An error has occurred loading the data. Error: {ex.Message}", Severity.Error);
        }
        finally
        {
            _isLoading = false;
        }
    }

    /// <summary>Port of <c>LoadDetails</c> — queries OrderRequestDetails joined with Products, populates grid and accumulates totals.
    /// Legacy SQL: Select d.Quantity, p.ProductID, p.ProductName, d.UnitPrice, d.SalePrice, p.UnitsInStock, p.UnitsOnOrder,
    /// cast(p.QuantityPerUnit as text) + p.Unit, d.LineTotal From Products as p, OrderRequestDetails as d
    /// Where d.OrderID = {OrderId} And d.ProductId = p.ProductId
    /// PARAMETERITERATION: legacy iterates over rs with MoveNext(), accumulates line totals.</summary>
    private async Task LoadDetailsAsync()
    {
        _detailLines.Clear();
        _currentSubTotal = 0;
        _currentTotalTax = 0;

        // Get all order request details
        var allDetails = await OrderReceptionService.GetOrderRequestDetailsAsync();
        var orderDetails = allDetails.Where(d => d.OrderID == OrderId).ToList();

        if (orderDetails.Count == 0)
        {
            return;
        }

        // PARAMETERITERATION: iterate over each detail row, build grid line, accumulate totals
        foreach (var detail in orderDetails)
        {
            var line = new OrderRequestDetailLineDto
            {
                Quantity = detail.Quantity ?? 0m,
                ProductID = detail.ProductID ?? string.Empty,
                ProductName = string.Empty,
                UnitPrice = detail.UnitPrice ?? 0m,
                SalePrice = detail.SalePrice ?? 0m,
                UnitsInStock = 0,
                UnitsOnOrder = 0,
                QuantityPerUnitDisplay = string.Empty,
                LineTotal = detail.LineTotal ?? 0m
            };

            _detailLines.Add(line);

            // Port of AddToTotals — accumulate per-line
            AddToTotals(detail.LineTotal ?? 0m);
        }

        // Update state with computed totals
        if (_state != null)
        {
            _state.CurrentSubTotal = _currentSubTotal;
            _state.CurrentTotalTax = _currentTotalTax;
            _state.CurrentTotal = _currentTotal;
            _state.CurrentFreightCharge = _currentFreightCharge;
            _state.CurrentTax = _currentTax;
        }
    }

    /// <summary>Port of <c>DisplayTotals</c> — computes and formats total fields.</summary>
    private void DisplayTotals()
    {
        _currentTotal = _currentFreightCharge + _currentSubTotal + _currentTotalTax;

        if (_state != null)
        {
            _state.TxtSubTotalText = _currentSubTotal.ToString("#,##0.00");
            _state.TxtTotalTaxText = _currentTotalTax.ToString("#,##0.00");
            _state.TxtTotalText = _currentTotal.ToString("#,##0.00");
            _state.CurrentSubTotal = _currentSubTotal;
            _state.CurrentTotal = _currentTotal;
            _state.CurrentTotalTax = _currentTotalTax;
        }
    }

    /// <summary>Port of <c>AddToTotals</c> — accumulates subtotal and recomputes tax/total.
    /// Legacy: currentSubTotal += current; currentTotalTax = currentSubTotal * currentTax;
    /// currentTotal = currentFreightCharge + currentSubTotal + currentTotalTax;</summary>
    private void AddToTotals(decimal current)
    {
        _currentSubTotal += current;
        _currentTotalTax = _currentSubTotal * _currentTax;
        _currentTotal = _currentFreightCharge + _currentSubTotal + _currentTotalTax;

        if (_state != null)
        {
            _state.TxtSubTotalText = _currentSubTotal.ToString("#,##0.00");
            _state.TxtTotalTaxText = _currentTotalTax.ToString("#,##0.00");
            _state.TxtTotalText = _currentTotal.ToString("#,##0.00");
        }
    }

    /// <summary>Port of <c>cmdApprove_Click</c> — validates status, calls service ApproveAsync, reloads data.
    /// PRESAVEVALIDATION: checks if already APPROVED or CANCELLED before updating.
    /// LOCKRETRY: catches DbUpdateConcurrencyException and retries once.</summary>
    private async Task OnApproveClickAsync()
    {
        _isLoading = true;
        try
        {
            // PRESAVEVALIDATION: check if already approved
            if (_txtStatus.Equals("APPROVED", StringComparison.OrdinalIgnoreCase))
            {
                LogStatus("Order is already approved, not need to be approved again");
                return;
            }

            // PRESAVEVALIDATION: check if cancelled
            if (_txtStatus.Equals("CANCELLED", StringComparison.OrdinalIgnoreCase))
            {
                LogStatus($"Order was already cancelled by {_txtChangedBy} on {_txtChanged}, it cannot be approved");
                return;
            }

            if (_state != null)
            {
                _state.TxtStatusText = _txtStatus;
            }

            // Call service — delegates to EF Core update of OrderRequests table
            await ActionOrderRequestService.ApproveAsync(_state!, AppState.UserId);

            // Reload data after approve
            await LoadDataAsync();

            Snackbar.Add("The order was successfully approved", Severity.Success);

            // Legacy closes the form after approval — navigate back
            NavigationManager.NavigateTo("/upgraded/request-approval");
        }
        catch (DbUpdateConcurrencyException ex)
        {
            // LOCKRETRY: retry once on concurrency exception
            Logger.LogWarning(ex, "Concurrency exception approving order {OrderId}, retrying", OrderId);
            try
            {
                await ActionOrderRequestService.ApproveAsync(_state!, AppState.UserId);
                await LoadDataAsync();
                Snackbar.Add("The order was successfully approved", Severity.Success);
                NavigationManager.NavigateTo("/upgraded/request-approval");
            }
            // Blazor signals a redirect by throwing NavigationException; catching
            // everything turns that redirect into an error and the page stays put.
            catch (Exception retryEx) when (retryEx is not NavigationException)
            {
                Logger.LogError(retryEx, "Retry failed for approve order {OrderId}", OrderId);
                Snackbar.Add($"An error has occurred adding the data. Error: {retryEx.Message}", Severity.Error);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error approving order request {OrderId}", OrderId);
            Snackbar.Add($"An error has occurred adding the data. Error: {ex.Message}", Severity.Error);
        }
        finally
        {
            _isLoading = false;
        }
    }

    /// <summary>Port of <c>cmdCancel_Click</c> — validates status, confirms with user, calls service CancelAsync.
    /// PRESAVEVALIDATION: checks if already CANCELLED or APPROVED before updating.
    /// Legacy: MessageBox.Show("Do you want to cancel the order request?", "Confirm cancellation", YesNo)
    /// LOCKRETRY: catches DbUpdateConcurrencyException and retries once.</summary>
    private async Task OnCancelClickAsync()
    {
        try
        {
            // PRESAVEVALIDATION: check if already cancelled
            if (_txtStatus.Equals("CANCELLED", StringComparison.OrdinalIgnoreCase))
            {
                LogStatus("Order was already cancelled, not need to be cancelled again");
                return;
            }

            // PRESAVEVALIDATION: check if already approved
            if (_txtStatus.Equals("APPROVED", StringComparison.OrdinalIgnoreCase))
            {
                LogStatus($"Order was already approved by {_txtChangedBy} on {_txtChanged}, it cannot be cancelled");
                return;
            }

            // Confirmation dialog — port of MessageBox.Show
            _isLoading = false;

            var confirmed = await DialogService.ShowMessageBox(
                "Confirm cancellation",
                "Do you want to cancel the order request?",
                yesText: "Yes",
                noText: "No");

            if (confirmed != true)
            {
                return;
            }

            _isLoading = true;

            if (_state != null)
            {
                _state.TxtStatusText = _txtStatus;
            }

            // Call service — delegates to EF Core update of OrderRequests table
            await ActionOrderRequestService.CancelAsync(_state!, AppState.UserId);

            // Reload data after cancel
            await LoadDataAsync();

            Snackbar.Add("The order was successfully cancelled", Severity.Success);

            // Legacy closes the form after cancellation — navigate back
            NavigationManager.NavigateTo("/upgraded/request-approval");
        }
        catch (DbUpdateConcurrencyException ex)
        {
            // LOCKRETRY: retry once on concurrency exception
            Logger.LogWarning(ex, "Concurrency exception cancelling order {OrderId}, retrying", OrderId);
            try
            {
                await ActionOrderRequestService.CancelAsync(_state!, AppState.UserId);
                await LoadDataAsync();
                Snackbar.Add("The order was successfully cancelled", Severity.Success);
                NavigationManager.NavigateTo("/upgraded/request-approval");
            }
            // Blazor signals a redirect by throwing NavigationException; catching
            // everything turns that redirect into an error and the page stays put.
            catch (Exception retryEx) when (retryEx is not NavigationException)
            {
                Logger.LogError(retryEx, "Retry failed for cancel order {OrderId}", OrderId);
                Snackbar.Add($"An error has occurred adding the data. Error: {retryEx.Message}", Severity.Error);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error cancelling order request {OrderId}", OrderId);
            Snackbar.Add($"An error has occurred adding the data. Error: {ex.Message}", Severity.Error);
        }
        finally
        {
            _isLoading = false;
        }
    }

    /// <summary>Port of <c>cmdClose_Click</c> — navigates back (legacy: this.Close()).</summary>
    private void OnCloseClick()
    {
        NavigationManager.NavigateTo("/upgraded/request-approval");
    }

    /// <summary>Port of <c>modMain.LogStatus</c> — displays status message on screen.</summary>
    private void LogStatus(string message)
    {
        _statusMessage = message;
        _statusBarText = message;
        AppState.LogStatus(message);
    }

    /// <summary>Port of <c>Form_Closed</c> — cleanup. Legacy was empty.</summary>
    public void Dispose()
    {
        // Port of Form_Closed — legacy was empty; no resources to release.
    }
}
