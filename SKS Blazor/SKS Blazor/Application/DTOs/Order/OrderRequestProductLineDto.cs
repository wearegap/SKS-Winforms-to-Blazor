using SKS_Blazor.Application.DTOs.Order;
namespace SKS_Blazor.Application.DTOs.Order;

/// <summary>Represents a single product line in the Order Request grid.
/// Port of the fgProducts FlexGrid row structure in <c>frmOrderRequest</c>.
/// Columns: Quantity (editable), Code, Product, UnitPrice, Price, Existence, Ordered, Quantity per unit.</summary>
public class OrderRequestProductLineDto
{
    /// <summary>Editable quantity — column 0 in legacy grid. Initialized to 0.</summary>
    public decimal Quantity { get; set; }

    /// <summary>Product code — column 1 (ProductID from DB).</summary>
    public string ProductCode { get; set; } = string.Empty;

    /// <summary>Product name — column 2 (ProductName from DB).</summary>
    public string ProductName { get; set; } = string.Empty;

    /// <summary>Unit price — column 3 (UnitPrice from DB).</summary>
    public decimal UnitPrice { get; set; }

    /// <summary>Calculated line price = Quantity * UnitPrice — column 4. Initialized to 0.</summary>
    public decimal LinePrice { get; set; }

    /// <summary>Units in stock — column 5 (UnitsInStock from DB).</summary>
    public int UnitsInStock { get; set; }

    /// <summary>Units on order — column 6 (UnitsOnOrder from DB).</summary>
    public int UnitsOnOrder { get; set; }

    /// <summary>Quantity per unit + unit — column 7 (QuantityPerUnit + Unit from DB).</summary>
    public string QuantityPerUnit { get; set; } = string.Empty;
}
