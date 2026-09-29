using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SKS_Blazor.Infrastructure.Data;

namespace SKS_Blazor.Application.Services.About;

/// <summary>
/// Implementation of <see cref="IAboutService"/>.
/// Port of <c>frmAbout</c> (frmAbout.cs — 70 legacy lines).
/// </summary>
public class AboutService : IAboutService
{
    private readonly IDbContextFactory<UpgradedDbContext> _dbFactory;
    private readonly ILogger<AboutService> _logger;

    public AboutService(
        IDbContextFactory<UpgradedDbContext> dbFactory,
        ILogger<AboutService> logger)
    {
        _dbFactory = dbFactory;
        _logger = logger;
    }

    /// <summary>
    /// Initializes application metadata from assembly attributes.
    /// <para>
    /// Port of <c>frmAbout.Form_Load</c>:
    /// <code>
    /// this.Text = $"About {AssemblyHelper.GetTitle(Assembly.GetExecutingAssembly())}";
    /// lblTitle.Text = AssemblyHelper.GetTitle(Assembly.GetExecutingAssembly());
    /// </code>
    /// </para>
    /// </summary>
    public async Task InitializeAsync(CancellationToken ct = default)
    {
        await Task.Run(() =>
        {
            _logger.LogInformation("Initializing About information from assembly metadata");

            // The legacy code reads from the entry assembly via AssemblyHelper.GetTitle.
            // In Blazor Server the entry assembly is the host; we read its attributes directly.
            var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();

            // Title — legacy: AssemblyHelper.GetTitle(Assembly.GetExecutingAssembly())
            var titleAttr = assembly.GetCustomAttribute<AssemblyTitleAttribute>();
            string title = !string.IsNullOrEmpty(titleAttr?.Title)
                ? titleAttr!.Title
                : Path.GetFileNameWithoutExtension(assembly.Location);

            // Version — legacy: App.Major & "." & App.Minor & "." & App.Revision
            var version = assembly.GetName().Version;
            string versionText = version is not null
                ? $"{version.Major}.{version.Minor}.{version.Build}"
                : "1.0.0";

            _logger.LogDebug("About info loaded: Title={Title}, Version={Version}", title, versionText);
        }, ct);
    }
}
