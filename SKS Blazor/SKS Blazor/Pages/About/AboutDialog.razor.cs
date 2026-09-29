using System.Reflection;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using SKS_Blazor.Application.Services.About;

namespace SKS_Blazor.Pages.About;

/// <summary>Port of <c>frmAbout</c> — About dialog displaying application title, version, and description.</summary>
public partial class AboutDialog : IDisposable
{
    [CascadingParameter]
    private IMudDialogInstance MudDialog { get; set; } = default!;

    [Inject]
    private IAboutService AboutService { get; set; } = default!;

    private string _dialogTitle = string.Empty;
    private string _appTitle = string.Empty;
    private string _appVersion = string.Empty;
    private string _appDescription = string.Empty;

    /// <summary>
    /// Port of <c>Form_Load</c> — sets dialog title and application info from the executing assembly,
    /// mirroring the legacy behaviour:
    ///   this.Text = $"About {AssemblyHelper.GetTitle(...)}";
    ///   lblTitle.Text = AssemblyHelper.GetTitle(...);
    /// Also calls IAboutService.InitializeAsync for any additional initialization.
    /// </summary>
    protected override async Task OnInitializedAsync()
    {
        var assembly = Assembly.GetExecutingAssembly();

        _appTitle = GetAssemblyTitle(assembly);
        _dialogTitle = $"About {_appTitle}";

        var version = assembly.GetName().Version;
        _appVersion = version is not null
            ? $"{version.Major}.{version.Minor}.{version.Build}"
            : "1.0.0";

        var descAttr = assembly.GetCustomAttribute<AssemblyDescriptionAttribute>();
        _appDescription = descAttr?.Description ?? string.Empty;

        await AboutService.InitializeAsync();
    }

    /// <summary>
    /// Port of <c>cmdOk_Click</c> — closes the About dialog.
    /// Legacy: private void cmdOk_Click(Object eventSender, EventArgs eventArgs) => this.Close();
    /// </summary>
    private void OnCmdOkClick()
    {
        MudDialog.Close(DialogResult.Ok(true));
    }

    /// <summary>
    /// Port of <c>Form_Closed</c> — legacy handler was empty; cleanup handled by Dispose.
    /// </summary>
    public void Dispose()
    {
        // Port of Form_Closed — legacy body was empty; no resources to release.
    }

    /// <summary>
    /// Extracts the assembly title, mirroring legacy AssemblyHelper.GetTitle().
    /// </summary>
    private static string GetAssemblyTitle(Assembly assembly)
    {
        var titleAttr = assembly.GetCustomAttribute<AssemblyTitleAttribute>();
        if (titleAttr is not null && !string.IsNullOrWhiteSpace(titleAttr.Title))
        {
            return titleAttr.Title;
        }

        return Path.GetFileNameWithoutExtension(assembly.Location);
    }
}
