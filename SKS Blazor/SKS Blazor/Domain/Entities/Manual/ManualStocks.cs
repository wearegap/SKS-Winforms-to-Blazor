#nullable disable
using System;
using System.Collections.Generic;

namespace SKS_Blazor.Domain.Entities.Manual;

/// <summary>Port of <c>main:main.ManualStocks table (reference)</c>. Deterministically generated from the plan's schema.</summary>
public class ManualStocks
{
    public string Action { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
    public int ManualID { get; set; }
    public decimal? Price { get; set; }
    public decimal? Quantity { get; set; }
    public int? StockID { get; set; }
    public string User { get; set; } = string.Empty;
}
