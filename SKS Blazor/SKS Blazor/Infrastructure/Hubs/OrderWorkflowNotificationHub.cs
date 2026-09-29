using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using SKS_Blazor.Application.Services.Action;

namespace SKS_Blazor.Infrastructure.Hubs;

/// <summary>
/// SignalR hub notifying users of order request/reception approval status changes.
/// New abstraction — replaces the implicit workflow in <c>frmReceptionApproval</c>
/// and <c>frmRequestApproval</c> where status changes were only visible after manual refresh.
/// </summary>
public class OrderWorkflowNotificationHub : Hub
{
    /// <summary>
    /// Broadcasts an approval notification to all connected clients.
    /// Called after <c>ActionOrderReceptionService.ApproveAsync</c> or
    /// <c>ActionOrderRequestService.ApproveAsync</c> succeeds.
    /// </summary>
    /// <param name="orderId">The order identifier that was approved.</param>
    /// <param name="status">The new status string (e.g., "APPROVED").</param>
    public async Task NotifyApproval(int orderId, string status)
    {
        await Clients.All.SendAsync("OrderApproved", orderId, status);
    }

    /// <summary>
    /// Broadcasts a rejection/cancellation notification to all connected clients.
    /// Called after <c>ActionOrderReceptionService.CancelAsync</c> or
    /// <c>ActionOrderRequestService.CancelAsync</c> succeeds.
    /// </summary>
    /// <param name="orderId">The order identifier that was cancelled.</param>
    /// <param name="reason">The reason or status string (e.g., "CANCELLED").</param>
    public async Task NotifyRejection(int orderId, string reason)
    {
        await Clients.All.SendAsync("OrderRejected", orderId, reason);
    }
}
