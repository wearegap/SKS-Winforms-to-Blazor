using Microsoft.AspNetCore.Components;
using MudBlazor;
using SKS_Blazor.Application.DTOs.Order;
using SKS_Blazor.Application.DTOs.Reception;
using SKS_Blazor.Application.Services.Functions;
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

    /// <summary>Port of <c>DoSearchReception</c>. The service applies every legacy filter,
    /// joins Providers/Users and sums LineTotal; the page only maps rows to the grid.</summary>
    private async Task DoSearchReception(int id = -1)
    {
        _isLoading = true;
        StateHasChanged();
        try
        {
            if (_state is null) return;

            var filter = new ReceptionApprovalFilterDto
            {
                ProviderId = id,
                ProviderName = _txtProviderName,
                ContactName = _txtContactName,
                ContactLastName = _txtContactLastName,
                OrderID = _txtOrderID,
                ProductID = _txtProductID,
                Status = _cmbStatusValue,
                FilterFromDate = _chkFrom,
                FromDate = _dtFrom,
                FilterToDate = _chkTo,
                ToDate = _dtTo
            };

            await ReceptionApprovalService.DoSearchReceptionAsync(_state, filter);

            _gridRows = _state.GridRows.Select(r => new OrderReceptionGridRow
            {
                OrderDate = r.OrderDate,
                OrderID = r.OrderID,
                ProviderName = r.ProviderName,
                ContactName = r.ContactName,
                ReceivedBy = r.ReceivedBy,
                Price = r.Price,
                Status = r.Status
            }).ToList();
            _statusMessage = $"There are {_gridRows.Count} records with the selected criteria";
            AppState.LogStatus(_statusMessage);
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
            { x => x.ActionCode, action }
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
