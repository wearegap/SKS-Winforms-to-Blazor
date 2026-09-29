using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SKS_Blazor.Infrastructure.Data.Configurations;

/// <summary>Deterministic EF relationship configuration for <see cref="SKS_Blazor.Domain.Entities.Invoice.InvoiceDetails"/>.</summary>
public class InvoiceDetailsConfiguration : IEntityTypeConfiguration<SKS_Blazor.Domain.Entities.Invoice.InvoiceDetails>
{
    public void Configure(EntityTypeBuilder<SKS_Blazor.Domain.Entities.Invoice.InvoiceDetails> builder)
    {
        builder.HasOne(x => x.Invoice).WithMany(y => y.InvoiceDetails).HasForeignKey(x => x.InvoiceID).OnDelete(DeleteBehavior.Restrict);
    }
}
