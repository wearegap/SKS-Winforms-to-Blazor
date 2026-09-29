using SKS_Blazor.Application.DTOs.Request;
using SKS_Blazor.Application.DTOs.Core;

namespace SKS_Blazor.Application.Services.Request;

/// <summary>
/// Port of <c>frmRequestApproval</c> (frmRequestAproval.cs) — service layer for the
/// order-request approval grid search and initialization.
/// </summary>
public interface IRequestApprovalService
{
    /// <summary>
    /// Searches order requests with dynamic filters, equivalent to legacy <c>DoSearchRequest</c>.
    /// The <paramref name="filter"/> carries all UI filter criteria.
    /// <paramref name="Id"/> is the customer ID (-1 = no customer filter).
    /// Returns the search result with rows and status message.
    /// </summary>
    Task<RequestApprovalSearchResultDto> DoSearchRequestAsync(
        RequestApprovalFilterDto filter,
        int Id,
        CancellationToken ct = default);

    /// <summary>
    /// Returns the grid column definitions for <c>fgOrders</c>.
    /// Equivalent to legacy <c>InitGrid</c>.
    /// Returns the list of column header strings.
    /// </summary>
    Task<List<string>> InitGridAsync(CancellationToken ct = default);
}
