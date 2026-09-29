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
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private IDialogService DialogService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private AppState AppState { get; set; } = default!;

    /// <summary>The order ID to display/act upon — passed via query string.</summary>
    [Parameter]
    [SupplyParameterFromQuery(Name = "orderId")]
    public int OrderId { get; set; }

    /// <summary>Action to auto-execute on load: 0=none, 1=approve, 2=cancel. Port of <c>frmActionOrderReception.Action</c>.</summary>
    [Parameter]
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
    /// Port of <c>LoadData</c> + <c>LoadDetails</c> + <c>DisplayTotals</c>. The service runs the
    /// joined header query (OrderReceptions + Users + Providers), the detail query and the totals;
    /// the page copies the result into its display fields.
    /// </summary>
    private async Task LoadDataAsync()
    {
        _isLoading = true;

        try
        {
            _state.OrderID = OrderId;
            _state = await ActionOrderReceptionService.LoadDataAsync(_state);

            if (!_state.OrderFound)
            {
                LogStatus(_state.ErrorMessage, Severity.Warning);
                return;
            }

            _txtOrderID = OrderId.ToString();
            _txtReceived = _state.OrderDate;
            _txtReceivedBy = _state.ReceivedByFullname;
            _txtNotes = _state.Notes;
            _txtProviderCompany = _state.ProviderCompany;
            _txtProviderContact = _state.ProviderContact;
            _txtChanged = _state.ChangedDate;
            _txtChangedBy = _state.ChangedBy;
            _showChangedFields = _state.ShowChangedFields;
            _lblChangedText = _state.ChangedDateLabel;
            _lblChangedByText = _state.ChangedByLabel;

            _currentFreightCharge = _state.CurrentFreightCharge;
            _currentTax = _state.CurrentTax;
            _currentSubTotal = _state.CurrentSubTotal;
            _currentTotalTax = _state.CurrentTotalTax;
            _currentTotal = _state.CurrentTotal;
            _txtFreightCharge = _currentFreightCharge.ToString("N2");
            _txtSalesTax = _currentTax.ToString("N2");

            _details = _state.Details.ToList();
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

    // ─────────────────────── Button Handlers ───────────────────────

    /// <summary>
    /// Port of <c>cmdApprove_Click</c>. The service re-validates the stored status, then in one
    /// transaction marks the order APPROVED, inserts Stocks + StockLog rows and bumps UnitsInStock.
    /// </summary>
    private async Task OnApproveClickAsync()
    {
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
            _state.CurrentUserId = AppState.UserId;
            await ActionOrderReceptionService.ApproveAsync(_state, AppState.UserId);

            if (!string.IsNullOrEmpty(_state.ErrorMessage))
            {
                LogStatus(_state.ErrorMessage, Severity.Warning);
                return;
            }

            await LoadDataAsync();
            Snackbar.Add(_state.SuccessMessage is { Length: > 0 } ok ? ok : "The order was successfully approved", Severity.Success);

            // Legacy closes the form after approval; navigate back
            NavigationManager.NavigateTo("/upgraded/reception-approval");
        }
        catch (Exception ex) when (ex is not NavigationException)
        {
            LogStatus($"An error has occurred adding the data. Error: {ex.Message}", Severity.Error);
        }
        finally
        {
            _isLoading = false;
        }
    }

    /// <summary>
    /// Port of <c>cmdCancel_Click</c>. Confirms with the user, then the service marks the order CANCELLED.
    /// </summary>
    private async Task OnCancelClickAsync()
    {
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
            _state.CurrentUserId = AppState.UserId;
            await ActionOrderReceptionService.CancelAsync(_state, AppState.UserId);

            if (!string.IsNullOrEmpty(_state.ErrorMessage))
            {
                LogStatus(_state.ErrorMessage, Severity.Warning);
                return;
            }

            await LoadDataAsync();
            Snackbar.Add(_state.SuccessMessage is { Length: > 0 } ok ? ok : "The order was successfully cancelled", Severity.Success);

            // Legacy closes the form after cancellation; navigate back
            NavigationManager.NavigateTo("/upgraded/reception-approval");
        }
        catch (Exception ex) when (ex is not NavigationException)
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

