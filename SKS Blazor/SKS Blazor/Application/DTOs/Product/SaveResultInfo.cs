namespace SKS_Blazor.Application.DTOs.Product;

/// <summary>
/// Result of a save operation, carrying success/failure info and a user message.
/// Port of the legacy MessageBox.Show calls in <c>cmdSave_Click</c>.
/// </summary>
public class SaveResultInfo
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public bool NoChanges { get; set; }
}
