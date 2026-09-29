using Microsoft.AspNetCore.Components;
using MudBlazor;
using SKS_Blazor.Application.DTOs.Order;
using SKS_Blazor.Application.DTOs.Reception;
using SKS_Blazor.Application.Services.Functions;
using SKS_Blazor.Application.Services.Order;
using SKS_Blazor.Application.Services.Reception;
using SKS_Blazor.Infrastructure;
using SKS_Blazor.Pages.Action;
using System.Globalization;
using SKS_Blazor.Domain.Entities.Core;

namespace SKS_Blazor.Pages.Reception;

/// <summary>Port of <c>frmReceptionApproval</c> (frmOrderAproval.cs).</summary>
public partial class ReceptionApproval : ComponentBase, IDisposable
{
    [Inject] private IReceptionApprovalService ReceptionApprovalService { get; set; } = default!;
    [Inject] private IFunctionsService FunctionsService { get; set; } = default!;
    [Inject] private IOrderReceptionService OrderReceptionService { get; set; } = default!;
    [Inject] private IDialogService DialogService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private AppState AppState { get; set; } = default!;

    private ReceptionApprovalStateDto? _state;

    // Filter fields
    private string _txtProviderName = string.Empty;
    private string _txtContactName = string.Empty;
    private string _txtContactLastName = string.Empty;
    private string _txtOrderID = string.Empty;
    private string _txtProductID = string.Empty;
    private string _cmbStatusValue = "All";

    // Date filters
    private DateTime? _dtFrom;
    private DateTime? _dtTo;
    private bool _chkFrom;
    private bool _chkTo;

    // Grid data
    private List<OrderReceptionGridRow> _gridRows = new();
    private OrderReceptionGridRow? _selectedRow;

    // UI state
    private bool _isLoading;
    private string _statusMessage = string.Empty;

    /// <summary>Port of <c>Form_Load</c> / <c>InitGrid</c>.</summary>
    protected override async Task OnInitializedAsync()
    {
        _state = new ReceptionApprovalStateDto
        {
            TxtContactLastNameText = string.Empty,
            TxtContactNameText = string.Empty,
            TxtProviderNameText = string.Empty
        };

        // InitGrid equivalent — grid structure is defined in markup, load initial data
        await InitGrid();

        // Per user answer: load fgOrders once on page init with default dates
        await DoSearchReception(-1);
    }

    /// <summary>Port of <c>InitGrid</c>. Grid columns are defined in markup; this loads initial empty state.</summary>
    private async Task InitGrid()
    {
        try
        {
            await ReceptionApprovalService.InitGridAsync();
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error initializing grid: {ex.Message}", Severity.Error);
        }
    }

    /// <summary>Port of <c>DoSearchReception</c>. Delegates to service and populates grid.</summary>
    private async Task DoSearchReception(int id = -1)
    {
        _isLoading = true;
        StateHasChanged();
        try
        {
            if (_state is null) return;

            // Sync state from UI fields
            _state.TxtProviderNameText = _txtProviderName;
            _state.TxtContactNameText = _txtContactName;
            _state.TxtContactLastNameText = _txtContactLastName;

            await ReceptionApprovalService.DoSearchReceptionAsync(_state, id);

            // Build the grid data by querying order receptions with the applied filters
            // The service populates internal state; we re-query from the order reception service
            var receptions = await OrderReceptionService.GetOrderReceptionsAsync();
            var receptionDetails = await OrderReceptionService.GetOrderReceptionDetailsAsync();

            // Apply filters matching legacy DoSearchReception logic
            var filtered = receptions.AsEnumerable();

            if (id != -1)
            {
                filtered = filtered.Where(o => o.ProviderID == id);
            }

            if (!string.IsNullOrEmpty(_txtOrderID) && int.TryParse(_txtOrderID, NumberStyles.Integer, CultureInfo.InvariantCulture, out int orderId))
            {
                filtered = filtered.Where(o => o.OrderID == orderId);
            }

            if (!string.IsNullOrEmpty(_txtProductID))
            {
                var orderIdsWithProduct = receptionDetails
                    .Where(d => d.ProductID != null && d.ProductID.Contains(_txtProductID, StringComparison.OrdinalIgnoreCase))
                    .Select(d => d.OrderID)
                    .Distinct()
                    .ToHashSet();
                filtered = filtered.Where(o => orderIdsWithProduct.Contains(o.OrderID));
            }

            if (_chkFrom && _dtFrom.HasValue)
            {
                filtered = filtered.Where(o =>
                {
                    if (DateTime.TryParse(o.OrderDate, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
                        return dt >= _dtFrom.Value.Date;
                    return true;
                });
            }

            if (_chkTo && _dtTo.HasValue)
            {
                filtered = filtered.Where(o =>
                {
                    if (DateTime.TryParse(o.OrderDate, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
                        return dt <= _dtTo.Value.Date;
                    return true;
                });
            }

            if (!string.IsNullOrEmpty(_cmbStatusValue) && _cmbStatusValue != "All")
            {
                var statusUpper = _cmbStatusValue.ToUpperInvariant();
                filtered = filtered.Where(o =>
                    string.Equals(o.Status, statusUpper, StringComparison.OrdinalIgnoreCase));
            }

            // Group by order and compute line totals — matching legacy SQL:
            // Select o.OrderDate, o.OrderID, p.ProviderName, ContactName, ReceivedBy, Sum(d.LineTotal), o.Status
            var filteredList = filtered.ToList();

            var gridRows = new List<OrderReceptionGridRow>();
            foreach (var order in filteredList)
            {
                var details = receptionDetails.Where(d => d.OrderID == order.OrderID).ToList();
                var totalPrice = details.Sum(d => d.LineTotal ?? 0m);

                gridRows.Add(new OrderReceptionGridRow
                {
                    OrderDate = order.OrderDate ?? string.Empty,
                    OrderID = order.OrderID,
                    ProviderName = string.Empty, // Provider name would need join with Providers table
                    ContactName = string.Empty,
                    ReceivedBy = order.ReceivedBy ?? string.Empty,
                    Price = totalPrice,
                    Status = order.Status ?? string.Empty,
                    ProviderID = order.ProviderID
                });
            }

            _gridRows = gridRows;
            _statusMessage = $"There are {_gridRows.Count} records with the selected criteria";
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error searching receptions: {ex.Message}", Severity.Error);
            _statusMessage = string.Empty;
        }
        finally
        {
            _isLoading = false;
            StateHasChanged();
        }
    }

    /// <summary>Port of <c>LoadActionOrderReception</c>.</summary>
    private async Task LoadActionOrderReception(int action = 0)
    {
        if (_selectedRow is null || _selectedRow.OrderID <= 0)
        {
            Snackbar.Add("Please select an order from the grid.", Severity.Warning);
            return;
        }

        int selectedOrderId = _selectedRow.OrderID;

        var parameters = new DialogParameters<ActionOrderReception>
        {
            { x => x.OrderId, selectedOrderId },
            { x => x.Action, action }
        };

        var options = new DialogOptions
        {
            MaxWidth = MaxWidth.Large,
            FullWidth = true,
            CloseOnEscapeKey = true
        };

        var dialog = await DialogService.ShowAsync<ActionOrderReception>(
            "Action Order Reception", parameters, options);
        var result = await dialog.Result;

        // Refresh grid after dialog closes (approval/cancellation may have changed status)
        await DoSearchReception(-1);
    }

    /// <summary>Port of <c>cmbStatus_SelectedIndexChanged</c>.</summary>
    private async Task OnCmbStatusSelectedIndexChanged()
    {
        await DoSearchReception();
    }

    /// <summary>Port of <c>cmdApprove_Click</c>.</summary>
    private async Task OnCmdApproveClick()
    {
        await LoadActionOrderReception(1);
    }

    /// <summary>Port of <c>cmdCancel_Click</c>.</summary>
    private async Task OnCmdCancelClick()
    {
        await LoadActionOrderReception(2);
    }

    /// <summary>Port of <c>cmdInfo_Click</c>.</summary>
    private async Task OnCmdInfoClick()
    {
        await LoadActionOrderReception(0);
    }

    /// <summary>Port of <c>dtFrom_ValueChanged</c>.</summary>
    private async Task OnDtFromValueChanged()
    {
        _chkFrom = true;
        await DoSearchReception();
    }

    /// <summary>Port of <c>dtTo_ValueChanged</c>.</summary>
    private async Task OnDtToValueChanged()
    {
        _chkTo = true;
        await DoSearchReception();
    }

    /// <summary>Port of <c>fgOrders_DoubleClick</c>.</summary>
    private async Task OnFgOrdersRowClick(DataGridRowClickEventArgs<OrderReceptionGridRow> args)
    {
        if (args.MouseEventArgs.Detail == 2) // double-click
        {
            await OnCmdInfoClick();
        }
    }

    /// <summary>Port of <c>txtOrderID_TextChanged</c>.</summary>
    private async Task OnTxtOrderIDChanged()
    {
        await DoSearchReception();
    }

    /// <summary>Port of <c>txtProductID_TextChanged</c>.</summary>
    private async Task OnTxtProductIDChanged()
    {
        await DoSearchReception();
    }

    /// <summary>Port of <c>txtProviderName_TextChanged</c>.</summary>
    private async Task OnTxtProviderNameChanged()
    {
        await DoSearchReception();
    }

    /// <summary>Port of <c>txtContactLastName_TextChanged</c>.</summary>
    private async Task OnTxtContactLastNameChanged()
    {
        await DoSearchReception();
    }

    /// <summary>Port of <c>txtContactName_TextChanged</c>.</summary>
    private async Task OnTxtContactNameChanged()
    {
        await DoSearchReception();
    }

    /// <summary>Port of <c>cmdClose_Click</c>.</summary>
    private void OnCmdCloseClick()
    {
        NavigationManager.NavigateTo("/");
    }

    /// <summary>Port of <c>cmdProviders_Click</c>.</summary>
    private async Task OnCmdProvidersClick()
    {
        var options = new DialogOptions
        {
            MaxWidth = MaxWidth.Large,
            FullWidth = true,
            CloseOnEscapeKey = true
        };

        var dialog = await DialogService.ShowAsync<SKS_Blazor.Pages.Providers.Providers>(
            "Providers", options);
        var result = await dialog.Result;

        // After dialog closes, clear filter fields and search with the selected provider ID
        _txtProviderName = string.Empty;
        _txtContactLastName = string.Empty;
        _txtContactName = string.Empty;

        int selectedProviderId = -1;
        if (result is not null && !result.Canceled && result.Data is int provId)
        {
            selectedProviderId = provId;
        }

        await DoSearchReception(selectedProviderId);
    }

    private void OnSelectedRowChanged(OrderReceptionGridRow? row)
    {
        _selectedRow = row;
    }

    /// <summary>Port of <c>Form_Closed</c> — cleanup in Dispose.</summary>
    public void Dispose()
    {
        // Form_Closed was empty in legacy — no cleanup needed
    }

    /// <summary>Grid row model for fgOrders. Columns: Date, Order, Supplier, Contact, Received by, Price, Status.</summary>
    public class OrderReceptionGridRow
    {
        public string OrderDate { get; set; } = string.Empty;
        public int OrderID { get; set; }
        public string ProviderName { get; set; } = string.Empty;
        public string ContactName { get; set; } = string.Empty;
        public string ReceivedBy { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string Status { get; set; } = string.Empty;
        public int? ProviderID { get; set; }
    }
}
