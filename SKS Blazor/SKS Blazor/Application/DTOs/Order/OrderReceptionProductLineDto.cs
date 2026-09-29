namespace SKS_Blazor.Application.DTOs.Order;

/// <summary>
/// DTO representing a single product line in an order reception grid.
/// Port of <c>fgProducts</c> grid row from frmOrderReception.
/// </summary>
public class OrderReceptionProductLineDto
{
    /// <summary>Grid column 0: Quantity ordered.</summary>
    public decimal Quantity { get; set; }

    /// <summary>Grid column 1: Product code (ProductID).</summary>
    public string ProductID { get; set; } = string.Empty;

    /// <summary>Grid column 2: Product name.</summary>
    public string ProductName { get; set; } = string.Empty;

    /// <summary>Grid column 3: Unit price.</summary>
    public decimal UnitPrice { get; set; }

    /// <summary>Grid column 4: Line price (Quantity × UnitPrice).</summary>
    public decimal Price { get; set; }

    /// <summary>Grid column 5: Units currently in stock.</summary>
    public int UnitsInStock { get; set; }

    /// <summary>Grid column 6: Units currently on order.</summary>
    public int UnitsOnOrder { get; set; }

    /// <summary>Grid column 7: Quantity per unit + unit display string.</summary>
    public string QuantityPerUnit { get; set; } = string.Empty;
}
