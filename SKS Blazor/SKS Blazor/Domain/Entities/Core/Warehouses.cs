#nullable disable
using System;
using System.Collections.Generic;

namespace SKS_Blazor.Domain.Entities.Core;

/// <summary>Port of <c>main:main.Warehouses table (reference)</c>. Deterministically generated from the plan's schema.</summary>
public class Warehouses
{
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string CountryRegion { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public string StateOrProvince { get; set; } = string.Empty;
    public int WarehouseID { get; set; }
}
