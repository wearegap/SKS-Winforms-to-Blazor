using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using MudBlazor;
using SKS_Blazor.Infrastructure;

namespace SKS_Blazor.Shared;

/// <summary>
/// Port of the frmMain shell (status bar and the sidebar that hosts <see cref="NavMenu"/>, which mirrors
/// the frmMain menu). Also the login guard: the scoped AppState is the user session, and it starts empty
/// on every new circuit (first load, F5, deep link), so any page other than /login redirects there until
/// LoginDialog sets IsLoggedIn.
/// </summary>
public partial class MainLayout : IDisposable
{
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private AppState AppState { get; set; } = default!;

    private readonly MudTheme _theme = new();

    protected override void OnInitialized()
    {
        AppState.OnChange += OnAppStateChangedAsync;
        NavigationManager.LocationChanged += OnLocationChanged;
        RedirectIfAnonymous(NavigationManager.Uri);
    }

    // The status bar reads AppState; repaint when pages call AppState.LogStatus.
    private Task OnAppStateChangedAsync() => InvokeAsync(StateHasChanged);

    private void OnLocationChanged(object? sender, LocationChangedEventArgs e) => RedirectIfAnonymous(e.Location);

    private void RedirectIfAnonymous(string uri)
    {
        if (AppState.IsLoggedIn) return;
        var path = NavigationManager.ToBaseRelativePath(uri).Split('?', 2)[0].Trim('/');
        if (path.Equals("login", StringComparison.OrdinalIgnoreCase)) return;
        NavigationManager.NavigateTo(path.Length == 0 ? "/login" : $"/login?returnUrl={Uri.EscapeDataString(path)}");
    }

    public void Dispose()
    {
        AppState.OnChange -= OnAppStateChangedAsync;
        NavigationManager.LocationChanged -= OnLocationChanged;
    }
}
