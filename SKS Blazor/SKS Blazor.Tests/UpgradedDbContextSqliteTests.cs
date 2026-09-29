using Microsoft.EntityFrameworkCore;
using SKS_Blazor.Domain.Entities.Order;
using Xunit;
using Xunit.Abstractions;

namespace SKS_Blazor.Tests;

public class UpgradedDbContextSqliteTests : IDisposable
{
    private readonly SqliteDbFixture _db = new();
    private readonly ITestOutputHelper _output;

    public UpgradedDbContextSqliteTests(ITestOutputHelper output) => _output = output;

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task DecimalColumns_OrderAndCompareInSql()
    {
        await using var ctx = _db.CreateDbContext();
        var query = ctx.OrderRequestDetails
            .Where(d => d.LineTotal > 0m)
            .OrderByDescending(d => d.LineTotal)
            .Take(3);
        var sql = query.ToQueryString();
        _output.WriteLine(sql);

        // With the convention the comparison is native REAL SQL. Without it EF Core 10 falls back to
        // its ef_compare() function and EF_DECIMAL collation over TEXT-typed values.
        Assert.DoesNotContain("ef_compare", sql);
        Assert.DoesNotContain("EF_DECIMAL", sql);

        var rows = await query.ToListAsync();

        Assert.NotEmpty(rows);
        Assert.True(rows[0].LineTotal >= rows[^1].LineTotal);
    }

    [Fact]
    public void DecimalColumns_MapToRealLikeTheLegacySchema()
    {
        // Orders.db declares every money/quantity column REAL. The decimal->double convention keeps
        // EF's store type (and therefore the parameter type it writes) aligned with that.
        using var ctx = _db.CreateDbContext();
        var property = ctx.Model.FindEntityType(typeof(OrderRequestDetails))!
            .FindProperty(nameof(OrderRequestDetails.LineTotal))!;
        _output.WriteLine($"store type: {property.GetColumnType()}, provider CLR type: {property.GetProviderClrType()}");

        Assert.Equal("REAL", property.GetColumnType());
        var providerType = property.GetProviderClrType();
        Assert.Equal(typeof(double), providerType is null ? null : Nullable.GetUnderlyingType(providerType) ?? providerType);
    }

    [Fact]
    public async Task RealDatabase_HasExpectedRowCounts()
    {
        await using var ctx = _db.CreateDbContext();
        Assert.Equal(25, await ctx.OrderRequests.CountAsync());
        Assert.Equal(19, await ctx.OrderReceptions.CountAsync());
        Assert.Equal(50, await ctx.Stocks.CountAsync());
        Assert.Equal(4, await ctx.Products.CountAsync());
        Assert.Equal(5, await ctx.Users.CountAsync());
    }
}
