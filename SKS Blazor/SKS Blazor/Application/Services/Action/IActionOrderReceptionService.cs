using SKS_Blazor.Application.DTOs.Action;

namespace SKS_Blazor.Application.Services.Action;

/// <summary>Port of <c>frmActionOrderReception</c> — order reception approval/cancellation workflow.</summary>
public interface IActionOrderReceptionService
{
    /// <summary>
    /// Loads order reception header, details, and computes totals.
    /// Port of <c>frmActionOrderReception.LoadData</c> + <c>LoadDetails</c> + <c>DisplayTotals</c>.
    /// </summary>
    Task<ActionOrderReceptionStateDto> LoadDataAsync(ActionOrderReceptionStateDto state, CancellationToken ct = default);

    /// <summary>
    /// Approves the order reception: updates status, inserts stock records, updates product quantities.
    /// Port of <c>frmActionOrderReception.cmdApprove_Click</c>.
    /// </summary>
    Task ApproveAsync(ActionOrderReceptionStateDto state, object eventSender, CancellationToken ct = default);

    /// <summary>
    /// Cancels the order reception: updates status to CANCELLED.
    /// Port of <c>frmActionOrderReception.cmdCancel_Click</c>.
    /// </summary>
    Task CancelAsync(ActionOrderReceptionStateDto state, object eventSender, CancellationToken ct = default);
}
