#nullable disable
namespace SKS_Blazor.Application.DTOs.Core;

public class RequestApprovalGridRowDto
{
    public string OrderDate { get; set; } = string.Empty;
    public int OrderID { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string ContactName { get; set; } = string.Empty;
    public string ReceivedBy { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Status { get; set; } = string.Empty;
}
