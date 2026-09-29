using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;
using SKS_Blazor.Pages.Splash;

namespace SKS_Blazor.Pages.Splash;

/// <summary>Port of <c>frmSplash</c> — splash screen dialog shown at application startup.</summary>
/// <remarks>
/// Legacy behavior: a timer auto-closes the splash after ~3 seconds;
/// any key press also closes it immediately.
/// Legacy methods ported: frmSplash_Activated, Form_KeyPress, Timer1_Tick, Form_Closed.
/// </remarks>
public partial class SplashDialog : ComponentBase, IDisposable
{
    [CascadingParameter]
    private IMudDialogInstance MudDialog { get; set; } = default!;

    private Timer? _autoCloseTimer;
    private bool _disposed;

    /// <summary>
    /// Port of frmSplash_Activated + Timer1 setup.
    /// The legacy form starts a Timer (Timer1) that fires once and calls this.Close().
    /// We replicate this with a System.Threading.Timer that closes the dialog after 3 seconds.
    /// </summary>
    protected override void OnInitialized()
    {
        // Port of frmSplash_Activated: no meaningful logic to port (just ActivateHelper tracking).
        // Port of Timer1 setup: legacy timer interval triggers Timer1_Tick which calls this.Close().
        // Using 3-second delay to match typical VB6 splash screen behavior.
        _autoCloseTimer = new Timer(OnTimerElapsed, null, 3000, Timeout.Infinite);
    }

    /// <summary>
    /// Port of Timer1_Tick — auto-close the splash dialog after the timer elapses.
    /// Legacy code: <c>private void Timer1_Tick(Object eventSender, EventArgs eventArgs) =&gt; this.Close();</c>
    /// </summary>
    private void OnTimerElapsed(object? state)
    {
        if (_disposed)
            return;

        try
        {
            // Timer1_Tick: fires after the configured interval and closes the splash form.
            // InvokeAsync is required because the timer callback runs on a thread-pool thread,
            // not on the Blazor synchronization context.
            _ = InvokeAsync(() =>
            {
                if (!_disposed)
                {
                    MudDialog.Close(DialogResult.Ok(true));
                }
            });
        }
        catch (ObjectDisposedException)
        {
            // Component already disposed — safe to ignore.
        }
    }

    /// <summary>
    /// Port of Form_KeyPress — any key press closes the splash dialog.
    /// Legacy code: the handler calls this.Close() on any key press.
    /// <code>
    /// private void Form_KeyPress(Object eventSender, KeyPressEventArgs eventArgs)
    /// {
    ///     int KeyAscii = Convert.ToInt32(eventArgs.KeyChar);
    ///     try { this.Close(); }
    ///     finally { if (KeyAscii == 0) { eventArgs.Handled = true; } eventArgs.KeyChar = Convert.ToChar(KeyAscii); }
    /// }
    /// </code>
    /// </summary>
    private void OnKeyPress(KeyboardEventArgs e)
    {
        // Port of Form_KeyPress: legacy code closes the form on any key press.
        // The legacy code also had a finally block that checked KeyAscii == 0 to set Handled,
        // but in Blazor there is no equivalent of suppressing the key — we just close.
        if (!_disposed)
        {
            MudDialog.Close(DialogResult.Ok(true));
        }
    }

    /// <summary>
    /// Port of Form_Closed — cleanup resources.
    /// Legacy code: empty method body, but we use this to dispose the timer.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _autoCloseTimer?.Dispose();
        _autoCloseTimer = null;
    }
}
