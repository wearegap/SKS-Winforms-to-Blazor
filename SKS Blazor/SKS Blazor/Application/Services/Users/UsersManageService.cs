using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SKS_Blazor.Application.DTOs.Core;
using SKS_Blazor.Application.DTOs.Users;
using SKS_Blazor.Infrastructure.Data;
using SKS_Blazor.Domain.Entities.Core;

namespace SKS_Blazor.Application.Services.Users;

/// <summary>Port of <c>frmUsersManage</c> — user account CRUD operations against the Users table.</summary>
public class UsersManageService : IUsersManageService
{
    private readonly IDbContextFactory<UpgradedDbContext> _dbFactory;
    private readonly ILogger<UsersManageService> _logger;

    public UsersManageService(
        IDbContextFactory<UpgradedDbContext> dbFactory,
        ILogger<UsersManageService> logger)
    {
        _dbFactory = dbFactory;
        _logger = logger;
    }

    /// <inheritdoc />
    /// <remarks>Port of <c>LoadUsers()</c>: SELECT * FROM Users, iterate rows, map Username/Fullname/Level.</remarks>
    public async Task<List<UsersDto>> LoadUsersAsync(CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var entities = await db.Users
            .AsNoTracking()
            .OrderBy(u => u.Username)
            .ToListAsync(ct);

        var result = new List<UsersDto>(entities.Count);
        foreach (var e in entities)
        {
            result.Add(new UsersDto
            {
                Username = e.Username ?? string.Empty,
                Fullname = e.Fullname ?? string.Empty,
                Level = e.Level ?? string.Empty,
                Password = e.Password ?? string.Empty
            });
        }

        return result;
    }

    /// <inheritdoc />
    /// <remarks>Port of <c>LoadUsersAvoidingWith()</c> — identical logic to LoadUsers in legacy code.</remarks>
    public async Task<List<UsersDto>> LoadUsersAvoidingWithAsync(CancellationToken ct = default)
    {
        return await LoadUsersAsync(ct);
    }

    /// <inheritdoc />
    /// <remarks>Port of <c>ClearFields()</c>: reset text fields, set button text to "&amp;Save".</remarks>
    public async Task<UsersManageStateDto> ClearFieldsAsync(UsersManageStateDto state, CancellationToken ct = default)
    {
        await Task.CompletedTask;

        state.TxtUsernameText = string.Empty;
        state.TxtPasswordText = string.Empty;
        state.TxtFullnameText = string.Empty;
        state.CboLevelSelectedIndex = -1;
        state.CboLevelText = string.Empty;
        state.CmdSaveText = "&Save";
        state.CurrentEditedUser = string.Empty;

        return state;
    }

    /// <inheritdoc />
    /// <remarks>Port of <c>cmdDelete_Click</c>: validate selection, prevent deleting last user, delete by username.</remarks>
    public async Task DeleteAsync(object eventSender, CancellationToken ct = default)
    {
        var selectedUsername = eventSender as string;

        if (string.IsNullOrWhiteSpace(selectedUsername))
        {
            throw new InvalidOperationException("Please add/select a user");
        }

        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var totalUsers = await db.Users.CountAsync(ct);
        if (totalUsers <= 1)
        {
            throw new InvalidOperationException("You cannot delete the last user");
        }

        var userEntity = await db.Users
            .FirstOrDefaultAsync(u => u.Username == selectedUsername, ct);

        if (userEntity == null)
        {
            throw new InvalidOperationException($"User '{selectedUsername}' not found");
        }

        db.Users.Remove(userEntity);
        await db.SaveChangesAsync(ct);

        _logger.LogInformation("Deleted user {Username}", selectedUsername);
    }

    /// <inheritdoc />
    /// <remarks>Port of <c>cmdEdit_Click</c>: load user by username into state, set button to "&amp;Update".</remarks>
    public async Task<UsersManageStateDto> EditAsync(UsersManageStateDto state, object eventSender, CancellationToken ct = default)
    {
        var selectedUsername = eventSender as string;

        if (string.IsNullOrWhiteSpace(selectedUsername))
        {
            state.CmdSaveText = state.CmdSaveText ?? "&Save";
            return state;
        }

        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var userEntity = await db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Username == selectedUsername, ct);

        if (userEntity == null)
        {
            state.TxtUsernameText = selectedUsername;
            _logger.LogWarning("Edit attempted for non-existent user {Username}", selectedUsername);
            return state;
        }

        state.TxtUsernameText = userEntity.Username ?? string.Empty;
        state.CurrentEditedUser = userEntity.Username ?? string.Empty;
        state.TxtPasswordText = userEntity.Password ?? string.Empty;
        state.TxtFullnameText = userEntity.Fullname ?? string.Empty;
        state.CboLevelText = userEntity.Level ?? string.Empty;
        state.CmdSaveText = "&Update";

        return state;
    }

    /// <inheritdoc />
    /// <remarks>Port of <c>cmdSave_Click</c>: validate fields, check duplicates, enforce admin constraints, insert or update.</remarks>
    public async Task SaveAsync(UsersManageStateDto state, object eventSender, CancellationToken ct = default)
    {
        var currentUserAdmin = eventSender is bool b && b;

        // Legacy: if (modFunctions.TextBoxEmpty(txtUsername)) return;
        if (string.IsNullOrWhiteSpace(state.TxtUsernameText))
        {
            throw new InvalidOperationException("Username is required");
        }

        // Legacy: if (modFunctions.TextBoxEmpty(txtPassword)) return;
        if (string.IsNullOrWhiteSpace(state.TxtPasswordText))
        {
            throw new InvalidOperationException("Password is required");
        }

        // Legacy: if (modFunctions.TextBoxEmpty(txtFullname)) return;
        if (string.IsNullOrWhiteSpace(state.TxtFullnameText))
        {
            throw new InvalidOperationException("Full name is required");
        }

        // Legacy: if (modFunctions.ComboEmpty(cboLevel)) return;
        if (string.IsNullOrWhiteSpace(state.CboLevelText))
        {
            throw new InvalidOperationException("Level is required");
        }

        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var isUpdateMode = state.CmdSaveText == "&Update";
        string statusMessage;

        if (!isUpdateMode)
        {
            // --- INSERT MODE ---
            // Legacy: modConnection.ExecuteSql($"Select * from Users where Username = '{txtUsername.Text}'");
            var existingUser = await db.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Username == state.TxtUsernameText, ct);

            // Legacy: if (!modConnection.rs.EOF) { MessageBox.Show("Add failed: Username already exists" ...); return; }
            if (existingUser != null)
            {
                throw new InvalidOperationException("Add failed: Username already exists");
            }

            // Legacy: if (cboLevel.Text != "Administrator") { check if any admin exists }
            if (state.CboLevelText != "Administrator")
            {
                var adminExists = await db.Users
                    .AsNoTracking()
                    .AnyAsync(u => u.Level == "Administrator", ct);

                // Legacy: if (rs2.EOF) { MessageBox.Show("Update failed: No any Administrator found..." ...); return; }
                if (!adminExists)
                {
                    throw new InvalidOperationException("Update failed: No any Administrator found on accounts. You are not allowed to change the level of this account");
                }
            }

            // Legacy: if (!modMain.CurrentUserAdmin && cboLevel.Text == "Administrator") { ... return; }
            if (!currentUserAdmin && state.CboLevelText == "Administrator")
            {
                throw new InvalidOperationException("You cannot add another level without being 'Administrator'");
            }

            // Legacy: modConnection.rs.AddNew(); set fields; rs.Update();
            var newUser = new Domain.Entities.Core.Users
            {
                Username = state.TxtUsernameText,
                Password = state.TxtPasswordText,
                Level = state.CboLevelText,
                Fullname = state.TxtFullnameText
            };

            db.Users.Add(newUser);
            await db.SaveChangesAsync(ct);

            statusMessage = $"Added new user {state.TxtUsernameText}";
        }
        else
        {
            // --- UPDATE MODE ---
            // Legacy: if (CurrentEditedUser != txtUsername.Text) { check if new username already exists }
            if (!string.Equals(state.CurrentEditedUser, state.TxtUsernameText, StringComparison.Ordinal))
            {
                var duplicateUser = await db.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Username == state.TxtUsernameText, ct);

                if (duplicateUser != null)
                {
                    throw new InvalidOperationException($"Username '{state.TxtUsernameText}' already exists.");
                }
            }

            // Legacy: admin-level enforcement on update
            if (state.CboLevelText != "Administrator")
            {
                // Check that at least one other administrator would remain
                var otherAdminExists = await db.Users
                    .AsNoTracking()
                    .AnyAsync(u => u.Level == "Administrator" && u.Username != state.CurrentEditedUser, ct);

                if (!otherAdminExists)
                {
                    // The edited user was the only admin and we're changing their level
                    var wasAdmin = await db.Users
                        .AsNoTracking()
                        .AnyAsync(u => u.Username == state.CurrentEditedUser && u.Level == "Administrator", ct);

                    if (wasAdmin)
                    {
                        throw new InvalidOperationException("Update failed: No any Administrator found on accounts. You are not allowed to change the level of this account");
                    }
                }
            }

            var userToUpdate = await db.Users
                .FirstOrDefaultAsync(u => u.Username == state.CurrentEditedUser, ct);

            if (userToUpdate == null)
            {
                throw new InvalidOperationException($"User '{state.CurrentEditedUser}' not found for update");
            }

            // If username changed, we need to delete old and insert new (Username is the PK)
            if (!string.Equals(state.CurrentEditedUser, state.TxtUsernameText, StringComparison.Ordinal))
            {
                db.Users.Remove(userToUpdate);
                await db.SaveChangesAsync(ct);

                var updatedUser = new Domain.Entities.Core.Users
                {
                    Username = state.TxtUsernameText,
                    Password = state.TxtPasswordText,
                    Level = state.CboLevelText,
                    Fullname = state.TxtFullnameText
                };

                db.Users.Add(updatedUser);
                await db.SaveChangesAsync(ct);
            }
            else
            {
                // Same username — just update the other fields
                // Legacy: modConnection.rs["Password"] = txtPassword.Text; etc.
                userToUpdate.Password = state.TxtPasswordText;
                userToUpdate.Level = state.CboLevelText;
                userToUpdate.Fullname = state.TxtFullnameText;
                await db.SaveChangesAsync(ct);
            }

            statusMessage = $"Record for the user {state.TxtUsernameText} has been successfully updated";
        }

        _logger.LogInformation("{StatusMessage}", statusMessage);

        // Legacy: ClearFields(); LoadUsers();
        state.TxtUsernameText = string.Empty;
        state.TxtPasswordText = string.Empty;
        state.TxtFullnameText = string.Empty;
        state.CboLevelSelectedIndex = -1;
        state.CboLevelText = string.Empty;
        state.CmdSaveText = "&Save";
        state.CurrentEditedUser = string.Empty;
    }

    /// <inheritdoc />
    /// <remarks>Port of <c>Form_Load</c>: load levels from Levels table, set initial combo state, load users.</remarks>
    public async Task<UsersManageStateDto> InitializeAsync(UsersManageStateDto state, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        // Legacy: modConnection.ExecuteSql("Select * from Levels"); while (!rs.EOF) { cboLevel.AddItem(rs["Level"]); rs.MoveNext(); }
        var levelEntities = await db.Levels
            .AsNoTracking()
            .OrderBy(l => l.Level)
            .ToListAsync(ct);

        var levels = new List<LevelsDto>(levelEntities.Count);
        foreach (var le in levelEntities)
        {
            levels.Add(new LevelsDto
            {
                Level = le.Level ?? string.Empty
            });
        }

        // Legacy: LoadUsers();
        var userEntities = await db.Users
            .AsNoTracking()
            .OrderBy(u => u.Username)
            .ToListAsync(ct);

        var users = new List<UsersDto>(userEntities.Count);
        foreach (var e in userEntities)
        {
            users.Add(new UsersDto
            {
                Username = e.Username ?? string.Empty,
                Fullname = e.Fullname ?? string.Empty,
                Level = e.Level ?? string.Empty,
                Password = e.Password ?? string.Empty
            });
        }

        state.CmdSaveText = "&Save";
        state.CurrentEditedUser = string.Empty;
        state.TxtUsernameText = string.Empty;
        state.TxtPasswordText = string.Empty;
        state.TxtFullnameText = string.Empty;

        return state;
    }

    /// <summary>Port of legacy <c>LoadUsers()</c> — internal helper that directly queries the Users table.
    /// Equivalent to <see cref="LoadUsersAsync"/> but named to match the legacy method exactly.</summary>
    private async Task<List<UsersDto>> LoadUsers(CancellationToken ct = default)
    {
        return await LoadUsersAsync(ct);
    }

    /// <summary>Port of legacy <c>LoadUsersAvoidingWith()</c> — internal helper identical to LoadUsers.
    /// In the legacy code this was a separate method with the same logic but avoiding VB6 "With" syntax.</summary>
    private async Task<List<UsersDto>> LoadUsersAvoidingWith(CancellationToken ct = default)
    {
        return await LoadUsersAsync(ct);
    }

    /// <summary>Port of legacy <c>ClearFields()</c> — internal helper that resets all form state fields.</summary>
    private UsersManageStateDto ClearFields(UsersManageStateDto state)
    {
        state.TxtUsernameText = string.Empty;
        state.TxtPasswordText = string.Empty;
        state.TxtFullnameText = string.Empty;
        state.CboLevelSelectedIndex = -1;
        state.CboLevelText = string.Empty;
        state.CmdSaveText = "&Save";
        state.CurrentEditedUser = string.Empty;
        return state;
    }
}
