using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SKS_Blazor.Infrastructure.Data.Configurations;

/// <summary>Deterministic EF relationship configuration for <see cref="SKS_Blazor.Domain.Entities.Order.OrderRequestDetails"/>.</summary>
public class OrderRequestDetailsConfiguration : IEntityTypeConfiguration<SKS_Blazor.Domain.Entities.Order.OrderRequestDetails>
{
    public void Configure(EntityTypeBuilder<SKS_Blazor.Domain.Entities.Order.OrderRequestDetails> builder)
    {
        builder.HasOne(x => x.OrderRequest).WithMany(y => y.OrderRequestDetails).HasForeignKey(x => x.OrderID).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Product).WithMany(y => y.OrderRequestDetails).HasForeignKey(x => x.ProductID).OnDelete(DeleteBehavior.Restrict);
    }
}
