using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SKS_Blazor.Application.DTOs.Products;
using SKS_Blazor.Application.Services.Products;
using Xunit;

namespace SKS_Blazor.Tests;

/// <summary>
/// The Products screen used to call no-op <c>SaveAsync()</c>/<c>DeleteAsync()</c> overloads while the
/// page reported "saved successfully", so edits were lost silently. These tests assert the write
/// actually reaches the database — a no-op implementation fails them.
/// </summary>
public class ProductsPersistenceTests : IDisposable
{
    private readonly SqliteDbFixture _db = new();

    public void Dispose() => _db.Dispose();

    private ProductsService Service() => new(_db, NullLogger<ProductsService>.Instance);

    [Fact]
    public async Task SaveProduct_UpdatesExistingRow()
    {
        var existing = await Service().GetProductByIdAsync("IK1");
        Assert.NotNull(existing);
        Assert.Equal(32m, existing!.UnitPrice);

        await Service().SaveProductAsync(new ProductSaveDto
        {
            ProductID = "IK1",
            ProductName = "Ikura Deluxe",
            ProductDescription = existing.ProductDescription,
            CategoryID = existing.CategoryID,
            UnitPrice = 99.5m,
            QuantityPerUnit = existing.QuantityPerUnit,
            UnitsInStock = existing.UnitsInStock,
            UnitsOnOrder = existing.UnitsOnOrder,
            Discontinued = existing.Discontinued,
            LeadTime = existing.LeadTime,
            SerialNumber = existing.SerialNumber,
            SupplierID = existing.SupplierID,
            Unit = existing.Unit,
            ReorderLevel = existing.ReorderLevel,
            IsNew = false
        });

        await using var ctx = _db.CreateDbContext();
        var row = await ctx.Products.AsNoTracking().SingleAsync(p => p.ProductID == "IK1");
        Assert.Equal("Ikura Deluxe", row.ProductName);
        Assert.Equal(99.5m, row.UnitPrice);
    }

    [Fact]
    public async Task SaveProduct_InsertsNewRow()
    {
        await Service().SaveProductAsync(new ProductSaveDto
        {
            ProductID = "TEST1",
            ProductName = "Test Fish",
            UnitPrice = 12.25m,
            CategoryID = 2,
            IsNew = true
        });

        await using var ctx = _db.CreateDbContext();
        Assert.Equal(5, await ctx.Products.CountAsync());
        var row = await ctx.Products.AsNoTracking().SingleAsync(p => p.ProductID == "TEST1");
        Assert.Equal("Test Fish", row.ProductName);
        Assert.Equal(12.25m, row.UnitPrice);
    }

    [Fact]
    public async Task SaveProduct_RejectsDuplicateId()
    {
        var dto = new ProductSaveDto { ProductID = "IK1", ProductName = "Dupe", IsNew = true };

        await Assert.ThrowsAsync<InvalidOperationException>(() => Service().SaveProductAsync(dto));
    }

    [Fact]
    public async Task DeleteProduct_RemovesRow()
    {
        await Service().DeleteProductAsync("SH22");

        await using var ctx = _db.CreateDbContext();
        Assert.Equal(3, await ctx.Products.CountAsync());
        Assert.False(await ctx.Products.AnyAsync(p => p.ProductID == "SH22"));
    }
}
