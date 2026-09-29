namespace SKS_Blazor.Application.DTOs.Order;

/// <summary>
/// Result DTO returned by <c>OrderReceptionService.SaveAsync</c>.
/// Indicates success/failure and provides the new order ID on success.
/// </summary>
public class SaveOrderReceptionResultDto
{
    /// <summary>Whether the save operation succeeded.</summary>
    public bool Success { get; set; }

    /// <summary>The newly created OrderID (identity column value) on success.</summary>
    public int NewOrderID { get; set; }

    /// <summary>Error message on failure, empty on success.</summary>
    public string ErrorMessage { get; set; } = string.Empty;
}
