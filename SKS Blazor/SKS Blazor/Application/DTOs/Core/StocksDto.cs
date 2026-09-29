#nullable disable
using System;
using System.Collections.Generic;

namespace SKS_Blazor.Application.DTOs.Core;

/// <summary>Stocks data transfer object (domain: Core).</summary>
public class StocksDto
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
}
