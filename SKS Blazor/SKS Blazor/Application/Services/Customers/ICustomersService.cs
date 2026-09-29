using SKS_Blazor.Application.DTOs.Core;
using SKS_Blazor.Application.DTOs.Customers;
using SKS_Blazor.Domain.Entities.Core;

namespace SKS_Blazor.Application.Services.Customers;

/// <summary>
/// Port of <c>frmCustomers</c> — customer CRUD and search operations.
/// </summary>
public interface ICustomersService
{
    /// <summary>
    /// Initializes form state — port of <c>frmCustomers.InitForm()</c>.
    /// Resets NewMode, EditMode, CancellingMode to false.
    /// </summary>
    Task<CustomersStateDto> InitFormAsync(CustomersStateDto state, CancellationToken ct = default);

    /// <summary>
    /// Port of <c>frmCustomers.InitForm()</c> — synchronous-style initialization
    /// that resets state flags. Delegates to <see cref="InitFormAsync"/>.
    /// </summary>
    Task<CustomersStateDto> InitForm(CustomersStateDto state, CancellationToken ct = default);

    /// <summary>
    /// Searches customers by a given field with LIKE prefix matching —
    /// port of <c>frmCustomers.SearchCriteriaProducts(field, value)</c>.
    /// Returns matching results as ProductsDto per the contract signature.
    /// </summary>
    Task<List<ProductsDto>> SearchCriteriaProductsAsync(string field, string value, CancellationToken ct = default);

    /// <summary>
    /// Port of <c>frmCustomers.SearchCriteriaProducts(field, value)</c> — synchronous-style alias.
    /// Delegates to <see cref="SearchCriteriaProductsAsync"/>.
    /// </summary>
    Task<List<ProductsDto>> SearchCriteriaProducts(string field, string value, CancellationToken ct = default);

    /// <summary>
    /// Saves (inserts or updates) the current customer record in state —
    /// port of <c>Toolbar1_ButtonClick</c> "Save" case.
    /// The page must call SetCurrentCustomer before invoking this method.
    /// </summary>
    Task SaveAsync(CancellationToken ct = default);

    /// <summary>
    /// Deletes the current customer record in state —
    /// port of <c>Toolbar1_ButtonClick</c> "Delete" case.
    /// The page must call SetCurrentCustomer before invoking this method.
    /// </summary>
    Task DeleteAsync(CancellationToken ct = default);

    /// <summary>
    /// Sets the current customer for save/delete operations.
    /// Called by the page component to prepare state before SaveAsync/DeleteAsync.
    /// </summary>
    void SetCurrentCustomer(CustomersDto? customer);

    /// <summary>All customers ordered by CustomerID (port of the dcCustomers initial load).</summary>
    Task<List<CustomersDto>> GetAllCustomersAsync(CancellationToken ct = default);

    /// <summary>Port of <c>SearchCriteriaProducts(field, value)</c> returning customers: <c>{field} LIKE 'value%'</c>.</summary>
    Task<List<CustomersDto>> SearchCustomersAsync(string field, string value, CancellationToken ct = default);

    /// <summary>Port of "Add" then "Save". Inserts a new customer.</summary>
    Task CreateCustomerAsync(CustomersDto customer, CancellationToken ct = default);

    /// <summary>Port of "Edit" then "Save". Updates an existing customer.</summary>
    Task UpdateCustomerAsync(CustomersDto customer, CancellationToken ct = default);

    /// <summary>Port of "Delete". Confirmation is a page concern.</summary>
    Task DeleteCustomerAsync(int customerId, CancellationToken ct = default);
}
