using SKS_Blazor.Domain.Entities.Products;
namespace SKS_Blazor.Application.DTOs.Product;

/// <summary>
/// Carries the junction-table context parameters that the legacy frmAddProductTo
/// received as public fields (Table, ColumnName, Id, ObjectReferred).
/// Tells the service which junction table (ProductsByCustomer or ProductsByProvider)
/// to query and modify.
/// </summary>
public class AddProductToContextDto
{
    /// <summary>Junction table name: "ProductsByCustomer" or "ProductsByProvider".</summary>
    public string Table { get; set; } = string.Empty;

    /// <summary>FK column in the junction table: "CustomerID" or "ProviderID".</summary>
    public string ColumnName { get; set; } = string.Empty;

    /// <summary>The FK value (customer ID or provider ID).</summary>
    public int Id { get; set; }

    /// <summary>Display label for the related object, e.g. "Customer: Acme Corp".</summary>
    public string ObjectReferred { get; set; } = string.Empty;
}
