#nullable disable
using System;
using System.Collections.Generic;

namespace SKS_Blazor.Application.DTOs.Core;

/// <summary>Warehouses data transfer object (domain: Core).</summary>
public class WarehousesDto
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
