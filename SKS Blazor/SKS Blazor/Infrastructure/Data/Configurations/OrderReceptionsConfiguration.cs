using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SKS_Blazor.Infrastructure.Data.Configurations;

/// <summary>Deterministic EF relationship configuration for <see cref="SKS_Blazor.Domain.Entities.Order.OrderReceptions"/>.</summary>
public class OrderReceptionsConfiguration : IEntityTypeConfiguration<SKS_Blazor.Domain.Entities.Order.OrderReceptions>
{
    public void Configure(EntityTypeBuilder<SKS_Blazor.Domain.Entities.Order.OrderReceptions> builder)
    {
        builder.HasOne(x => x.Provider).WithMany(y => y.OrderReceptions).HasForeignKey(x => x.ProviderID).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.OrderRequest).WithMany(y => y.OrderReceptions).HasForeignKey(x => x.OrderID).OnDelete(DeleteBehavior.Restrict);
    }
}
