using SKS_Blazor.Application.DTOs.Product;
using SKS_Blazor.Application.DTOs.Core;
using SKS_Blazor.Domain.Entities.Products;
using SKS_Blazor.Domain.Entities.Core;

namespace SKS_Blazor.Application.Services.Product;

/// <summary>
/// Port of <c>frmAddProductTo</c> — manages associating/disassociating products
/// with a customer or provider via junction tables (ProductsByCustomer / ProductsByProvider).
/// </summary>
public interface IAddProductToService
{
    /// <summary>
    /// Initializes the state: clears tracking dictionaries, loads existing product
    /// associations from the junction table into <see cref="AddProductToStateDto.ProductsStored"/>.
    /// Port of <c>LoadData()</c> + <c>LoadProductsById()</c>.
    /// </summary>
    Task<AddProductToStateDto> LoadDataAsync(AddProductToStateDto state, CancellationToken ct = default);

    /// <summary>
    /// Searches the Products table using optional ProductID / ProductName LIKE filters
    /// from <c>state.TxtCodeText</c> and <c>state.TxtNameText</c>.
    /// Port of <c>DoSearchProduct()</c>.
    /// </summary>
    Task<List<ProductsDto>> DoSearchProductAsync(AddProductToStateDto state, CancellationToken ct = default);

    /// <summary>
    /// Adds the product identified by <c>state.CurrentIdProduct</c> to the pending-add set
    /// (or removes it from pending-delete if it was previously stored).
    /// Port of <c>AddProductToSet()</c>.
    /// </summary>
    Task<AddProductToStateDto> AddProductToSetAsync(AddProductToStateDto state, CancellationToken ct = default);

    /// <summary>
    /// Removes checked products from the association list, tracking them in
    /// <c>ProductsToDelete</c> or removing them from <c>ProductsToAdd</c>.
    /// Port of <c>cmdRemove_Click()</c>.
    /// </summary>
    Task<AddProductToStateDto> RemoveAsync(AddProductToStateDto state, object eventSender, CancellationToken ct = default);

    /// <summary>
    /// Persists all pending additions and deletions to the junction table in a single transaction.
    /// Port of <c>cmdSave_Click()</c>.
    /// </summary>
    Task<AddProductToStateDto> SaveAsync(AddProductToStateDto state, object eventSender, CancellationToken ct = default);

    /// <summary>
    /// Loads a single product by <c>state.CurrentIdProduct</c> for insertion into the associated products list.
    /// Port of <c>LoadProductsById()</c>.
    /// </summary>
    Task<ProductsDto?> LoadProductsByIdAsync(AddProductToStateDto state, CancellationToken ct = default);
}
