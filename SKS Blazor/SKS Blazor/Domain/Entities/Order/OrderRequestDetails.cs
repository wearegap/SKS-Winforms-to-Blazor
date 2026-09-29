#nullable disable
using System;
using System.Collections.Generic;

namespace SKS_Blazor.Domain.Entities.Order;

/// <summary>Port of <c>main:main.OrderRequestDetails table (reference)</c>. Deterministically generated from the plan's schema.</summary>
public class OrderRequestDetails
{
    public string DateSold { get; set; } = string.Empty;
    public decimal? Discount { get; set; }
    public decimal? LineTotal { get; set; }
    public int OrderDetailID { get; set; }
    public int? OrderID { get; set; }
    public string ProductID { get; set; } = string.Empty;
    public decimal? Quantity { get; set; }
    public decimal? SalePrice { get; set; }
    public decimal? SalesTax { get; set; }
    public decimal? UnitPrice { get; set; }

    public virtual SKS_Blazor.Domain.Entities.Order.OrderRequests OrderRequest { get; set; }
    public virtual SKS_Blazor.Domain.Entities.Core.Products Product { get; set; }
}
