using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using MudBlazor;
using SKS_Blazor.Application.DTOs.Action;
using SKS_Blazor.Application.DTOs.Order;
using SKS_Blazor.Application.Services.Action;
using SKS_Blazor.Application.Services.Order;
using SKS_Blazor.Infrastructure;
using SKS_Blazor.Infrastructure.Data;
using SKS_Blazor.Domain.Entities.Order;
using SKS_Blazor.Domain.Entities.Core;
using SKS_Blazor.Domain.Entities.Stock;

namespace SKS_Blazor.Pages.Action;

/// <summary>Port of <c>frmActionOrderReception</c>.</summary>
public partial class ActionOrderReception : ComponentBase, IDisposable
{
    [Inject] private IActionOrderReceptionService ActionOrderReceptionService { get; set; } = default!;
    [Inject] private IOrderReceptionService OrderReceptionService { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private IDialogService DialogService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private AppState AppState { get; set; } = default!;

    /// <summary>The order ID to display/act upon — passed via query string.</summary>
    [SupplyParameterFromQuery(Name = "orderId")]
    public int OrderId { get; set; }

    /// <summary>Action to auto-execute on load: 0=none, 1=approve, 2=cancel. Port of <c>frmActionOrderReception.Action</c>.</summary>
    [SupplyParameterFromQuery(Name = "action")]
    public int ActionCode { get; set; }

    // ── Shared state ──
    private ActionOrderReceptionStateDto _state = new();

    // ── Details grid data ──
    private List<ActionOrderReceptionDetailDto> _details = new();

    // ── UI fields populated by LoadData ──
    private string _txtOrderID = string.Empty;
    private string _txtReceived = string.Empty;
    private string _txtReceivedBy = string.Empty;
    private string _txtProviderCompany = string.Empty;
    private string _txtProviderContact = string.Empty;
    private string _txtFreightCharge = string.Empty;
    private string _txtSalesTax = string.Empty;
    private string _txtNotes = string.Empty;
    private string _txtChanged = string.Empty;
    private string _txtChangedBy = string.Empty;

    // ── Visibility / label state ──
    private bool _showChangedFields;
    private string _lblChangedText = "Changed Date:";
    private string _lblChangedByText = "Changed By:";

    // ── Local totals (mirrors legacy fields) ──
    private decimal _currentSubTotal;
    private decimal _currentTotal;
    private decimal _currentTax;
    private decimal _currentFreightCharge;
    private decimal _currentTotalTax;

    // ── Loading / status ──
    private bool _isLoading;
    private string _statusMessage = string.Empty;
    private Severity _statusSeverity = Severity.Info;

    // ── Track whether we've loaded for the current OrderId ──
    private int _lastLoadedOrderId = -1;

    // ─────────────────────── Lifecycle ───────────────────────

    /// <summary>Port of <c>frmActionOrderReception_Activated</c>. Initialize state once.</summary>
    protected override Task OnInitializedAsync()
    {
        _state = new ActionOrderReceptionStateDto
        {
            CurrentSubTotal = 0m,
            CurrentTotal = 0m,
            CurrentTax = 0m,
            CurrentFreightCharge = 0m,
            CurrentTotalTax = 0m,
            TxtStatusText = string.Empty,
            TxtSubTotalText = "0.00",
            TxtTotalText = "0.00",
            TxtTotalTaxText = "0.00"
        };

        return Task.CompletedTask;
    }

    /// <summary>Port of <c>Form_Load</c>. React to query parameter changes.</summary>
    protected override async Task OnParametersSetAsync()
    {
        if (OrderId <= 0)
        {
            if (_lastLoadedOrderId != -1)
            {
                LogStatus("No order ID specified.", Severity.Warning);
            }
            return;
        }

        if (OrderId == _lastLoadedOrderId && _lastLoadedOrderId != -1)
        {
            return;
        }

        _lastLoadedOrderId = OrderId;

        await LoadDataAsync();

        // Port of Form_Load: auto-execute action if provided
        if (ActionCode != 0)
        {
            switch (ActionCode)
            {
                case 1:
                    await OnApproveClickAsync();
                    break;
                case 2:
                    await OnCancelClickAsync();
                    break;
            }
        }
    }

    /// <summary>Port of <c>Form_Closed</c> — cleanup.</summary>
    public void Dispose()
    {
        // Legacy Form_Closed was empty; nothing to dispose.
    }

    // ─────────────────────── Data Loading ───────────────────────

    /// <summary>
    /// Port of <c>LoadData</c>. Queries via service for the joined header
    /// (OrderReceptions + Users + Providers) and detail lines, populates UI fields.
    /// </summary>
    private async Task LoadDataAsync()
    {
        _isLoading = true;

        try
        {
            // Reset subtotal accumulators
            _currentSubTotal = 0m;
            _currentTotalTax = 0m;

            await LoadHeaderAsync();
            await LoadDetailsAsync();
            DisplayTotals();
        }
        catch (Exception ex)
        {
            LogStatus($"An error has occurred loading data. Error: {ex.Message}", Severity.Error);
        }
        finally
        {
            _isLoading = false;
        }
    }

    /// <summary>
    /// Loads the order reception header fields via service.
    /// </summary>
    private async Task LoadHeaderAsync()
    {
        var header = await ActionOrderReceptionService.GetOrderReceptionHeaderAsync(OrderId);

        if (header is null)
        {
            LogStatus($"The order with the ID '{OrderId}' does not exist", Severity.Warning);
            return;
        }

        _txtOrderID = OrderId.ToString();
        _txtReceived = header.OrderDate ?? string.Empty;
        _txtReceivedBy = header.ReceivedByFullName ?? string.Empty;
        _txtNotes = header.Notes ?? string.Empty;

        _currentFreightCharge = header.FreightCharge ?? 0m;
        _txtFreightCharge = _currentFreightCharge.ToString("N2");

        _currentTax = header.SalesTaxRate ?? 0m;
        _txtSalesTax = _currentTax.ToString("N2");

        _txtProviderCompany = header.ProviderName ?? string.Empty;
        _txtProviderContact = header.ProviderContact ?? string.Empty;

        var status = header.Status ?? string.Empty;
        _state.TxtStatusText = status;

        _txtChanged = header.ChangedDate ?? string.Empty;
        _txtChangedBy = header.ChangedBy ?? string.Empty;

        bool isReceived = string.Equals(status, "RECEIVED", StringComparison.OrdinalIgnoreCase);
        _showChangedFields = !isReceived;

        if (string.Equals(status, "APPROVED", StringComparison.OrdinalIgnoreCase))
        {
            _lblChangedText = "Approved Date:";
            _lblChangedByText = "Approved By:";
        }
        else
        {
            _lblChangedText = "Cancelled Date:";
            _lblChangedByText = "Cancelled By:";
        }

        _state.CurrentFreightCharge = _currentFreightCharge;
        _state.CurrentTax = _currentTax;
    }

    /// <summary>
    /// Port of <c>LoadDetails</c>. Loads detail lines via service and iterates
    /// them to accumulate totals (PARAMETER ITERATION — rule #40).
    /// </summary>
    private async Task LoadDetailsAsync()
    {
        var rows = await ActionOrderReceptionService.GetOrderReceptionDetailsAsync(OrderId);

        var detailDisplayList = new List<ActionOrderReceptionDetailDto>();

        // PARAMETER ITERATION: iterate over each detail line, accumulate totals
        foreach (var row in rows)
        {
            var detailDto = new ActionOrderReceptionDetailDto
            {
                Quantity = row.Quantity ?? 0m,
                ProductID = row.ProductID ?? string.Empty,
                ProductName = row.ProductName ?? string.Empty,
                UnitPrice = row.UnitPrice ?? 0m,
                SalePrice = row.SalePrice ?? 0m,
                UnitsInStock = row.UnitsInStock ?? 0,
                UnitsOnOrder = row.UnitsOnOrder ?? 0,
                QuantityPerUnitDisplay = row.QuantityPerUnitDisplay ?? string.Empty,
                LineTotal = row.LineTotal ?? 0m
            };

            // Accumulate totals — port of AddToTotals
            AddToTotals(row.LineTotal ?? 0m);

            detailDisplayList.Add(detailDto);
        }

        _details = detailDisplayList;
    }

    // ─────────────────────── Totals Calculation ───────────────────────

    /// <summary>Port of <c>AddToTotals(double current)</c>. Accumulates running sub-total, tax, and total.</summary>
    private void AddToTotals(decimal current)
    {
        _currentSubTotal += current;
        _currentTotalTax = _currentSubTotal * _currentTax;
        _currentTotal = _currentFreightCharge + _currentSubTotal + _currentTotalTax;

        _state.CurrentSubTotal = _currentSubTotal;
        _state.CurrentTotalTax = _currentTotalTax;
        _state.CurrentTotal = _currentTotal;
        _state.TxtSubTotalText = _currentSubTotal.ToString("#,##0.00");
        _state.TxtTotalTaxText = _currentTotalTax.ToString("#,##0.00");
        _state.TxtTotalText = _currentTotal.ToString("#,##0.00");
    }

    /// <summary>Port of <c>DisplayTotals</c>. Recomputes final total and updates state text fields.</summary>
    private void DisplayTotals()
    {
        _currentTotal = _currentFreightCharge + _currentSubTotal + _currentTotalTax;

        _state.CurrentTotal = _currentTotal;
        _state.TxtSubTotalText = _currentSubTotal.ToString("#,##0.00");
        _state.TxtTotalTaxText = _currentTotalTax.ToString("#,##0.00");
        _state.TxtTotalText = _currentTotal.ToString("#,##0.00");
    }

    // ─────────────────────── Button Handlers ───────────────────────

    /// <summary>
    /// Port of <c>cmdApprove_Click</c>.
    /// Validates status, calls service to approve order.
    /// Uses EF Core transaction (TRANSACTION BOUNDARIES — rule #41).
    /// </summary>
    private async Task OnApproveClickAsync()
    {
        // PRE-SAVE VALIDATION: check current status before attempting approval
        var currentStatus = (_state.TxtStatusText ?? string.Empty).ToUpperInvariant();

        if (currentStatus == "APPROVED")
        {
            LogStatus("Order is already approved, not need to be approved again", Severity.Warning);
            return;
        }

        if (currentStatus == "CANCELLED")
        {
            LogStatus("Order was already cancelled, it cannot be approved", Severity.Warning);
            return;
        }

        _isLoading = true;

        try
        {
            var userId = AppState.CurrentUser ?? "system";
            await ActionOrderReceptionService.ApproveOrderReceptionAsync(OrderId, userId);

            // Reload data after approval to reflect updated status
            await LoadDataAsync();

            Snackbar.Add("The order was successfully approved", Severity.Success);

            // Legacy closes the form after approval; navigate back
            NavigationManager.NavigateTo("/upgraded/reception-approval");
        }
        catch (DbUpdateConcurrencyException)
        {
            // LOCK RETRY: retry once on concurrency conflict
            try
            {
                var userId = AppState.CurrentUser ?? "system";
                await ActionOrderReceptionService.ApproveOrderReceptionAsync(OrderId, userId);

                await LoadDataAsync();
                Snackbar.Add("The order was successfully approved (after retry)", Severity.Success);
                NavigationManager.NavigateTo("/upgraded/reception-approval");
            }
            // Blazor signals a redirect by throwing NavigationException; catching
            // everything turns that redirect into an error and the page stays put.
            catch (Exception retryEx) when (retryEx is not NavigationException)
            {
                LogStatus($"An error has occurred adding the data. Error: {retryEx.Message}", Severity.Error);
            }
        }
        catch (Exception ex)
        {
            LogStatus($"An error has occurred adding the data. Error: {ex.Message}", Severity.Error);
        }
        finally
        {
            _isLoading = false;
        }
    }

    /// <summary>
    /// Port of <c>cmdCancel_Click</c>.
    /// Validates status, confirms with user, calls service to cancel order.
    /// Uses EF Core transaction (TRANSACTION BOUNDARIES — rule #41).
    /// </summary>
    private async Task OnCancelClickAsync()
    {
        // PRE-SAVE VALIDATION: check current status before attempting cancellation
        var currentStatus = (_state.TxtStatusText ?? string.Empty).ToUpperInvariant();

        if (currentStatus == "CANCELLED")
        {
            LogStatus("Order was already cancelled, not need to be cancelled again", Severity.Warning);
            return;
        }

        if (currentStatus == "APPROVED")
        {
            LogStatus("Order was already approved, it cannot be cancelled", Severity.Warning);
            return;
        }

        // Confirmation dialog — port of MessageBox.Show("Do you want to cancel the order reception?")
        var confirmed = await DialogService.ShowMessageBox(
            "Confirm cancellation",
            "Do you want to cancel the order reception?",
            yesText: "Yes",
            noText: "No");

        if (confirmed != true)
        {
            return;
        }

        _isLoading = true;

        try
        {
            var userId = AppState.CurrentUser ?? "system";
            await ActionOrderReceptionService.CancelOrderReceptionAsync(OrderId, userId);

            await LoadDataAsync();

            Snackbar.Add("The order was successfully cancelled", Severity.Success);

            // Legacy closes the form after cancellation; navigate back
            NavigationManager.NavigateTo("/upgraded/reception-approval");
        }
        catch (DbUpdateConcurrencyException)
        {
            // LOCK RETRY
            try
            {
                var userId = AppState.CurrentUser ?? "system";
                await ActionOrderReceptionService.CancelOrderReceptionAsync(OrderId, userId);

                await LoadDataAsync();
                Snackbar.Add("The order was successfully cancelled (after retry)", Severity.Success);
                NavigationManager.NavigateTo("/upgraded/reception-approval");
            }
            // Blazor signals a redirect by throwing NavigationException; catching
            // everything turns that redirect into an error and the page stays put.
            catch (Exception retryEx) when (retryEx is not NavigationException)
            {
                LogStatus($"An error has occurred adding the data. Error: {retryEx.Message}", Severity.Error);
            }
        }
        catch (Exception ex)
        {
            LogStatus($"An error has occurred adding the data. Error: {ex.Message}", Severity.Error);
        }
        finally
        {
            _isLoading = false;
        }
    }

    /// <summary>Port of <c>cmdClose_Click</c>. Navigates back to reception approval list.</summary>
    private void OnCloseClick()
    {
        NavigationManager.NavigateTo("/upgraded/reception-approval");
    }

    // ─────────────────────── Helpers ───────────────────────

    /// <summary>Port of <c>modMain.LogStatus</c> for this page's status bar.</summary>
    private void LogStatus(string message, Severity severity)
    {
        _statusMessage = message;
        _statusSeverity = severity;
    }
}

