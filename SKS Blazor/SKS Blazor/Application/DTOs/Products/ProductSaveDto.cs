using SKS_Blazor.Domain.Entities.Core;
namespace SKS_Blazor.Application.DTOs.Products;

/// <summary>
/// DTO used for saving (insert or update) a product record.
/// Port of <c>frmProducts</c> Toolbar1_ButtonClick "Save" / "Add" cases.
/// </summary>
public class ProductSaveDto
{
    public string ProductID { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string ProductDescription { get; set; } = string.Empty;
    public int? CategoryID { get; set; }
    public decimal? UnitPrice { get; set; }
    public decimal? QuantityPerUnit { get; set; }
    public int? UnitsInStock { get; set; }
    public int? UnitsOnOrder { get; set; }
    public int Discontinued { get; set; }
    public string LeadTime { get; set; } = string.Empty;
    public string SerialNumber { get; set; } = string.Empty;
    public int? SupplierID { get; set; }
    public string Unit { get; set; } = string.Empty;
    public int? ReorderLevel { get; set; }
    public bool IsNew { get; set; }
}
