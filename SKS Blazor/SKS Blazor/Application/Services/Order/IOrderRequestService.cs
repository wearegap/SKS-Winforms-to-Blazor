using SKS_Blazor.Application.DTOs.Core;
using SKS_Blazor.Application.DTOs.Order;
using SKS_Blazor.Domain.Entities.Products;

namespace SKS_Blazor.Application.Services.Order;

/// <summary>Port of <c>frmOrderRequest</c> business logic.</summary>
public interface IOrderRequestService
{
    /// <summary>
    /// Searches customers by ID, company name, contact first name, and contact last name.
    /// Port of <c>DoSearchCustomer</c>.
    /// </summary>
    Task<List<CustomersDto>> DoSearchCustomerAsync(OrderRequestStateDto state, string Id, CancellationToken ct = default);

    /// <summary>
    /// Validates and saves inline quantity edits on the product grid, recalculating line price and totals.
    /// Port of <c>SaveEdits</c>.
    /// </summary>
    Task<OrderRequestStateDto> SaveEditsAsync(OrderRequestStateDto state, CancellationToken ct = default);

    /// <summary>
    /// Loads products assigned to the current customer via ProductsByCustomer join table.
    /// Port of <c>LoadProductsById</c>.
    /// </summary>
    Task<List<ProductsDto>> LoadProductsByIdAsync(OrderRequestStateDto state, CancellationToken ct = default);

    /// <summary>
    /// Persists the order request header and all detail lines, updates product UnitsOnOrder.
    /// Port of <c>cmdSave_Click</c>.
    /// </summary>
    Task<OrderRequestStateDto> SaveAsync(OrderRequestStateDto state, object eventSender, CancellationToken ct = default);

    /// <summary>
    /// Recalculates totals when product grid quantities change.
    /// Port of <c>ReCalculateTotals</c> — called after quantity edits.
    /// </summary>
    Task<OrderRequestStateDto> ProductsAsync(OrderRequestStateDto state, object eventSender, CancellationToken ct = default);

    /// <summary>
    /// Recalculates totals when freight charge changes.
    /// Port of <c>txtFreightCharge_TextChanged</c>.
    /// </summary>
    Task<OrderRequestStateDto> FreightChargeAsync(OrderRequestStateDto state, object eventSender, CancellationToken ct = default);

    /// <summary>
    /// Recalculates totals when sales tax rate changes.
    /// Port of <c>txtSalesTax_TextChanged</c>.
    /// </summary>
    Task<OrderRequestStateDto> SalesTaxAsync(OrderRequestStateDto state, object eventSender, CancellationToken ct = default);
}
