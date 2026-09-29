using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SKS_Blazor.Domain.Entities.Stock;
using SKS_Blazor.Domain.Entities.Manual;
using SKS_Blazor.Domain.Entities.Order;

namespace SKS_Blazor.Infrastructure.Logging;

/// <summary>
/// Centralized audit trail logger recording user actions across stock, order,
/// and invoice operations. Port of the implicit audit pattern in the legacy app
/// where <c>modMain.LogStatus</c> wrote status messages and various forms recorded
/// the <c>User</c> field on StockLog, ManualStocks, and order tables.
/// </summary>
public class AuditLogger
{
    private readonly ILogger<AuditLogger> _logger;

    public AuditLogger(ILogger<AuditLogger> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Logs an auditable user action using structured Serilog logging.
    /// In the legacy app, audit information was scattered across:
    /// - StockLog.User + StockLog.Date + StockLog.DocType
    /// - ManualStocks.User + ManualStocks.Date + ManualStocks.Action
    /// - OrderReceptions.ReceivedBy / ChangedBy
    /// - OrderRequests.EmployeeID / ChangedBy
    /// This centralizes the pattern into a single structured log entry
    /// that can be forwarded to any Serilog sink (file, database, Seq, etc.).
    /// </summary>
    /// <param name="userId">The username performing the action (port of modMain.UserId).</param>
    /// <param name="entity">The entity type being acted upon (e.g., "Stock", "OrderReception", "Product").</param>
    /// <param name="action">The action performed (e.g., "ADD", "MOD", "APPROVED", "CANCELLED", "DELETE").</param>
    /// <param name="details">Free-text details about the action (e.g., IDs, quantities, reason).</param>
    public Task LogActionAsync(string userId, string entity, string action, string details)
    {
        _logger.LogInformation(
            "AUDIT: User={UserId} Entity={Entity} Action={Action} Details={Details} Timestamp={Timestamp}",
            userId,
            entity,
            action,
            details,
            DateTime.UtcNow);

        return Task.CompletedTask;
    }
}
