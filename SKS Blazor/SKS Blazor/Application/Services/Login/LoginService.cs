using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SKS_Blazor.Application.DTOs.Login;
using SKS_Blazor.Domain.Entities.Core;
using SKS_Blazor.Infrastructure.Data;

namespace SKS_Blazor.Application.Services.Login;

/// <summary>
/// Port of <c>frmLogin</c> — authenticates users against the existing Users table.
/// Implements credential validation originally in <c>cmdOk_Click</c>.
/// </summary>
public class LoginService : ILoginService
{
    private readonly IDbContextFactory<UpgradedDbContext> _dbFactory;
    private readonly ILogger<LoginService> _logger;

    public LoginService(
        IDbContextFactory<UpgradedDbContext> dbFactory,
        ILogger<LoginService> logger)
    {
        _dbFactory = dbFactory;
        _logger = logger;
    }

    /// <summary>
    /// Port of <c>cmdOk_Click</c> from frmLogin.cs.
    /// Legacy logic:
    ///   1. SELECT * FROM Users WHERE username = '{txtUserName.Text}' and password = '{txtPassword.Text}'
    ///   2. If rs.EOF → show error, return (login failed)
    ///   3. Read Fullname and Level from the matched row
    ///   4. Set modMain.CurrentUserAdmin = (Level == "Administrator")
    ///   5. Set LoginSucceeded = true
    ///   6. Build a status log message with user fullname and current timestamp
    /// </summary>
    public async Task<LoginStateDto> OkAsync(LoginStateDto state, object eventSender, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        // The actual username/password validation requires the page to pass them.
        // This method maintains contract compatibility with the plan.
        // Real authentication is performed via ValidateCredentialsAsync which the page calls directly.
        _logger.LogDebug("OkAsync called via interface contract");

        return new LoginStateDto
        {
            LoginSucceeded = false
        };
    }

    /// <summary>
    /// Internal helper method to validate username and password against the Users table.
    /// Called by the login page component after extracting form fields.
    /// </summary>
    public async Task<(bool Success, string Fullname, string Level, bool IsAdmin, string StatusMessage, string ErrorMessage)>
        ValidateCredentialsAsync(string username, string password, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            _logger.LogWarning("Login attempted with empty username or password");
            return (false, string.Empty, string.Empty, false, string.Empty,
                "Invalid 'Username' or 'Password', please try again!");
        }

        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        // Legacy: SELECT * FROM Users WHERE username = '...' and password = '...'
        // Using EF Core LINQ — parameterized, no SQL injection
        var user = await db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Username == username && u.Password == password, ct);

        // Legacy: if (modConnection.rs.EOF) → login failed
        if (user is null)
        {
            _logger.LogWarning("Failed login attempt for username: {Username}", username);
            return (false, string.Empty, string.Empty, false, string.Empty,
                "Invalid 'Username' or 'Password', please try again!");
        }

        // Legacy: modMain.UserFullname = Convert.ToString(modConnection.rs["Fullname"]);
        var userFullname = user.Fullname ?? string.Empty;

        // Legacy: modMain.UserLevel = Convert.ToString(modConnection.rs["Level"]);
        var userLevel = user.Level ?? string.Empty;

        // Legacy: modMain.CurrentUserAdmin = (modMain.UserLevel == "Administrator");
        var isAdmin = string.Equals(userLevel, "Administrator", StringComparison.Ordinal);

        // Legacy: modMain.LogStatus($"User : {modMain.UserFullname} logged at {DateTime.Now},{DateTime.Now}");
        var now = DateTime.UtcNow;
        var statusMessage = string.Format(
            CultureInfo.InvariantCulture,
            "User : {0} logged at {1},{2}",
            userFullname,
            now.ToString("G", CultureInfo.CurrentCulture),
            now.ToString("G", CultureInfo.CurrentCulture));

        _logger.LogInformation("User {Username} ({Fullname}) logged in successfully. Level: {Level}, IsAdmin: {IsAdmin}",
            username, userFullname, userLevel, isAdmin);

        // Legacy: LoginSucceeded = true; this.Close();
        return (true, userFullname, userLevel, isAdmin, statusMessage, string.Empty);
    }
}
