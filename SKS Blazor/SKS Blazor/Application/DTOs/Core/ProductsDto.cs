#nullable disable
using System;
using System.Collections.Generic;

namespace SKS_Blazor.Application.DTOs.Core;

/// <summary>Products data transfer object (domain: Core).</summary>
public class ProductsDto
{
    public int? CategoryID { get; set; }
    public int Discontinued { get; set; }
    public string LeadTime { get; set; } = string.Empty;
    public string ProductDescription { get; set; } = string.Empty;
    public string ProductID { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public decimal? QuantityPerUnit { get; set; }
    public int? ReorderLevel { get; set; }
    public string SerialNumber { get; set; } = string.Empty;
    public int? SupplierID { get; set; }
    public string Unit { get; set; } = string.Empty;
    public decimal? UnitPrice { get; set; }
    public int? UnitsInStock { get; set; }
    public int? UnitsOnOrder { get; set; }
}
