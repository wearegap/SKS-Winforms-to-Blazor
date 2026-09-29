#nullable disable
using System;
using System.Collections.Generic;

namespace SKS_Blazor.Domain.Entities.Core;

/// <summary>Port of <c>main:main.Stocks table (reference)</c>. Deterministically generated from the plan's schema.</summary>
public class Stocks
{
    public string DateModified { get; set; } = string.Empty;
    public string DateStarted { get; set; } = string.Empty;
    public decimal? InitialStock { get; set; }
    public string ProductID { get; set; } = string.Empty;
    public decimal? Stock { get; set; }
    public int StockID { get; set; }
    public decimal? StockPrice { get; set; }
    public decimal? UnitPrice { get; set; }
    public string User { get; set; } = string.Empty;

    public virtual SKS_Blazor.Domain.Entities.Core.Products Product { get; set; }
}
