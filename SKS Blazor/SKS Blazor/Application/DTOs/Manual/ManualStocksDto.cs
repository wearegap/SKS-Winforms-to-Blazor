#nullable disable
using System;
using System.Collections.Generic;

namespace SKS_Blazor.Application.DTOs.Manual;

/// <summary>ManualStocks data transfer object (domain: Manual).</summary>
public class ManualStocksDto
{
    public string Action { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
    public int ManualID { get; set; }
    public decimal? Price { get; set; }
    public decimal? Quantity { get; set; }
    public int? StockID { get; set; }
    public string User { get; set; } = string.Empty;
}
