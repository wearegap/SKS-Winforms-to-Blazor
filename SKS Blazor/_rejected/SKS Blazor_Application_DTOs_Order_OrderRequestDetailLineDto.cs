using SKS_Blazor.Application.DTOs.Action;
namespace SKS_Blazor.Application.DTOs.Order;

/// <summary>DTO for the fgDetails grid in ActionOrderRequest. Each row represents one OrderRequestDetail joined with Product info.
/// Port of legacy grid columns: Quantity, Code, Product, UnitPrice, Price, Existence, Ordered, Quantity per unit, Line Total.</summary>
public class OrderRequestDetailLineDto
{
    public decimal Quantity { get; set; }
    public string ProductID { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public decimal SalePrice { get; set; }
    public int UnitsInStock { get; set; }
    public int UnitsOnOrder { get; set; }
    public string QuantityPerUnitDisplay { get; set; } = string.Empty;
    public decimal LineTotal { get; set; }
}
