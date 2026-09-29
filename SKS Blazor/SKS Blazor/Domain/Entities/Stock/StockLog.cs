#nullable disable
using System;
using System.Collections.Generic;

namespace SKS_Blazor.Domain.Entities.Stock;

/// <summary>Port of <c>main:main.StockLog table (reference)</c>. Deterministically generated from the plan's schema.</summary>
public class StockLog
{
    public string Date { get; set; } = string.Empty;
    public int? DocID { get; set; }
    public string DocType { get; set; } = string.Empty;
    public int ID { get; set; }
    public string ProductID { get; set; } = string.Empty;
    public decimal? Quantity { get; set; }
    public int? StockID { get; set; }
    public decimal? StockPrice { get; set; }
    public string User { get; set; } = string.Empty;

    public virtual SKS_Blazor.Domain.Entities.Core.Products Product { get; set; }
}
