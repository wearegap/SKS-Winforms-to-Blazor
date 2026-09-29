using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SKS_Blazor.Infrastructure.Data.Configurations;

/// <summary>Deterministic EF relationship configuration for <see cref="SKS_Blazor.Domain.Entities.Core.Invoices"/>.</summary>
public class InvoicesConfiguration : IEntityTypeConfiguration<SKS_Blazor.Domain.Entities.Core.Invoices>
{
    public void Configure(EntityTypeBuilder<SKS_Blazor.Domain.Entities.Core.Invoices> builder)
    {
        builder.HasOne(x => x.Customer).WithMany(y => y.Invoices).HasForeignKey(x => x.CustomerID).OnDelete(DeleteBehavior.Restrict);
    }
}
