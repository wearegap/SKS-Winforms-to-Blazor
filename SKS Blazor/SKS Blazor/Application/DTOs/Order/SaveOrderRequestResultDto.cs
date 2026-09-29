namespace SKS_Blazor.Application.DTOs.Order;

/// <summary>
/// Result from <c>SaveAsync</c> — carries the newly created OrderID and success flag
/// so the caller can provide user feedback (CONFIRMATIONRETURN).
/// </summary>
public class SaveOrderRequestResultDto
{
    public bool Success { get; set; }
    public int NewOrderID { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
}
