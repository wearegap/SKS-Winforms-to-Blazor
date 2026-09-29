using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace SKS_Blazor.Shared.Dialogs;

/// <summary>Confirmation dialog with Yes/No/Cancel — port of legacy MessageBox.Show with YesNoCancel buttons.</summary>
public partial class ConfirmSaveDialog : ComponentBase
{
    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = default!;

    [Parameter] public string Message { get; set; } = string.Empty;

    private void OnYes()
    {
        MudDialog.Close(DialogResult.Ok("Yes"));
    }

    private void OnNo()
    {
        MudDialog.Close(DialogResult.Ok("No"));
    }

    private void OnCancel()
    {
        MudDialog.Close(DialogResult.Ok("Cancel"));
    }
}
