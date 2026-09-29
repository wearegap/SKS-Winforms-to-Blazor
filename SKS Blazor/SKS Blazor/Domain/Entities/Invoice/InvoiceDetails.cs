#nullable disable
using System;
using System.Collections.Generic;

namespace SKS_Blazor.Domain.Entities.Invoice;

/// <summary>Port of <c>main:main.InvoiceDetails table (reference)</c>. Deterministically generated from the plan's schema.</summary>
public class InvoiceDetails
{
    public decimal? Discount { get; set; }
    public int InvoiceDetailID { get; set; }
    public int? InvoiceID { get; set; }
    public string PaymentTerms { get; set; } = string.Empty;
    public decimal? PricePerUnit { get; set; }
    public decimal? Quantity { get; set; }
    public int? StockID { get; set; }

    public virtual SKS_Blazor.Domain.Entities.Core.Invoices Invoice { get; set; }
}
