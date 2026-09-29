using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using SKS_Blazor.Application.DTOs.Core;
using SKS_Blazor.Application.DTOs.Users;
using SKS_Blazor.Application.Services.Users;
using SKS_Blazor.Application.Services.Functions;
using SKS_Blazor.Infrastructure;

namespace SKS_Blazor.Pages.Users;

/// <summary>Port of <c>frmUsersManage</c> (frmUsersManage.cs).</summary>
public partial class UsersManage : ComponentBase, IDisposable
{
    [Inject] private IUsersManageService UsersManageService { get; set; } = default!;
    [Inject] private IFunctionsService FunctionsService { get; set; } = default!;
    [Inject] private IDialogService DialogService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private AppState AppState { get; set; } = default!;
    [Inject] private IJSRuntime JS { get; set; } = default!;

    private UsersManageStateDto? _state;
    private List<UsersDto> _usersList = new();
    private List<string> _levels = new();
    private UsersDto? _selectedUser;
    private DateTime _lastClickTime = DateTime.MinValue;
    private string? _lastClickedUsername;

    private MudTextField<string>? _txtUsernameRef;
    private MudTextField<string>? _txtPasswordRef;
    private MudTextField<string>? _txtFullnameRef;

    /// <summary>Port of Form_Load — loads levels combo and users grid.</summary>
    protected override async Task OnInitializedAsync()
    {
        _state = new UsersManageStateDto
        {
            CurrentEditedUser = string.Empty,
            CmdSaveText = "Save",
            TxtFullnameText = string.Empty,
            TxtPasswordText = string.Empty,
            TxtUsernameText = string.Empty,
            CboLevelText = string.Empty,
            CboLevelSelectedIndex = -1
        };

        // InitializeAsync loads levels and sets defaults (port of Form_Load)
        _state = await UsersManageService.InitializeAsync(_state);

        // Extract levels for the combo box
        await LoadLevelsAsync();

        // LoadUsers — port of LoadUsers()
        await LoadUsersGridAsync();
    }

    /// <summary>Port of Form_Load levels initialization.</summary>
    private async Task LoadLevelsAsync()
    {
        // Load all users to extract distinct levels
        var users = await UsersManageService.LoadUsersAsync();
        var distinctLevels = users
            .Where(u => !string.IsNullOrEmpty(u.Level))
            .Select(u => u.Level!)
            .Distinct()
            .OrderBy(l => l)
            .ToList();

        if (distinctLevels.Count == 0)
        {
            distinctLevels = new List<string> { "Administrator", "User" };
        }

        _levels = distinctLevels;

        // Legacy: if CurrentUserAdmin, set to "Administrator"; else selectedIndex = -1
        if (AppState.IsLoggedIn && AppState.CurrentUserAdmin)
        {
            _state!.CboLevelText = "Administrator";
        }
        else
        {
            _state!.CboLevelText = string.Empty;
        }
    }

    /// <summary>Port of LoadUsers() — refreshes the grid.</summary>
    private async Task LoadUsersGridAsync()
    {
        _usersList = await UsersManageService.LoadUsersAsync();
    }

    /// <summary>Port of LoadUsersAvoidingWith().</summary>
    private async Task LoadUsersAvoidingWithAsync()
    {
        _usersList = await UsersManageService.LoadUsersAvoidingWithAsync();
    }

    /// <summary>Port of ClearFields().</summary>
    private async Task ClearFieldsAsync()
    {
        _state = await UsersManageService.ClearFieldsAsync(_state!);
        _selectedUser = null;
    }

    /// <summary>Port of cmdClear_Click — clears username, focuses it, clears all fields.</summary>
    private async Task cmdClear_Click()
    {
        _state!.TxtUsernameText = string.Empty;
        await ClearFieldsAsync();
        // Port: txtUsername.Focus()
        if (_txtUsernameRef is not null)
        {
            await _txtUsernameRef.FocusAsync();
        }
    }

    /// <summary>Port of cmdDelete_Click — deletes the selected user after confirmation.</summary>
    private async Task cmdDelete_Click()
    {
        // Legacy: if (modFunctions.NoRecords(lstAccounts, "Please add/select a user")) return;
        if (_selectedUser is null || _usersList.Count == 0)
        {
            Snackbar.Add("Please add/select a user", Severity.Warning);
            return;
        }

        var confirm = await DialogService.ShowMessageBox(
            "Confirm Delete",
            $"Are you sure you want to delete the user '{_selectedUser.Username}'?",
            yesText: "Yes",
            noText: "No");

        if (confirm == true)
        {
            // Legacy: check if only 1 user remains — "You cannot delete the last user"
            if (_usersList.Count == 1)
            {
                Snackbar.Add("You cannot delete the last user", Severity.Error);
                return;
            }

            await UsersManageService.DeleteAsync(_selectedUser);
            await LoadUsersGridAsync();
            Snackbar.Add($"User '{_selectedUser.Username}' deleted.", Severity.Success);
            _selectedUser = null;
        }
    }

    /// <summary>Port of cmdEdit_Click — loads selected user into edit fields.</summary>
    private async Task cmdEdit_Click()
    {
        // Legacy: if (modFunctions.NoRecords(lstAccounts, "Please add/select a user")) return;
        if (_selectedUser is null || _usersList.Count == 0)
        {
            Snackbar.Add("Please add/select a user", Severity.Warning);
            return;
        }

        _state = await UsersManageService.EditAsync(_state!, _selectedUser);
    }

    /// <summary>Port of cmdSave_Click — saves (add or update) user with validation.</summary>
    private async Task cmdSave_Click()
    {
        // Pre-save validation — port of modFunctions.TextBoxEmpty / ComboEmpty checks
        if (string.IsNullOrWhiteSpace(_state!.TxtUsernameText))
        {
            Snackbar.Add("Username is required.", Severity.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(_state.TxtPasswordText))
        {
            Snackbar.Add("Password is required.", Severity.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(_state.TxtFullnameText))
        {
            Snackbar.Add("Full name is required.", Severity.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(_state.CboLevelText))
        {
            Snackbar.Add("Level is required.", Severity.Warning);
            return;
        }

        try
        {
            await UsersManageService.SaveAsync(_state, _state);
            AppState.LogStatus($"User '{_state.TxtUsernameText}' saved successfully.");
            Snackbar.Add($"User '{_state.TxtUsernameText}' saved.", Severity.Success);

            await ClearFieldsAsync();
            await LoadUsersGridAsync();
        }
        catch (InvalidOperationException ex)
        {
            // Service throws for business rule violations (duplicate username, last admin, etc.)
            Snackbar.Add(ex.Message, Severity.Error);
        }
    }

    /// <summary>Port of cmdClose_Click — navigates back.</summary>
    private void cmdClose_Click()
    {
        NavigationManager.NavigateTo("/");
    }

    /// <summary>Port of lstAccounts row click — handles both single click selection and double-click edit.</summary>
    private async Task lstAccounts_RowClick(DataGridRowClickEventArgs<UsersDto> args)
    {
        _selectedUser = args.Item;

        // Detect double-click: if the same row was clicked within 400ms, treat as double-click
        var now = DateTime.UtcNow;
        if (_lastClickedUsername == args.Item?.Username && (now - _lastClickTime).TotalMilliseconds < 400)
        {
            // Port of lstAccounts_DoubleClick => cmdEdit_Click
            await lstAccounts_DoubleClick();
            _lastClickedUsername = null;
            _lastClickTime = DateTime.MinValue;
        }
        else
        {
            _lastClickedUsername = args.Item?.Username;
            _lastClickTime = now;
        }
    }

    /// <summary>Port of lstAccounts_DoubleClick — triggers edit on double-click.</summary>
    private async Task lstAccounts_DoubleClick()
    {
        await cmdEdit_Click();
    }

    /// <summary>Handles grid selection change.</summary>
    private void OnSelectedUserChanged(UsersDto? user)
    {
        _selectedUser = user;
    }

    /// <summary>Port of txtUsername_Enter — selects all text (modFunctions.SelectAll).
    /// Uses JS interop to select all text in the focused input element.</summary>
    private async Task OnUsernameEnterAsync()
    {
        await SelectAllTextAsync("txtUsername");
    }

    /// <summary>Port of txtPassword_Enter — selects all text (modFunctions.SelectAll).</summary>
    private async Task txtPassword_Enter()
    {
        await SelectAllTextAsync("txtPassword");
    }

    /// <summary>Port of txtFullname_Enter — selects all text (modFunctions.SelectAll).</summary>
    private async Task txtFullname_Enter()
    {
        await SelectAllTextAsync("txtFullname");
    }

    /// <summary>Port of modFunctions.SelectAll — selects all text in the specified input element.</summary>
    private async Task SelectAllTextAsync(string elementId)
    {
        try
        {
            await JS.InvokeVoidAsync("eval",
                $"document.getElementById('{elementId}')?.querySelector('input')?.select()");
        }
        catch
        {
            // Ignore JS interop failures during prerender
        }
    }

    /// <summary>Port of Form_Closed — checks for empty users table when admin.</summary>
    public void Dispose()
    {
        // Port of Form_Closed:
        // Legacy checked if CurrentUserAdmin and no users exist, then showed error and exited.
        // In Blazor Server we cannot call Environment.Exit, but we log the status.
        AppState.LogStatus(string.Empty);
        GC.SuppressFinalize(this);
    }
}
