#nullable disable
using System;
using System.Collections.Generic;

namespace SKS_Blazor.Domain.Entities.Order;

/// <summary>Port of <c>main:main.OrderReceptions table (reference)</c>. Deterministically generated from the plan's schema.</summary>
public class OrderReceptions
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

    public virtual SKS_Blazor.Domain.Entities.Core.Providers Provider { get; set; }
    public virtual SKS_Blazor.Domain.Entities.Order.OrderRequests OrderRequest { get; set; }
    public virtual ICollection<SKS_Blazor.Domain.Entities.Order.OrderReceptionDetails> OrderReceptionDetails { get; set; } = new List<SKS_Blazor.Domain.Entities.Order.OrderReceptionDetails>();
}
