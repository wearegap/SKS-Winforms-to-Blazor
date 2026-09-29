#nullable disable
using System;
using System.Collections.Generic;

namespace SKS_Blazor.Domain.Entities.Order;

/// <summary>Port of <c>main:main.OrderRequests table (reference)</c>. Deterministically generated from the plan's schema.</summary>
public class OrderRequests
{
    public string ChangedBy { get; set; } = string.Empty;
    public string ChangedDate { get; set; } = string.Empty;
    public int? CustomerID { get; set; }
    public string EmployeeID { get; set; } = string.Empty;
    public decimal? FreightCharge { get; set; }
    public string Notes { get; set; } = string.Empty;
    public string OrderDate { get; set; } = string.Empty;
    public int OrderID { get; set; }
    public string PromisedByDate { get; set; } = string.Empty;
    public string PurchaseOrderNumber { get; set; } = string.Empty;
    public string RequiredByDate { get; set; } = string.Empty;
    public decimal? SalesTaxRate { get; set; }
    public string ShipAddress { get; set; } = string.Empty;
    public string ShipCity { get; set; } = string.Empty;
    public string ShipCountry { get; set; } = string.Empty;
    public string ShipDate { get; set; } = string.Empty;
    public string ShipName { get; set; } = string.Empty;
    public string ShipPhoneNumber { get; set; } = string.Empty;
    public int? ShippingMethodID { get; set; }
    public string ShipPostalCode { get; set; } = string.Empty;
    public string ShipState { get; set; } = string.Empty;
    public string ShipStateOrProvince { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;

    public virtual ICollection<SKS_Blazor.Domain.Entities.Order.OrderReceptions> OrderReceptions { get; set; } = new List<SKS_Blazor.Domain.Entities.Order.OrderReceptions>();
    public virtual ICollection<SKS_Blazor.Domain.Entities.Order.OrderRequestDetails> OrderRequestDetails { get; set; } = new List<SKS_Blazor.Domain.Entities.Order.OrderRequestDetails>();
}
