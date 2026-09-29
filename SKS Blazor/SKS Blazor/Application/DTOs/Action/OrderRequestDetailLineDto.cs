namespace SKS_Blazor.Application.DTOs.Action;

/// <summary>Single detail line shown in the ActionOrderRequest grid. Port of <c>frmActionOrderRequest.fgProducts</c> row.</summary>
public class OrderRequestDetailLineDto
{
    public int OrderDetailID { get; set; }
    public decimal? Quantity { get; set; }
    public string ProductID { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public decimal? UnitPrice { get; set; }
    public decimal? SalePrice { get; set; }
    public int? UnitsInStock { get; set; }
    public int? UnitsOnOrder { get; set; }
    public string QuantityPerUnitDisplay { get; set; } = string.Empty;
    public decimal? LineTotal { get; set; }
}
