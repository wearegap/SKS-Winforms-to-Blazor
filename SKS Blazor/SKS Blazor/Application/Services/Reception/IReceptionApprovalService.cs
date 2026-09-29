using SKS_Blazor.Application.DTOs.Reception;

namespace SKS_Blazor.Application.Services.Reception;

/// <summary>
/// Port of <c>frmReceptionApproval</c> (frmOrderAproval.cs) — service contract for
/// order reception approval search and grid initialization.
/// </summary>
public interface IReceptionApprovalService
{
    /// <summary>
    /// Searches order receptions with dynamic filtering criteria, returning grouped grid rows.
    /// Port of <c>frmReceptionApproval.DoSearchReception</c>.
    /// </summary>
    /// <param name="state">Current page state to update with filter results.</param>
    /// <param name="filter">All filter criteria from the page.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Completed task.</returns>
    Task DoSearchReceptionAsync(ReceptionApprovalStateDto state, ReceptionApprovalFilterDto filter, CancellationToken ct = default);

    /// <summary>
    /// Returns the grid column definitions for the fgOrders grid.
    /// Port of <c>frmReceptionApproval.InitGrid</c>.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Completed task.</returns>
    Task InitGridAsync(CancellationToken ct = default);
}
