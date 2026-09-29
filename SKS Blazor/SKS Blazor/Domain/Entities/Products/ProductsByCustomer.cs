#nullable disable
using System;
using System.Collections.Generic;

namespace SKS_Blazor.Domain.Entities.Products;

/// <summary>Port of <c>main:main.ProductsByCustomer table (reference)</c>. Deterministically generated from the plan's schema.</summary>
public class ProductsByCustomer
{
    public int? CustomerID { get; set; }
    public int ID { get; set; }
    public string ProductID { get; set; } = string.Empty;

    public virtual SKS_Blazor.Domain.Entities.Core.Products Product { get; set; }
    public virtual SKS_Blazor.Domain.Entities.Core.Customers Customer { get; set; }
}
