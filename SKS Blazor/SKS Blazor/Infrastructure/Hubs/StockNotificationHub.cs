using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using SKS_Blazor.Application.Services.Stock;
using SKS_Blazor.Application.Services.Adjust;

namespace SKS_Blazor.Infrastructure.Hubs;

/// <summary>
/// SignalR hub broadcasting real-time stock level changes and low-stock alerts
/// to connected Blazor Server clients. New abstraction — the legacy WinForms app
/// had no real-time push; this enables multi-user stock visibility.
/// </summary>
public class StockNotificationHub : Hub
{
    /// <summary>
    /// Broadcasts a stock quantity update for a specific product to all connected clients.
    /// Called by <c>AddStockManualService</c> and <c>AdjustStockManualService</c> after
    /// persisting stock changes.
    /// </summary>
    /// <param name="productId">The numeric product identifier (parsed from string ProductID).</param>
    /// <param name="newQuantity">The updated total stock quantity for the product.</param>
    public async Task SendStockUpdate(int productId, decimal newQuantity)
    {
        await Clients.All.SendAsync("ReceiveStockUpdate", productId, newQuantity);
    }

    /// <summary>
    /// Adds the calling connection to a warehouse-specific SignalR group so the client
    /// only receives stock updates relevant to its warehouse context.
    /// </summary>
    /// <param name="warehouseId">The warehouse identifier to join.</param>
    public async Task JoinWarehouseGroup(int warehouseId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"warehouse-{warehouseId}");
        await Clients.Caller.SendAsync("JoinedWarehouseGroup", warehouseId);
    }
}
