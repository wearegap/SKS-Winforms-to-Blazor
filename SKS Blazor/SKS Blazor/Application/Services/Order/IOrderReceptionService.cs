using SKS_Blazor.Application.DTOs.Core;
using SKS_Blazor.Application.DTOs.Order;

namespace SKS_Blazor.Application.Services.Order;

/// <summary>
/// Service interface for order reception operations.
/// Port of <c>frmOrderReception</c> business logic.
/// </summary>
public interface IOrderReceptionService
{
    /// <summary>
    /// Search providers by ID and/or name filters from the state.
    /// Port of <c>DoSearchProvider</c>.
    /// </summary>
    Task<List<ProvidersDto>> DoSearchProviderAsync(OrderReceptionStateDto state, int Id, CancellationToken ct = default);

    /// <summary>
    /// Validate and commit inline grid edits — recalculates line price and totals.
    /// Port of <c>SaveEdits</c>.
    /// </summary>
    Task<OrderReceptionStateDto> SaveEditsAsync(OrderReceptionStateDto state, CancellationToken ct = default);

    /// <summary>
    /// Load products associated with the currently selected provider.
    /// Port of <c>LoadProductsById</c>.
    /// </summary>
    Task<List<ProductsDto>> LoadProductsByIdAsync(OrderReceptionStateDto state, CancellationToken ct = default);

    /// <summary>
    /// Persist a new order reception with all product lines to the database.
    /// Port of <c>cmdSave_Click</c>.
    /// </summary>
    Task<OrderReceptionStateDto> SaveAsync(OrderReceptionStateDto state, object eventSender, CancellationToken ct = default);

    /// <summary>
    /// Recalculate totals when product lines change.
    /// Port of <c>ReCalculateTotals</c> — called after edits, freight, or tax changes.
    /// </summary>
    Task<OrderReceptionStateDto> ProductsAsync(OrderReceptionStateDto state, object eventSender, CancellationToken ct = default);

    /// <summary>
    /// Handle freight charge change — recalculates totals.
    /// Port of <c>txtFreightCharge_TextChanged</c>.
    /// </summary>
    Task<OrderReceptionStateDto> FreightChargeAsync(OrderReceptionStateDto state, object eventSender, CancellationToken ct = default);

    /// <summary>
    /// Handle sales tax change — recalculates totals.
    /// Port of <c>txtSalesTax_TextChanged</c>.
    /// </summary>
    Task<OrderReceptionStateDto> SalesTaxAsync(OrderReceptionStateDto state, object eventSender, CancellationToken ct = default);

    /// <summary>
    /// Retrieve all order reception detail records.
    /// </summary>
    Task<List<OrderReceptionDetailsDto>> GetOrderReceptionDetailsAsync(CancellationToken ct = default);

    /// <summary>
    /// Retrieve all order reception header records.
    /// </summary>
    Task<List<OrderReceptionsDto>> GetOrderReceptionsAsync(CancellationToken ct = default);

    /// <summary>
    /// Retrieve all order request detail records.
    /// </summary>
    Task<List<OrderRequestDetailsDto>> GetOrderRequestDetailsAsync(CancellationToken ct = default);

    /// <summary>
    /// Retrieve all order request header records.
    /// </summary>
    Task<List<OrderRequestsDto>> GetOrderRequestsAsync(CancellationToken ct = default);
}
