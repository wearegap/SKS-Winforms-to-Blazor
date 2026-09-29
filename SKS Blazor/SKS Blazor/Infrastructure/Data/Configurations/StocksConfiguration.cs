using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SKS_Blazor.Infrastructure.Data.Configurations;

/// <summary>Deterministic EF relationship configuration for <see cref="SKS_Blazor.Domain.Entities.Core.Stocks"/>.</summary>
public class StocksConfiguration : IEntityTypeConfiguration<SKS_Blazor.Domain.Entities.Core.Stocks>
{
    public void Configure(EntityTypeBuilder<SKS_Blazor.Domain.Entities.Core.Stocks> builder)
    {
        builder.HasOne(x => x.Product).WithMany(y => y.Stocks).HasForeignKey(x => x.ProductID).OnDelete(DeleteBehavior.Restrict);
    }
}
