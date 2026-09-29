using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SKS_Blazor.Infrastructure.Data.Configurations;

/// <summary>Deterministic EF relationship configuration for <see cref="SKS_Blazor.Domain.Entities.Stock.StockLog"/>.</summary>
public class StockLogConfiguration : IEntityTypeConfiguration<SKS_Blazor.Domain.Entities.Stock.StockLog>
{
    public void Configure(EntityTypeBuilder<SKS_Blazor.Domain.Entities.Stock.StockLog> builder)
    {
        builder.HasOne(x => x.Product).WithMany(y => y.StockLogs).HasForeignKey(x => x.ProductID).OnDelete(DeleteBehavior.Restrict);
    }
}
