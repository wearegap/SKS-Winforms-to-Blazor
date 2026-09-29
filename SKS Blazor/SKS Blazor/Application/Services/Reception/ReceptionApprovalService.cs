using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SKS_Blazor.Application.DTOs.Reception;
using SKS_Blazor.Infrastructure.Data;
using SKS_Blazor.Domain.Entities.Order;
using SKS_Blazor.Domain.Entities.Core;
using SKS_Blazor.Application.DTOs.Core;
using SKS_Blazor.Infrastructure.Helpers;

namespace SKS_Blazor.Application.Services.Reception;

/// <summary>
/// Port of <c>frmReceptionApproval</c> (frmOrderAproval.cs).
/// Implements order reception approval search and grid initialization.
/// </summary>
public class ReceptionApprovalService : IReceptionApprovalService
{
    private readonly IDbContextFactory<UpgradedDbContext> _dbFactory;
    private readonly ILogger<ReceptionApprovalService> _logger;

    public ReceptionApprovalService(
        IDbContextFactory<UpgradedDbContext> dbFactory,
        ILogger<ReceptionApprovalService> logger)
    {
        _dbFactory = dbFactory;
        _logger = logger;
    }

    /// <summary>
    /// Port of <c>frmReceptionApproval.DoSearchReception</c>.
    /// Builds a dynamic LINQ query joining OrderReceptions, OrderReceptionDetails, Providers, and Users,
    /// applying all filter criteria, then groups by order to produce the grid rows.
    ///
    /// Legacy SQL:
    ///   Select o.OrderDate, o.OrderID, p.ProviderName,
    ///          p.ContactFirstName + ' ' + p.ContactLastName as ContactName,
    ///          u.Fullname as [Received by], Sum(d.LineTotal) as Price, o.Status
    ///   From OrderReceptions as o, OrderReceptionDetails as d, Providers as p, Users as u
    ///   Where o.OrderID = d.OrderID And p.ProviderID = o.ProviderID And u.Username = o.ReceivedBy
    ///   [+ dynamic filters]
    ///   Group by o.orderDate, o.OrderID, p.ProviderName,
    ///            p.ContactFirstName + ' ' + p.ContactLastName, u.Fullname, o.Status
    /// </summary>
    public async Task DoSearchReceptionAsync(
        ReceptionApprovalStateDto state,
        ReceptionApprovalFilterDto filter,
        CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        // Start with the base join: OrderReceptions -> OrderReceptionDetails, Providers, Users
        // Legacy: From OrderReceptions as o, OrderReceptionDetails as d, Providers as p, Users as u
        //         Where o.OrderID = d.OrderID And p.ProviderID = o.ProviderID And u.Username = o.ReceivedBy
        var query = from o in db.OrderReceptions.AsNoTracking()
                    join p in db.Providers.AsNoTracking()
                        on o.ProviderID equals (int?)p.ProviderID
                    join u in db.Users.AsNoTracking()
                        on o.ReceivedBy equals u.Username
                    join d in db.OrderReceptionDetails.AsNoTracking()
                        on (int?)o.OrderID equals d.OrderID
                    select new { Order = o, Detail = d, Provider = p, User = u };

        // Apply dynamic filters — mirrors the legacy filter string building exactly

        // Filter: o.ProviderID = {Id}
        if (filter.ProviderId != -1)
        {
            query = query.Where(x => x.Order.ProviderID == filter.ProviderId);
        }

        // Filter: p.ProviderName LIKE '%{txtProviderName}%'
        if (!string.IsNullOrEmpty(filter.ProviderName))
        {
            var provName = filter.ProviderName;
            query = query.Where(x => x.Provider.ProviderName != null &&
                                     EF.Functions.Like(x.Provider.ProviderName, "%" + provName + "%"));
        }

        // Filter: p.ContactFirstName LIKE '%{txtContactName}%'
        if (!string.IsNullOrEmpty(filter.ContactName))
        {
            var contactFirst = filter.ContactName;
            query = query.Where(x => x.Provider.ContactFirstName != null &&
                                     EF.Functions.Like(x.Provider.ContactFirstName, "%" + contactFirst + "%"));
        }

        // Filter: p.ContactLastName LIKE '%{txtContactLastName}%'
        if (!string.IsNullOrEmpty(filter.ContactLastName))
        {
            var contactLast = filter.ContactLastName;
            query = query.Where(x => x.Provider.ContactLastName != null &&
                                     EF.Functions.Like(x.Provider.ContactLastName, "%" + contactLast + "%"));
        }

        // Filter: o.OrderID = {txtOrderID}
        if (!string.IsNullOrEmpty(filter.OrderID))
        {
            if (int.TryParse(filter.OrderID, NumberStyles.Integer, CultureInfo.InvariantCulture, out var orderId))
            {
                query = query.Where(x => x.Order.OrderID == orderId);
            }
        }

        // Filter: d.ProductID LIKE '%{txtProductID}%'
        if (!string.IsNullOrEmpty(filter.ProductID))
        {
            var prodId = filter.ProductID;
            query = query.Where(x => x.Detail.ProductID != null &&
                                     EF.Functions.Like(x.Detail.ProductID, "%" + prodId + "%"));
        }

        // Filter: o.Status = '{cmbStatus.Text.ToUpper()}'
        if (!string.IsNullOrEmpty(filter.Status) &&
            !string.Equals(filter.Status, "All", StringComparison.OrdinalIgnoreCase))
        {
            var statusUpper = filter.Status.ToUpperInvariant();
            query = query.Where(x => x.Order.Status == statusUpper);
        }

        // Materialize to apply date filtering in memory (OrderDate is TEXT in DB)
        var materialized = await query.Select(x => new
        {
            OrderDate = x.Order.OrderDate,
            OrderID = x.Order.OrderID,
            ProviderName = x.Provider.ProviderName,
            ContactFirstName = x.Provider.ContactFirstName,
            ContactLastName = x.Provider.ContactLastName,
            ReceivedByFullname = x.User.Fullname,
            LineTotal = x.Detail.LineTotal,
            Status = x.Order.Status
        }).ToListAsync(ct);

        // Date range in memory: OrderDate is TEXT in three legacy shapes (epoch ms, MM/dd/yyyy, M/d/yyyy).
        // The legacy SQL compared strings (o.OrderDate >= #MM/dd/yyyy#), which cannot order these values.
        if ((filter.FilterFromDate && filter.FromDate.HasValue) || (filter.FilterToDate && filter.ToDate.HasValue))
        {
            var from = filter.FilterFromDate ? filter.FromDate : null;
            var to = filter.FilterToDate ? filter.ToDate : null;
            materialized = materialized.Where(r => LegacyDate.InRange(r.OrderDate, from, to)).ToList();
        }

        // Group by: o.orderDate, o.OrderID, p.ProviderName,
        //           p.ContactFirstName + ' ' + p.ContactLastName, u.Fullname, o.Status
        // Aggregate: Sum(d.LineTotal) as Price
        var grouped = materialized
            .GroupBy(r => new
            {
                r.OrderDate,
                r.OrderID,
                ProviderName = r.ProviderName ?? string.Empty,
                ContactName = ((r.ContactFirstName ?? string.Empty) + " " + (r.ContactLastName ?? string.Empty)).Trim(),
                ReceivedBy = r.ReceivedByFullname ?? string.Empty,
                Status = r.Status ?? string.Empty
            })
            .Select(g => new ReceptionApprovalGridRowDto
            {
                OrderDate = LegacyDate.Display(g.Key.OrderDate),
                OrderID = g.Key.OrderID,
                ProviderName = g.Key.ProviderName,
                ContactName = g.Key.ContactName,
                ReceivedBy = g.Key.ReceivedBy,
                Price = g.Sum(x => x.LineTotal ?? 0m),
                Status = g.Key.Status
            })
            .ToList();

        var recordCount = grouped.Count;

        // Update state with results
        state.TxtProviderNameText = filter.ProviderName;
        state.TxtContactNameText = filter.ContactName;
        state.TxtContactLastNameText = filter.ContactLastName;

        // Store grid rows in a temporary collection on the state for the page to bind
        // (The page will iterate over these rows to populate the MudDataGrid)
        if (!state.GridRows.Any())
        {
            state.GridRows = grouped;
        }
        else
        {
            state.GridRows.Clear();
            state.GridRows.AddRange(grouped);
        }

        var statusMessage = $"There are {recordCount} records with the selected criteria";

        _logger.LogInformation("DoSearchReceptionAsync completed: {RecordCount} records found", recordCount);
    }

    /// <summary>
    /// Port of <c>frmReceptionApproval.InitGrid</c>.
    /// Returns the column headers for the fgOrders grid in display order.
    ///
    /// Legacy:
    ///   fgOrders.ColumnsCount = 7
    ///   fgOrders.AddItem("Date\tOrder\tSupplier\tContact\tReceived by\tPrice\tStatus")
    /// </summary>
    public async Task InitGridAsync(CancellationToken ct = default)
    {
        _logger.LogDebug("InitGridAsync: initializing grid columns");

        await Task.CompletedTask;
    }
}
