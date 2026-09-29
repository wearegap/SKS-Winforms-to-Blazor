#nullable disable
using System;
using System.Collections.Generic;

namespace SKS_Blazor.Domain.Entities.Core;

/// <summary>Port of <c>main:main.Invoices table (reference)</c>. Deterministically generated from the plan's schema.</summary>
public class Invoices
{
    public int? CustomerID { get; set; }
    public int? DeliveryID { get; set; }
    public int? EmployeeID { get; set; }
    public string InvoiceDate { get; set; } = string.Empty;
    public int InvoiceID { get; set; }
    public string Notes { get; set; } = string.Empty;
    public int? OrderID { get; set; }
    public string SalespersonName { get; set; } = string.Empty;
    public string ShipDate { get; set; } = string.Empty;
    public string ShippedTo { get; set; } = string.Empty;
    public string ShippedVia { get; set; } = string.Empty;
    public decimal? ShippingCost { get; set; }
    public string Status { get; set; } = string.Empty;

    public virtual SKS_Blazor.Domain.Entities.Core.Customers Customer { get; set; }
    public virtual ICollection<SKS_Blazor.Domain.Entities.Invoice.InvoiceDetails> InvoiceDetails { get; set; } = new List<SKS_Blazor.Domain.Entities.Invoice.InvoiceDetails>();
}
