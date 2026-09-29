using SKS_Blazor.Application.DTOs.Action;

namespace SKS_Blazor.Application.Services.Action;

/// <summary>Port of <c>frmActionOrderRequest</c> — view/approve/cancel an order request.</summary>
public interface IActionOrderRequestService
{
    /// <summary>
    /// Loads the order request header, detail lines, and computes totals.
    /// Port of <c>frmActionOrderRequest.LoadData</c> + <c>LoadDetails</c> + <c>DisplayTotals</c>.
    /// </summary>
    Task<ActionOrderRequestStateDto> LoadDataAsync(ActionOrderRequestStateDto state, CancellationToken ct = default);

    /// <summary>
    /// Approves the order request — validates current status, updates to APPROVED.
    /// Port of <c>frmActionOrderRequest.cmdApprove_Click</c>.
    /// </summary>
    Task ApproveAsync(ActionOrderRequestStateDto state, object eventSender, CancellationToken ct = default);

    /// <summary>
    /// Cancels the order request — validates current status, updates to CANCELLED.
    /// Port of <c>frmActionOrderRequest.cmdCancel_Click</c>.
    /// </summary>
    Task CancelAsync(ActionOrderRequestStateDto state, object eventSender, CancellationToken ct = default);

    /// <summary>
    /// Loads detail lines for a given order request and returns them as DTOs.
    /// Port of <c>frmActionOrderRequest.LoadData</c> (detail grid population).
    /// </summary>
    Task<List<OrderRequestDetailLineDto>> LoadDataAsync(int orderId, CancellationToken ct = default);
}
