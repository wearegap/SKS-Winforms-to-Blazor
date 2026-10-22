using Microsoft.Extensions.Logging.Abstractions;
using SKS_Blazor.Application.DTOs.Adjust;
using SKS_Blazor.Application.DTOs.Order;
using SKS_Blazor.Application.Services.Adjust;
using SKS_Blazor.Application.Services.Order;
using Xunit;

namespace SKS_Blazor.Tests;

/// <summary>
/// These screens filtered with <c>.Contains()</c>, which EF Core translates to SQLite's
/// case-sensitive <c>instr()</c> — typing "ikura" found nothing. The legacy app used SQL LIKE,
/// which is case-insensitive for ASCII. Searches now use <c>EF.Functions.Like</c>.
/// </summary>
public class CaseInsensitiveSearchTests : IDisposable
{
    private readonly SqliteDbFixture _db = new();

    public void Dispose() => _db.Dispose();

    [Theory]
    [InlineData("Ikura")]   // exact case
    [InlineData("ikura")]   // all lower
    [InlineData("IKURA")]   // all upper
    [InlineData("kur")]     // mid-string, so LIKE must wrap both sides
    public async Task AdjustStockManual_ProductNameSearch_IgnoresCase(string term)
    {
        var service = new AdjustStockManualService(_db, NullLogger<AdjustStockManualService>.Instance);

        var rows = await service.DoSearchProductAsync(new AdjustStockManualStateDto { TxtNameText = term });

        Assert.Single(rows);
        Assert.Equal("IK1", rows[0].ProductID);
    }

    [Theory]
    [InlineData("CRAB1")]
    [InlineData("crab1")]
    [InlineData("rab")]
    public async Task AdjustStockManual_ProductCodeSearch_IgnoresCase(string term)
    {
        var service = new AdjustStockManualService(_db, NullLogger<AdjustStockManualService>.Instance);

        var rows = await service.DoSearchProductAsync(new AdjustStockManualStateDto { TxtCodeText = term });

        Assert.Single(rows);
        Assert.Equal("CRAB1", rows[0].ProductID);
    }

    [Theory]
    [InlineData("Spectacular")]
    [InlineData("spectacular")]
    [InlineData("SPECTACULAR")]
    public async Task OrderRequest_CustomerSearch_IgnoresCase(string term)
    {
        var service = new OrderRequestService(_db, NullLogger<OrderRequestService>.Instance);

        var rows = await service.DoSearchCustomerAsync(
            new OrderRequestStateDto { TxtCompanyNameText = term }, string.Empty);

        Assert.Single(rows);
        Assert.Equal("Spectacular Food test", rows[0].CompanyName);
    }
}
