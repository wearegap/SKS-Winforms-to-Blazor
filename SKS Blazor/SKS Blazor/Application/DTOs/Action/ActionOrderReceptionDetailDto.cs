using SKS_Blazor.Application.DTOs.Action;
namespace SKS_Blazor.Application.DTOs.Action;

/// <summary>DTO representing a single detail line in the Action Order Reception grid (fgDetails).</summary>
public class ActionOrderReceptionDetailDto
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
