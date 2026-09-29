using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SKS_Blazor.Infrastructure.Data.Configurations;

/// <summary>Deterministic EF relationship configuration for <see cref="SKS_Blazor.Domain.Entities.Products.ProductsByProvider"/>.</summary>
public class ProductsByProviderConfiguration : IEntityTypeConfiguration<SKS_Blazor.Domain.Entities.Products.ProductsByProvider>
{
    public void Configure(EntityTypeBuilder<SKS_Blazor.Domain.Entities.Products.ProductsByProvider> builder)
    {
        builder.HasOne(x => x.Product).WithMany(y => y.ProductsByProviders).HasForeignKey(x => x.ProductID).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Provider).WithMany(y => y.ProductsByProviders).HasForeignKey(x => x.ProviderID).OnDelete(DeleteBehavior.Restrict);
    }
}
