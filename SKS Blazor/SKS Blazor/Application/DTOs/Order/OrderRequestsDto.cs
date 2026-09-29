#nullable disable
using System;
using System.Collections.Generic;

namespace SKS_Blazor.Application.DTOs.Order;

/// <summary>OrderRequests data transfer object (domain: Order).</summary>
public class OrderRequestsDto
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
}
