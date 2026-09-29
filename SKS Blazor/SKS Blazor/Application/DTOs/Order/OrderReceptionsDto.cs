#nullable disable
using System;
using System.Collections.Generic;

namespace SKS_Blazor.Application.DTOs.Order;

/// <summary>OrderReceptions data transfer object (domain: Order).</summary>
public class OrderReceptionsDto
{
    public string ChangedBy { get; set; } = string.Empty;
    public string ChangedDate { get; set; } = string.Empty;
    public decimal? FreightCharge { get; set; }
    public string Notes { get; set; } = string.Empty;
    public string OrderDate { get; set; } = string.Empty;
    public int OrderID { get; set; }
    public string PromisedByDate { get; set; } = string.Empty;
    public int? ProviderID { get; set; }
    public string PurchaseOrderNumber { get; set; } = string.Empty;
    public string ReceivedBy { get; set; } = string.Empty;
    public string RequiredByDate { get; set; } = string.Empty;
    public decimal? SalesTaxRate { get; set; }
    public string Status { get; set; } = string.Empty;
}
