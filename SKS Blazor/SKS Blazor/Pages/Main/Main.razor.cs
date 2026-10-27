using Microsoft.AspNetCore.Components;
using MudBlazor;
using SKS_Blazor.Application.DTOs.Customers;
using SKS_Blazor.Application.Services.Customers;
using SKS_Blazor.Infrastructure;
using SKS_Blazor.Pages.About;
using SKS_Blazor.Pages.Customers;
using SKS_Blazor.Pages.Order;
using SKS_Blazor.Pages.Splash;
using SKS_Blazor.Domain.Entities.Core;

namespace SKS_Blazor.Pages.Main;

/// <summary>Port of <c>frmMain</c> (MDI parent form). Dashboard landing page.</summary>
public partial class Main : ComponentBase, IDisposable
{
    [Inject] private IDialogService DialogService { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private ICustomersService CustomersService { get; set; } = default!;
    [Inject] private AppState _appState { get; set; } = default!;
    [Inject] private ILogger<Main> Logger { get; set; } = default!;

    private bool _firstRenderDone;

    protected override void OnInitialized()
    {
        // frmMain_Activated — legacy tracked the active form. In Blazor,
        // the page component lifecycle handles focus. No equivalent needed.

        // Subscribe to layout command dispatch (rule #14)
        _appState.OnCommand += HandleLayoutCommandAsync;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && !_firstRenderDone)
        {
            _firstRenderDone = true;

            // MDIForm_Load: show splash then open order request
            await ShowSplashDialogAsync();

            // Legacy: frmOrderRequest.DefInstance.Show()
            // In route-based approach, navigate to order request page
            NavigationManager.NavigateTo("/upgraded/order-request", replace: true);
        }
    }

    /// <summary>
    /// Handles commands dispatched from MainLayout via AppState (rule #14).
    /// Maps legacy menu keys to the appropriate handler.
    /// </summary>
    private async Task HandleLayoutCommandAsync(string command)
    {
        switch (command)
        {
            case "mnuAbout":
                await OnAboutClick();
                break;
            case "mnuAddStockManually":
                await OnAddStockManuallyClick();
                break;
            case "mnuAdjustStockManually":
                await OnAdjustStockManuallyClick();
                break;
            case "mnuCreateOrderReception":
                await OnCreateOrderReceptionClick();
                break;
            case "mnuCreateOrderRequest":
                await OnCreateOrderRequestClick();
                break;
            case "mnuCustomer":
                await OnCustomerClick();
                break;
            case "mnuOrderReceptionsApproval":
                await OnOrderReceptionsApprovalClick();
                break;
            case "mnuOrderRequestsApproval":
                await OnOrderRequestsApprovalClick();
                break;
            case "mnuProducts":
                await OnProductsClick();
                break;
            case "mnuProviders":
                await OnProvidersClick();
                break;
            case "mnuSecurity":
                await OnSecurityClick();
                break;
        }
    }

    /// <summary>Port of <c>MDIForm_Load</c>: shows splash dialog.</summary>
    private async Task ShowSplashDialogAsync()
    {
        try
        {
            var options = new DialogOptions
            {
                CloseButton = false,
                NoHeader = true,
                MaxWidth = MaxWidth.Small,
                FullWidth = false,
                BackdropClick = false
            };
            var dialog = await DialogService.ShowAsync<SplashDialog>("", options);
            await dialog.Result;
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Splash dialog could not be displayed");
        }
    }

    /// <summary>Port of <c>mnuAbout_Click</c>: opens About dialog.</summary>
    private async Task OnAboutClick()
    {
        // Legacy: frmAbout.DefInstance.ShowDialog(this)
        var options = new DialogOptions
        {
            CloseButton = true,
            MaxWidth = MaxWidth.Small,
            FullWidth = false
        };
        var dialog = await DialogService.ShowAsync<AboutDialog>("About SKS", options);
        await dialog.Result;
    }

    /// <summary>Port of <c>mnuAddStockManually_Click</c>: navigates to Add Stock Manual page.</summary>
    private Task OnAddStockManuallyClick()
    {
        // Legacy: frmAddStockManual.DefInstance.Show() — non-modal
        NavigationManager.NavigateTo("/upgraded/add-stock-manual");
        return Task.CompletedTask;
    }

    /// <summary>Port of <c>mnuAdjustStockManually_Click</c>: navigates to Adjust Stock Manual page.</summary>
    private Task OnAdjustStockManuallyClick()
    {
        // Legacy: frmAdjustStockManual.DefInstance.Show() — non-modal
        NavigationManager.NavigateTo("/upgraded/adjust-stock-manual");
        return Task.CompletedTask;
    }

    /// <summary>Port of <c>mnuCreateOrderReception_Click</c>: navigates to Order Reception page.</summary>
    private Task OnCreateOrderReceptionClick()
    {
        // Legacy: frmOrderReception.DefInstance.Show() — non-modal
        NavigationManager.NavigateTo("/upgraded/order-reception");
        return Task.CompletedTask;
    }

    /// <summary>Port of <c>mnuCreateOrderRequest_Click</c>: navigates to Order Request page.</summary>
    private Task OnCreateOrderRequestClick()
    {
        // Legacy: frmOrderRequest.DefInstance.Show() — non-modal
        NavigationManager.NavigateTo("/upgraded/order-request");
        return Task.CompletedTask;
    }

    /// <summary>Port of <c>mnuCustomer_Click</c>: opens Customers dialog, then re-inits.</summary>
    private async Task OnCustomerClick()
    {
        // Legacy: frmCustomers.DefInstance.ShowDialog() — modal
        //         frmCustomers.DefInstance.InitForm()
        var options = new DialogOptions
        {
            CloseButton = true,
            MaxWidth = MaxWidth.Large,
            FullWidth = true,
            CloseOnEscapeKey = true
        };
        var dialog = await DialogService.ShowAsync<Customers.Customers>("Customers", options);
        var result = await dialog.Result;

        // After dialog closes, legacy called InitForm to reset state.
        // The Customers dialog handles its own init on open, but we call
        // InitFormAsync for any post-close state reset the service requires.
        try
        {
            var state = new CustomersStateDto
            {
                NewMode = false,
                EditMode = false,
                CancellingMode = false
            };
            await CustomersService.InitFormAsync(state);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Error re-initializing customers state after dialog close");
        }
    }

    /// <summary>Port of <c>mnuOrderReceptionsApproval_Click</c>: navigates to Reception Approval.</summary>
    private Task OnOrderReceptionsApprovalClick()
    {
        // Legacy: frmReceptionApproval.DefInstance.Show() — non-modal
        NavigationManager.NavigateTo("/upgraded/reception-approval");
        return Task.CompletedTask;
    }

    /// <summary>Port of <c>mnuOrderRequestsApproval_Click</c>: navigates to Request Approval.</summary>
    private Task OnOrderRequestsApprovalClick()
    {
        // Legacy: frmRequestApproval.DefInstance.Show() — non-modal
        NavigationManager.NavigateTo("/upgraded/request-approval");
        return Task.CompletedTask;
    }

    /// <summary>Port of <c>mnuProducts_Click</c>: opens Products dialog.</summary>
    private async Task OnProductsClick()
    {
        // Legacy: frmProducts.DefInstance.ShowDialog() — modal
        var options = new DialogOptions
        {
            CloseButton = true,
            MaxWidth = MaxWidth.Large,
            FullWidth = true,
            CloseOnEscapeKey = true
        };
        var dialog = await DialogService.ShowAsync<SKS_Blazor.Pages.Products.Products>("Products", options);
        await dialog.Result;
    }

    /// <summary>Port of <c>mnuProviders_Click</c>: opens Providers dialog.</summary>
    private async Task OnProvidersClick()
    {
        // Legacy: frmProviders.DefInstance.ShowDialog() — modal
        var options = new DialogOptions
        {
            CloseButton = true,
            MaxWidth = MaxWidth.Large,
            FullWidth = true,
            CloseOnEscapeKey = true
        };
        var dialog = await DialogService.ShowAsync<SKS_Blazor.Pages.Providers.Providers>("Providers", options);
        await dialog.Result;
    }

    /// <summary>Port of <c>mnuSecurity_Click</c>: navigates to Users Manage page.</summary>
    private Task OnSecurityClick()
    {
        // Legacy: frmUsersManage.DefInstance.Show() — non-modal
        NavigationManager.NavigateTo("/upgraded/users-manage");
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        // Unsubscribe from AppState command dispatch
        _appState.OnCommand -= HandleLayoutCommandAsync;
    }
}
