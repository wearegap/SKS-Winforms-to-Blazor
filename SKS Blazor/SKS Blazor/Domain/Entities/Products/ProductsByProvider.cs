#nullable disable
using System;
using System.Collections.Generic;

namespace SKS_Blazor.Domain.Entities.Products;

/// <summary>Port of <c>main:main.ProductsByProvider table (reference)</c>. Deterministically generated from the plan's schema.</summary>
public class ProductsByProvider
{
    public int ID { get; set; }
    public string ProductID { get; set; } = string.Empty;
    public int? ProviderID { get; set; }

    public virtual SKS_Blazor.Domain.Entities.Core.Products Product { get; set; }
    public virtual SKS_Blazor.Domain.Entities.Core.Providers Provider { get; set; }
}
