using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SKS_Blazor.Application.DTOs.Request;
using SKS_Blazor.Domain.Entities.Order;
using SKS_Blazor.Domain.Entities.Core;
using SKS_Blazor.Infrastructure.Data;
using SKS_Blazor.Application.DTOs.Core;
using SKS_Blazor.Infrastructure.Helpers;

namespace SKS_Blazor.Application.Services.Request;

/// <summary>
/// Port of <c>frmRequestApproval</c> (frmRequestAproval.cs).
/// Implements search and grid initialization for order request approval workflow.
/// </summary>
public class RequestApprovalService : IRequestApprovalService
{
    private readonly IDbContextFactory<UpgradedDbContext> _dbFactory;
    private readonly ILogger<RequestApprovalService> _logger;

    public RequestApprovalService(
        IDbContextFactory<UpgradedDbContext> dbFactory,
        ILogger<RequestApprovalService> logger)
    {
        _dbFactory = dbFactory;
        _logger = logger;
    }

    /// <summary>
    /// Port of legacy <c>DoSearchRequest(int Id)</c>.
    /// Builds a dynamic LINQ query joining OrderRequests, OrderRequestDetails, Customers, Users
    /// with filters matching the legacy SQL exactly:
    ///   - CustomerID (when Id != -1)
    ///   - CompanyName LIKE
    ///   - ContactFirstName LIKE
    ///   - ContactLastName LIKE
    ///   - OrderID exact match
    ///   - ProductID LIKE (on details)
    ///   - OrderDate range (from/to)
    ///   - Status (when not "All")
    /// Groups by order and sums LineTotal for the Price column.
    /// </summary>
    public async Task<RequestApprovalSearchResultDto> DoSearchRequestAsync(
        RequestApprovalFilterDto filter,
        int Id,
        CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        // Legacy SQL:
        //   Select o.OrderDate, o.OrderID, c.CompanyName,
        //          c.ContactFirstName + ' ' + c.ContactLastName as ContactName,
        //          u.Fullname as [Received by], Sum(d.LineTotal) as Price, o.Status
        //   From OrderRequests as o, OrderRequestDetails as d, Customers as c, Users as u
        //   Where o.OrderID = d.OrderID And c.CustomerID = o.CustomerID And u.Username = o.EmployeeId
        //   [+ dynamic filters]
        //   Group by o.orderDate, o.OrderID, c.CompanyName,
        //            c.ContactFirstName + ' ' + c.ContactLastName, u.Fullname, o.Status

        // Start with the cross-join equivalent: inner joins matching legacy WHERE clause
        var query = from o in db.OrderRequests.AsNoTracking()
                    join d in db.OrderRequestDetails.AsNoTracking()
                        on o.OrderID equals (d.OrderID ?? 0)
                    join c in db.Customers.AsNoTracking()
                        on o.CustomerID equals (int?)c.CustomerID
                    join u in db.Users.AsNoTracking()
                        on o.EmployeeID equals u.Username
                    select new { o, d, c, u };

        // Filter: CustomerID
        if (Id != -1)
        {
            query = query.Where(x => x.o.CustomerID == Id);
        }

        // Filter: CompanyName LIKE '%...%'
        if (!string.IsNullOrEmpty(filter.CompanyNameText))
        {
            var companyFilter = filter.CompanyNameText;
            query = query.Where(x => x.c.CompanyName != null &&
                                     EF.Functions.Like(x.c.CompanyName, "%" + companyFilter + "%"));
        }

        // Filter: ContactFirstName LIKE '%...%'
        if (!string.IsNullOrEmpty(filter.ContactNameText))
        {
            var contactFilter = filter.ContactNameText;
            query = query.Where(x => x.c.ContactFirstName != null &&
                                     EF.Functions.Like(x.c.ContactFirstName, "%" + contactFilter + "%"));
        }

        // Filter: ContactLastName LIKE '%...%'
        if (!string.IsNullOrEmpty(filter.ContactLastNameText))
        {
            var lastNameFilter = filter.ContactLastNameText;
            query = query.Where(x => x.c.ContactLastName != null &&
                                     EF.Functions.Like(x.c.ContactLastName, "%" + lastNameFilter + "%"));
        }

        // Filter: OrderID exact match
        if (!string.IsNullOrEmpty(filter.OrderIDText))
        {
            if (int.TryParse(filter.OrderIDText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var orderId))
            {
                query = query.Where(x => x.o.OrderID == orderId);
            }
        }

        // Filter: ProductID LIKE '%...%' (on detail rows)
        if (!string.IsNullOrEmpty(filter.ProductIDText))
        {
            var productFilter = filter.ProductIDText;
            query = query.Where(x => x.d.ProductID != null &&
                                     EF.Functions.Like(x.d.ProductID, "%" + productFilter + "%"));
        }

        // Filter: Status (when not "All" and a selection is made)
        if (!string.IsNullOrEmpty(filter.StatusFilter) && filter.StatusFilter != "All")
        {
            var statusUpper = filter.StatusFilter.ToUpperInvariant();
            query = query.Where(x => x.o.Status != null &&
                                     x.o.Status.ToUpper() == statusUpper);
        }

        // Materialize the joined rows first, then group in memory.
        // This avoids EF Core translation issues with string concatenation in GroupBy.
        var rawRows = await query.Select(x => new
        {
            OrderDate = x.o.OrderDate,
            OrderID = x.o.OrderID,
            CompanyName = x.c.CompanyName,
            ContactFirstName = x.c.ContactFirstName,
            ContactLastName = x.c.ContactLastName,
            Fullname = x.u.Fullname,
            LineTotal = x.d.LineTotal,
            Status = x.o.Status
        }).ToListAsync(ct);

        // Date range in memory: OrderDate is TEXT in three legacy shapes (epoch ms, MM/dd/yyyy, M/d/yyyy).
        // The legacy SQL compared strings (o.OrderDate >= 'MM/dd/yyyy'), which cannot order these values.
        if ((filter.FromDateEnabled && filter.FromDate.HasValue) || (filter.ToDateEnabled && filter.ToDate.HasValue))
        {
            var from = filter.FromDateEnabled ? filter.FromDate : null;
            var to = filter.ToDateEnabled ? filter.ToDate : null;
            rawRows = rawRows.Where(r => LegacyDate.InRange(r.OrderDate, from, to)).ToList();
        }

        // Group by the same columns as the legacy SQL
        var grouped = rawRows
            .GroupBy(r => new
            {
                r.OrderDate,
                r.OrderID,
                r.CompanyName,
                ContactName = (r.ContactFirstName ?? string.Empty) + " " + (r.ContactLastName ?? string.Empty),
                ReceivedBy = r.Fullname ?? string.Empty,
                Status = r.Status ?? string.Empty
            })
            .Select(g => new RequestApprovalGridRowDto
            {
                OrderDate = LegacyDate.Display(g.Key.OrderDate),
                OrderID = g.Key.OrderID,
                CompanyName = g.Key.CompanyName ?? string.Empty,
                ContactName = g.Key.ContactName.Trim(),
                ReceivedBy = g.Key.ReceivedBy,
                Price = g.Sum(x => x.LineTotal ?? 0m),
                Status = g.Key.Status
            })
            .ToList();

        var recordCount = grouped.Count;
        var statusMessage = $"There are {recordCount} records with the selected criteria";

        _logger.LogInformation("DoSearchRequestAsync completed: {RecordCount} records found", recordCount);

        return new RequestApprovalSearchResultDto
        {
            Rows = grouped,
            StatusMessage = statusMessage,
            RecordCount = recordCount
        };
    }

    /// <summary>
    /// Port of legacy <c>InitGrid()</c>.
    /// Returns the column headers for the fgOrders grid:
    /// Date, Order, Customer, Contact, Received by, Price, Status.
    /// </summary>
    public async Task<List<string>> InitGridAsync(CancellationToken ct = default)
    {
        // Legacy code:
        //   fgOrders.ColumnsCount = 7;
        //   fgOrders.AddItem("Date\tOrder\tCustomer\tContact\tReceived by\tPrice\tStatus");
        var columns = new List<string>
        {
            "Date",
            "Order",
            "Customer",
            "Contact",
            "Received by",
            "Price",
            "Status"
        };

        _logger.LogDebug("InitGridAsync: returning {ColumnCount} column definitions", columns.Count);

        return await Task.FromResult(columns);
    }
}
