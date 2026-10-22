using SKS_Blazor.Application.DTOs.Core;
using SKS_Blazor.Application.DTOs.Products;
using SKS_Blazor.Domain.Entities.Core;
using SKS_Blazor.Domain.Entities.Products;

namespace SKS_Blazor.Application.Services.Products;

/// <summary>
/// Service contract for the Products management screen.
/// Port of <c>frmProducts</c>.
/// </summary>
public interface IProductsService
{
    /// <summary>
    /// Handles category selection change logic. When the category combo index changes,
    /// resolves the CategoryID text; when CategoryID text changes, resolves the combo index.
    /// Port of <c>cmbCategory_SelectedIndexChanged</c> and <c>txtCategory_TextChanged</c>.
    /// </summary>
    Task<ProductsStateDto> CategoryAsync(ProductsStateDto state, object eventSender, CancellationToken ct = default);

    /// <summary>
    /// Returns all ProductsByCustomer junction records with product details.
    /// </summary>
    Task<List<ProductsByCustomerDto>> GetProductsByCustomerAsync(CancellationToken ct = default);

    /// <summary>
    /// Returns all ProductsByProvider junction records with provider details.
    /// </summary>
    Task<List<ProductsByProviderDto>> GetProductsByProviderAsync(CancellationToken ct = default);

    /// <summary>
    /// Loads all categories for the category combo box.
    /// Port of <c>modFunctions.LoadCombo("Categories", ...)</c>.
    /// </summary>
    Task<List<CategoriesDto>> LoadCategoriesAsync(CancellationToken ct = default);

    /// <summary>
    /// Loads all products from the database for grid display.
    /// Port of <c>dcProducts.Recordset</c> data binding on Form_Load.
    /// </summary>
    Task<List<ProductsDto>> LoadProductsAsync(CancellationToken ct = default);

    /// <summary>
    /// Loads a single product by its ProductID.
    /// </summary>
    Task<ProductsDto?> GetProductByIdAsync(string productId, CancellationToken ct = default);

    /// <summary>
    /// Persists a product record (insert or update) to the database using a DTO.
    /// Overload with explicit product data.
    /// </summary>
    Task SaveProductAsync(ProductSaveDto dto, CancellationToken ct = default);

    /// <summary>
    /// Deletes a product record by ProductID using explicit id parameter.
    /// Overload with explicit product id.
    /// </summary>
    Task DeleteProductAsync(string productId, CancellationToken ct = default);
}
