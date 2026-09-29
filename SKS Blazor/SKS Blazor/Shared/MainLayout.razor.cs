using Microsoft.AspNetCore.Components;
using MudBlazor;
using SKS_Blazor.Infrastructure;
using SKS_Blazor.Pages.About;

namespace SKS_Blazor.Shared;

public partial class MainLayout
{
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private IDialogService DialogService { get; set; } = default!;
    [Inject] private AppState AppState { get; set; } = default!;

    private readonly MudTheme _theme = new();

    private void NavigateTo(string url) => NavigationManager.NavigateTo(url);

    private async Task Refresh()
    {
        await AppState.SendCommandAsync("Refresh");
    }

    private async Task ShowAbout()
    {
        await DialogService.ShowAsync<AboutDialog>("About");
    }

    private void Exit()
    {
        AppState.IsLoggedIn = false;
        NavigationManager.NavigateTo("/", forceLoad: true);
    }
}
