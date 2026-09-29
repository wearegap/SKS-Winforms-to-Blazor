using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SKS_Blazor.Infrastructure.Data.Configurations;

/// <summary>Deterministic EF relationship configuration for <see cref="SKS_Blazor.Domain.Entities.Order.OrderReceptionDetails"/>.</summary>
public class OrderReceptionDetailsConfiguration : IEntityTypeConfiguration<SKS_Blazor.Domain.Entities.Order.OrderReceptionDetails>
{
    public void Configure(EntityTypeBuilder<SKS_Blazor.Domain.Entities.Order.OrderReceptionDetails> builder)
    {
        builder.HasOne(x => x.OrderReception).WithMany(y => y.OrderReceptionDetails).HasForeignKey(x => x.OrderID).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Product).WithMany(y => y.OrderReceptionDetails).HasForeignKey(x => x.ProductID).OnDelete(DeleteBehavior.Restrict);
    }
}
