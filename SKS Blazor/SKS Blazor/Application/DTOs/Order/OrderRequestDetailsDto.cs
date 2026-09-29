#nullable disable
using System;
using System.Collections.Generic;

namespace SKS_Blazor.Application.DTOs.Order;

/// <summary>OrderRequestDetails data transfer object (domain: Order).</summary>
public class OrderRequestDetailsDto
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
}
