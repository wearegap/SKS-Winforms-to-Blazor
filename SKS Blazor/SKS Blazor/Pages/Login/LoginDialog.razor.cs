using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;
using SKS_Blazor.Application.DTOs.Login;
using SKS_Blazor.Application.Services.Login;
using SKS_Blazor.Infrastructure;
using SKS_Blazor.Application.Services.Functions;

namespace SKS_Blazor.Pages.Login;

/// <summary>Port of <c>frmLogin</c> — primary application login dialog.</summary>
public partial class LoginDialog : IDisposable
{
    [CascadingParameter]
    private IMudDialogInstance MudDialog { get; set; } = default!;

    [Inject]
    private ILoginService LoginService { get; set; } = default!;

    [Inject]
    private IFunctionsService FunctionsService { get; set; } = default!;

    [Inject]
    private AppState AppState { get; set; } = default!;

    [Inject]
    private ISnackbar Snackbar { get; set; } = default!;

    private LoginStateDto? _state;

    private string _userName = string.Empty;
    private string _password = string.Empty;
    private string _errorMessage = string.Empty;
    private bool _isProcessing;

    // Password visibility toggle
    private bool _passwordVisible;
    private InputType _passwordInputType = InputType.Password;
    private string _passwordVisibilityIcon = Icons.Material.Filled.VisibilityOff;

    protected override void OnInitialized()
    {
        // frmLogin_Activated — legacy just tracked active form; no equivalent needed in Blazor dialog.
        // Initialize the shared state once for the dialog's lifetime.
        _state = new LoginStateDto { LoginSucceeded = false };
    }

    /// <summary>Port of <c>cmdOk_Click</c> — validates credentials via service and closes on success.</summary>
    private async Task OnOkClick()
    {
        _errorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(_userName) || string.IsNullOrWhiteSpace(_password))
        {
            _errorMessage = "Invalid 'Username' or 'Password', please try again!";
            return;
        }

        _isProcessing = true;

        try
        {
            _state = new LoginStateDto { UserName = _userName, Password = _password };
            _state = await LoginService.OkAsync(_state);

            if (!_state.LoginSucceeded)
            {
                // Port of: MessageBox.Show("Invalid 'Username' or 'Password', please try again!", ...)
                _errorMessage = _state.ErrorMessage;
                _password = string.Empty;
                return;
            }

            // Port of the modMain globals set by cmdOk_Click; AppState is scoped to the circuit (one user session).
            AppState.UserId = _state.UserId;
            AppState.CurrentUser = _state.UserId;
            AppState.UserFullname = _state.UserFullname;
            AppState.UserLevel = _state.UserLevel;
            AppState.CurrentUserAdmin = _state.CurrentUserAdmin;
            AppState.IsLoggedIn = true;
            AppState.LogStatus(_state.StatusMessage);

            // Login succeeded — close dialog and return success to the caller
            MudDialog.Close(DialogResult.Ok(true));
        }
        catch (Exception ex)
        {
            _errorMessage = $"Login error: {ex.Message}";
        }
        finally
        {
            _isProcessing = false;
        }
    }

    /// <summary>Port of <c>cmdCancel_Click</c> — sets LoginSucceeded = false and closes.</summary>
    private void OnCancelClick()
    {
        // Port of: LoginSucceeded = false; this.Close();
        if (_state is not null)
        {
            _state.LoginSucceeded = false;
        }

        MudDialog.Cancel();
    }

    /// <summary>Allow Enter key to submit the form (legacy Tab order / Enter behavior).</summary>
    private async Task OnKeyDown(KeyboardEventArgs args)
    {
        if (args.Key == "Enter")
        {
            await OnOkClick();
        }
    }

    /// <summary>Toggle password field visibility.</summary>
    private void TogglePasswordVisibility()
    {
        _passwordVisible = !_passwordVisible;
        _passwordInputType = _passwordVisible ? InputType.Text : InputType.Password;
        _passwordVisibilityIcon = _passwordVisible
            ? Icons.Material.Filled.Visibility
            : Icons.Material.Filled.VisibilityOff;
    }

    /// <summary>Port of <c>Form_Closed</c> — legacy was empty; Dispose handles cleanup.</summary>
    public void Dispose()
    {
        // Form_Closed was empty in legacy. No resources to release.
    }
}
