#nullable disable
using System;
using System.Collections.Generic;

namespace SKS_Blazor.Domain.Entities.Core;

/// <summary>Port of <c>main:main.Categories table (reference)</c>. Deterministically generated from the plan's schema.</summary>
public class Categories
{
    public int CategoryID { get; set; }
    public string CategoryName { get; set; } = string.Empty;

    public virtual ICollection<SKS_Blazor.Domain.Entities.Core.Products> Products { get; set; } = new List<SKS_Blazor.Domain.Entities.Core.Products>();
}
