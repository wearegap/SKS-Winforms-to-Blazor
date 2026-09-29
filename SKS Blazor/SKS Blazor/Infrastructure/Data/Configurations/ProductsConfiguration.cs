using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SKS_Blazor.Infrastructure.Data.Configurations;

/// <summary>Deterministic EF relationship configuration for <see cref="SKS_Blazor.Domain.Entities.Core.Products"/>.</summary>
public class ProductsConfiguration : IEntityTypeConfiguration<SKS_Blazor.Domain.Entities.Core.Products>
{
    public void Configure(EntityTypeBuilder<SKS_Blazor.Domain.Entities.Core.Products> builder)
    {
        builder.HasOne(x => x.Category).WithMany(y => y.Products).HasForeignKey(x => x.CategoryID).OnDelete(DeleteBehavior.Restrict);
    }
}
