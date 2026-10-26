using System.Globalization;
using Microsoft.EntityFrameworkCore;
using SKS_Blazor.Application.DTOs.Login;
using SKS_Blazor.Infrastructure.Data;

namespace SKS_Blazor.Application.Services.Login;

/// <summary>Port of <c>frmLogin</c> — authenticates users against the existing Users table.</summary>
public class LoginService : ILoginService
{
    /// <summary>Legacy MessageBox text (frmLogin.cmdOk_Click).</summary>
    public const string InvalidCredentialsMessage = "Invalid 'Username' or 'Password', please try again!";

    private readonly IDbContextFactory<UpgradedDbContext> _dbFactory;
    private readonly ILogger<LoginService> _logger;

    public LoginService(IDbContextFactory<UpgradedDbContext> dbFactory, ILogger<LoginService> logger)
    {
        _dbFactory = dbFactory;
        _logger = logger;
    }

    /// <summary>
    /// Port of <c>frmLogin.cmdOk_Click</c>:
    /// <code>SELECT * FROM Users WHERE username = '..' and password = '..'</code>
    /// On a match: UserFullname/UserLevel from the row, CurrentUserAdmin = Level == "Administrator",
    /// LogStatus("User : {Fullname} logged at {now}"). The query is parameterised (the legacy string was not).
    /// </summary>
    public async Task<LoginStateDto> OkAsync(LoginStateDto state, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(state);
        state.LoginSucceeded = false;
        state.ErrorMessage = string.Empty;

        var userName = (state.UserName ?? string.Empty).Trim();
        var password = state.Password ?? string.Empty;
        if (userName.Length == 0 || password.Length == 0)
        {
            state.ErrorMessage = InvalidCredentialsMessage;
            return state;
        }

        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var user = await db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Username == userName && u.Password == password, ct);

        if (user is null)
        {
            _logger.LogWarning("Failed login attempt for {Username}", userName);
            state.ErrorMessage = InvalidCredentialsMessage;
            return state;
        }

        state.LoginSucceeded = true;
        state.UserId = user.Username;
        state.UserFullname = user.Fullname ?? string.Empty;
        state.UserLevel = user.Level ?? string.Empty;
        state.CurrentUserAdmin = string.Equals(state.UserLevel, "Administrator", StringComparison.Ordinal);
        state.StatusMessage = string.Format(CultureInfo.InvariantCulture, "User : {0} logged at {1:MM/dd/yyyy HH:mm:ss}",
            state.UserFullname, DateTime.Now);
        state.Password = string.Empty; // do not keep the secret in circuit state

        _logger.LogInformation("User {Username} logged in. Level: {Level}", state.UserId, state.UserLevel);
        return state;
    }

    /// <inheritdoc />
    public async Task<LoginStateDto> LoadUserAsync(string userName, CancellationToken ct = default)
    {
        var state = new LoginStateDto { UserName = userName ?? string.Empty };

        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var user = await db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Username == state.UserName, ct);

        if (user is null)
        {
            _logger.LogWarning("Auto-login user {Username} is not in the Users table", state.UserName);
            return state;
        }

        state.LoginSucceeded = true;
        state.UserId = user.Username;
        state.UserFullname = user.Fullname ?? string.Empty;
        state.UserLevel = user.Level ?? string.Empty;
        state.CurrentUserAdmin = string.Equals(state.UserLevel, "Administrator", StringComparison.Ordinal);
        state.StatusMessage = string.Format(CultureInfo.InvariantCulture, "User : {0} logged at {1:MM/dd/yyyy HH:mm:ss}",
            state.UserFullname, DateTime.Now);

        _logger.LogInformation("Auto-login as {Username}. Level: {Level}", state.UserId, state.UserLevel);
        return state;
    }
}
