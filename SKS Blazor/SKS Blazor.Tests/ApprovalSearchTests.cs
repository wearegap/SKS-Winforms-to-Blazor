using Microsoft.Extensions.Logging.Abstractions;
using SKS_Blazor.Application.DTOs.Reception;
using SKS_Blazor.Application.DTOs.Request;
using SKS_Blazor.Application.Services.Reception;
using SKS_Blazor.Application.Services.Request;
using Xunit;

namespace SKS_Blazor.Tests;

/// <summary>
/// The approval grids filter TEXT dates stored in three shapes. Expected order ids come from a
/// read-only probe of Orders.db with the same joins the services use.
/// </summary>
public class ApprovalSearchTests : IDisposable
{
    private readonly SqliteDbFixture _db = new();

    public void Dispose() => _db.Dispose();

    private RequestApprovalService RequestService() => new(_db, NullLogger<RequestApprovalService>.Instance);
    private ReceptionApprovalService ReceptionService() => new(_db, NullLogger<ReceptionApprovalService>.Instance);

    [Fact]
    public async Task RequestSearch_Feb2013_MatchesEpochMillisecondRows()
    {
        var filter = new RequestApprovalFilterDto
        {
            FromDateEnabled = true, FromDate = new DateTime(2013, 2, 1),
            ToDateEnabled = true, ToDate = new DateTime(2013, 2, 28)
        };

        var result = await RequestService().DoSearchRequestAsync(filter, -1);

        Assert.Equal(new[] { 11, 12, 13, 14, 15, 16, 17, 18, 19, 21 }, result.Rows.Select(r => r.OrderID).OrderBy(x => x));
        Assert.All(result.Rows, r => Assert.Matches(@"^02/\d\d/2013$", r.OrderDate)); // displayed normalised
        Assert.Contains(result.Rows, r => r.OrderID == 14 && r.Price == 21617m);     // Sum(LineTotal) over REAL
        Assert.Equal(10, result.RecordCount);
    }

    [Fact]
    public async Task RequestSearch_Feb2020_MatchesMonthDayYearRows()
    {
        var filter = new RequestApprovalFilterDto
        {
            FromDateEnabled = true, FromDate = new DateTime(2020, 2, 1),
            ToDateEnabled = true, ToDate = new DateTime(2020, 2, 29)
        };

        var result = await RequestService().DoSearchRequestAsync(filter, -1);

        Assert.Equal(new[] { 44, 45, 46 }, result.Rows.Select(r => r.OrderID).OrderBy(x => x));
    }

    [Fact]
    public async Task RequestSearch_NoFilters_ReturnsAllJoinedOrders_WithNamesFilled()
    {
        var result = await RequestService().DoSearchRequestAsync(new RequestApprovalFilterDto(), -1);

        Assert.Equal(17, result.RecordCount);
        Assert.All(result.Rows, r =>
        {
            Assert.False(string.IsNullOrWhiteSpace(r.CompanyName));
            Assert.Equal("Allan Cantillo", r.ReceivedBy);
        });
    }

    [Fact]
    public async Task RequestSearch_StatusAndCaseInsensitiveProduct()
    {
        var filter = new RequestApprovalFilterDto { StatusFilter = "Cancelled", ProductIDText = "ik" };

        var result = await RequestService().DoSearchRequestAsync(filter, -1);

        Assert.NotEmpty(result.Rows);
        Assert.All(result.Rows, r => Assert.Equal("CANCELLED", r.Status));
    }

    [Fact]
    public async Task ReceptionSearch_Feb2020_MatchesShortMonthDayYearRow()
    {
        var state = new ReceptionApprovalStateDto();
        var filter = new ReceptionApprovalFilterDto
        {
            FilterFromDate = true, FromDate = new DateTime(2020, 2, 1),
            FilterToDate = true, ToDate = new DateTime(2020, 2, 29)
        };

        await ReceptionService().DoSearchReceptionAsync(state, filter);

        var row = Assert.Single(state.GridRows);
        Assert.Equal(22, row.OrderID);
        Assert.Equal("02/13/2020", row.OrderDate);
        Assert.False(string.IsNullOrWhiteSpace(row.ProviderName));
    }

    [Fact]
    public async Task ReceptionSearch_NoFilters_ReturnsAllJoinedOrders()
    {
        var state = new ReceptionApprovalStateDto();

        await ReceptionService().DoSearchReceptionAsync(state, new ReceptionApprovalFilterDto());

        Assert.Equal(11, state.GridRows.Count);
        Assert.All(state.GridRows, r => Assert.True(r.Price > 0m));
    }
}
