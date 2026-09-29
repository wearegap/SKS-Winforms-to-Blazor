using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SKS_Blazor.Infrastructure.Data.Configurations;

/// <summary>Deterministic EF relationship configuration for <see cref="SKS_Blazor.Domain.Entities.Products.ProductsByCustomer"/>.</summary>
public class ProductsByCustomerConfiguration : IEntityTypeConfiguration<SKS_Blazor.Domain.Entities.Products.ProductsByCustomer>
{
    public void Configure(EntityTypeBuilder<SKS_Blazor.Domain.Entities.Products.ProductsByCustomer> builder)
    {
        builder.HasOne(x => x.Product).WithMany(y => y.ProductsByCustomers).HasForeignKey(x => x.ProductID).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Customer).WithMany(y => y.ProductsByCustomers).HasForeignKey(x => x.CustomerID).OnDelete(DeleteBehavior.Restrict);
    }
}
