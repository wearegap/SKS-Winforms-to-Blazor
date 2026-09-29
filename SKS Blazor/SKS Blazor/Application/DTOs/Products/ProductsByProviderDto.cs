#nullable disable
using System;
using System.Collections.Generic;

namespace SKS_Blazor.Application.DTOs.Products;

/// <summary>ProductsByProvider data transfer object (domain: Products).</summary>
public class ProductsByProviderDto
{
    public int ID { get; set; }
    public string ProductID { get; set; } = string.Empty;
    public int? ProviderID { get; set; }
}
