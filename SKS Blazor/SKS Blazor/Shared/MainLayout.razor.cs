using Microsoft.AspNetCore.Components;
using MudBlazor;
using SKS_Blazor.Application.Services.Login;
using SKS_Blazor.Infrastructure;

namespace SKS_Blazor.Shared;

/// <summary>
/// Port of the frmMain shell (status bar and the sidebar that hosts <see cref="NavMenu"/>, which mirrors
/// the frmMain menu). There is no login screen, matching <c>modMain.Main()</c>, which sets the user
/// globals itself and opens frmMain directly. The scoped AppState is the session and starts empty on
/// every new circuit, so the identity is seeded here, once per circuit.
/// </summary>
public partial class MainLayout : IDisposable
{
    /// <summary>The user the legacy <c>modMain.Main()</c> hardcodes.</summary>
    private const string AutoLoginUserName = "acantillo";

    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private AppState AppState { get; set; } = default!;
    [Inject] private ILoginService LoginService { get; set; } = default!;
    [Inject] private ILogger<MainLayout> Logger { get; set; } = default!;

    private readonly MudTheme _theme = new();

    protected override void OnInitialized()
    {
        AppState.OnChange += OnAppStateChangedAsync;

        // Seed synchronously from the same literals as modMain.Main() so the first paint already has a
        // user; OnInitializedAsync then replaces these with the real row. Without this the sidebar and
        // status bar would render blank for one frame while the lookup runs.
        if (!AppState.IsLoggedIn)
        {
            AppState.IsLoggedIn = true;
            AppState.UserId = AutoLoginUserName;
            AppState.CurrentUser = AutoLoginUserName;
            AppState.UserFullname = "Allan Cantillo";
            AppState.UserLevel = "Administrator";
            AppState.CurrentUserAdmin = true;
        }
    }

    protected override async Task OnInitializedAsync()
    {
        if (!string.IsNullOrEmpty(AppState.StatusMessage)) return; // already seeded on this circuit

        try
        {
            // Legacy hardcodes the four globals. Reading the row instead keeps Fullname and Level —
            // and therefore CurrentUserAdmin — true to the data if the account is ever changed.
            var state = await LoginService.LoadUserAsync(AutoLoginUserName);
            if (state.LoginSucceeded)
            {
                AppState.UserId = state.UserId;
                AppState.CurrentUser = state.UserId;
                AppState.UserFullname = state.UserFullname;
                AppState.UserLevel = state.UserLevel;
                AppState.CurrentUserAdmin = state.CurrentUserAdmin;
                AppState.LogStatus(state.StatusMessage);
            }
            else
            {
                // Keep the legacy literals already seeded in OnInitialized and say so.
                Logger.LogWarning("Auto-login user {Username} not found; using the legacy hardcoded identity",
                    AutoLoginUserName);
                AppState.LogStatus($"User : {AppState.UserFullname} logged at {DateTime.Now:MM/dd/yyyy HH:mm:ss}");
            }
        }
        catch (Exception ex)
        {
            // A failed lookup must not keep the app off the screen — the legacy never consults the
            // database for this at all.
            Logger.LogError(ex, "Auto-login lookup failed; using the legacy hardcoded identity");
            AppState.LogStatus($"User : {AppState.UserFullname} logged at {DateTime.Now:MM/dd/yyyy HH:mm:ss}");
        }
    }

    // The status bar reads AppState; repaint when pages call AppState.LogStatus.
    private Task OnAppStateChangedAsync() => InvokeAsync(StateHasChanged);

    public void Dispose()
    {
        AppState.OnChange -= OnAppStateChangedAsync;
    }
}
