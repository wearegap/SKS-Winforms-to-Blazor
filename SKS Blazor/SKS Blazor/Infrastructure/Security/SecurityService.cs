using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SKS_Blazor.Infrastructure.Data;
using SKS_Blazor.Domain.Entities.Core;

namespace SKS_Blazor.Infrastructure.Security;

/// <summary>
/// Authentication and authorization helper managing login validation and role-based access checks.
/// Port of the login logic from <c>frmLogin.cmdOk_Click</c>:
///   <c>SELECT * FROM Users WHERE username = '{txtUserName.Text}' and password = '{txtPassword.Text}'</c>
/// and the admin-check pattern from <c>modMain.CurrentUserAdmin = (modMain.UserLevel == "Administrator")</c>.
/// </summary>
public class SecurityService
{
    private readonly IDbContextFactory<UpgradedDbContext> _dbFactory;
    private readonly ILogger<SecurityService> _logger;

    public SecurityService(
        IDbContextFactory<UpgradedDbContext> dbFactory,
        ILogger<SecurityService> logger)
    {
        _dbFactory = dbFactory;
        _logger = logger;
    }

    /// <summary>
    /// Validates user credentials against the Users table.
    /// Port of <c>frmLogin.cmdOk_Click</c>:
    ///   <c>ExecuteSql("SELECT * FROM Users WHERE username = '...' and password = '...'")</c>
    ///   <c>if (modConnection.rs.EOF) { /* invalid */ }</c>
    /// Uses parameterized EF Core query (no SQL injection).
    /// </summary>
    /// <param name="username">The username entered by the user.</param>
    /// <param name="password">The password entered by the user (plaintext — matches legacy behavior).</param>
    /// <returns><c>true</c> if a matching user record exists; otherwise <c>false</c>.</returns>
    public async Task<bool> ValidateCredentialsAsync(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            _logger.LogWarning("ValidateCredentials: empty username or password");
            return false;
        }

        await using var db = await _dbFactory.CreateDbContextAsync();

        var user = await db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u =>
                u.Username == username &&
                u.Password == password);

        if (user is null)
        {
            _logger.LogWarning("ValidateCredentials: no matching user for Username={Username}", username);
            return false;
        }

        _logger.LogInformation("ValidateCredentials: successful login for Username={Username}", username);
        return true;
    }

    /// <summary>
    /// Checks whether a user has permission to access a given resource.
    /// Port of the legacy role-check pattern:
    ///   <c>modMain.CurrentUserAdmin = (modMain.UserLevel == "Administrator")</c>
    /// In the legacy app, only two levels existed: "Administrator" and others.
    /// Administrators have access to all resources. Non-administrators are denied
    /// access to resources prefixed with "Admin:" (e.g., "Admin:UsersManage").
    /// </summary>
    /// <param name="userId">The username to check.</param>
    /// <param name="resource">The resource identifier (e.g., "Admin:UsersManage", "Order:Create").</param>
    /// <returns><c>true</c> if the user has permission; otherwise <c>false</c>.</returns>
    public async Task<bool> HasPermissionAsync(string userId, string resource)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return false;
        }

        await using var db = await _dbFactory.CreateDbContextAsync();

        var user = await db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Username == userId);

        if (user is null)
        {
            _logger.LogWarning("HasPermission: user not found Username={Username}", userId);
            return false;
        }

        var userLevel = user.Level ?? string.Empty;
        var isAdmin = userLevel == "Administrator";

        // Administrators have full access to everything
        if (isAdmin)
        {
            return true;
        }

        // Non-admin users are blocked from admin-prefixed resources
        // This mirrors the legacy pattern where mnuSecurity (UsersManage) was only
        // fully functional for admins, and cboLevel was restricted for non-admins.
        if (resource.StartsWith("Admin:"))
        {
            _logger.LogInformation(
                "HasPermission: denied for Username={Username}, Resource={Resource}, Level={Level}",
                userId, resource, userLevel);
            return false;
        }

        // All other resources are accessible to any authenticated user
        return true;
    }
}
