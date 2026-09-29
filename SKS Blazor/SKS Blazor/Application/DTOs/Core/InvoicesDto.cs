#nullable disable
using System;
using System.Collections.Generic;

namespace SKS_Blazor.Application.DTOs.Core;

/// <summary>Invoices data transfer object (domain: Core).</summary>
public class InvoicesDto
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
}
