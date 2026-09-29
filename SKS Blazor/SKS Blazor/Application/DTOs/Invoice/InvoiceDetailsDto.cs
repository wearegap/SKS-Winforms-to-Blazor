#nullable disable
using System;
using System.Collections.Generic;

namespace SKS_Blazor.Application.DTOs.Invoice;

/// <summary>InvoiceDetails data transfer object (domain: Invoice).</summary>
public class InvoiceDetailsDto
{
    public decimal? Discount { get; set; }
    public int InvoiceDetailID { get; set; }
    public int? InvoiceID { get; set; }
    public string PaymentTerms { get; set; } = string.Empty;
    public decimal? PricePerUnit { get; set; }
    public decimal? Quantity { get; set; }
    public int? StockID { get; set; }
}
