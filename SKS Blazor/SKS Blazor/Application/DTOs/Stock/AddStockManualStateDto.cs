#nullable disable
using System;
using System.Collections.Generic;

namespace SKS_Blazor.Application.DTOs.Stock;

/// <summary>AddStockManualState data transfer object (domain: Stock).</summary>
public class AddStockManualStateDto
{
    public bool EditingData { get; set; }
    public string CurrentIdProduct { get; set; } = string.Empty;
    public bool CodeGeneratedChange { get; set; }
    public string TxtUnitText { get; set; } = string.Empty;
    public string TxtProductNameText { get; set; } = string.Empty;
    public string TxtQuantityPerUnitText { get; set; } = string.Empty;
    public string TxtNameText { get; set; } = string.Empty;
    public string TxtCodeText { get; set; } = string.Empty;
    public string LblNewQuantityText { get; set; } = string.Empty;
}
