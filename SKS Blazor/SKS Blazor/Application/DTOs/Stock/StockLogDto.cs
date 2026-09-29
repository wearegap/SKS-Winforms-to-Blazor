#nullable disable
using System;
using System.Collections.Generic;

namespace SKS_Blazor.Application.DTOs.Stock;

/// <summary>StockLog data transfer object (domain: Stock).</summary>
public class StockLogDto
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
}
