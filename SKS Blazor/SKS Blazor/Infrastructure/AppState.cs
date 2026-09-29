using System;
using System.Threading.Tasks;

namespace SKS_Blazor.Infrastructure;

/// <summary>
/// Global Blazor application state singleton. Port of <c>modMain</c> global variables
/// (UserId, UserFullname, UserLevel, CurrentUserAdmin, ConnectionString, msg, i)
/// and <c>modMain.LogStatus</c> / <c>modMain.ClearLogStatus</c>.
/// </summary>
public class AppState
{
    public string CurrentUser { get; set; } = string.Empty;

    public string CurrentCompany { get; set; } = string.Empty;

    public int CurrentYear { get; set; } = DateTime.Now.Year;

    public bool IsLoggedIn { get; set; }

    public string StatusMessage { get; set; } = string.Empty;

    /// <summary>
    /// Layout → page command dispatch event.
    /// Pages subscribe with += and unsubscribe with -= in Dispose.
    /// Pattern: <c>AppState.OnCommand += HandleCommandAsync;</c>
    /// </summary>
    public Func<string, Task>? OnCommand { get; set; }

    /// <summary>
    /// General state-change notification event for UI refresh.
    /// </summary>
    public Func<Task>? OnChange { get; set; }

    // ── Legacy modMain fields ──────────────────────────────────────

    /// <summary>Port of <c>modMain.UserId</c>.</summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>Port of <c>modMain.UserFullname</c>.</summary>
    public string UserFullname { get; set; } = string.Empty;

    /// <summary>Port of <c>modMain.UserLevel</c>.</summary>
    public string UserLevel { get; set; } = string.Empty;

    /// <summary>Port of <c>modMain.CurrentUserAdmin</c>.</summary>
    public bool CurrentUserAdmin { get; set; }

    /// <summary>Port of <c>modMain.ConnectionString</c> — not used directly with EF Core,
    /// but preserved for legacy parity.</summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>Port of <c>modMain.msg</c> — transient message buffer used during save operations.</summary>
    public string Msg { get; set; } = string.Empty;

    // ── Methods ────────────────────────────────────────────────────

    /// <summary>
    /// Sends a command string from MainLayout to the active page component.
    /// Port of toolbar/menu action dispatch in <c>frmMain</c>.
    /// </summary>
    public async Task SendCommandAsync(string command)
    {
        var handler = OnCommand;
        if (handler is not null)
        {
            await handler.Invoke(command);
        }
    }

    /// <summary>
    /// Port of <c>modMain.LogStatus</c>. Sets the status bar message and notifies listeners.
    /// </summary>
    public void LogStatus(string message)
    {
        StatusMessage = message;
        // Fire-and-forget notification for UI refresh
        _ = NotifyStateChangedAsync();
    }

    /// <summary>
    /// Port of <c>modMain.ClearLogStatus</c>. Clears the status bar message.
    /// </summary>
    public void ClearLogStatus()
    {
        StatusMessage = string.Empty;
        _ = NotifyStateChangedAsync();
    }

    /// <summary>
    /// Notifies all subscribers that application state has changed,
    /// triggering UI re-renders in listening components.
    /// </summary>
    public async Task NotifyStateChangedAsync()
    {
        var handler = OnChange;
        if (handler is not null)
        {
            await handler.Invoke();
        }
    }
}
