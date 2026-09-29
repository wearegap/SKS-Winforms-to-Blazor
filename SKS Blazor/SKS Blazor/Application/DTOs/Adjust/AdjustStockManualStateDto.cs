#nullable disable
using System;
using System.Collections.Generic;

namespace SKS_Blazor.Application.DTOs.Adjust;

/// <summary>AdjustStockManualState data transfer object (domain: Adjust).</summary>
public class AdjustStockManualStateDto
{
    public bool EditingData { get; set; }
    public string CurrentIdProduct { get; set; } = string.Empty;
    public int CurrentIdStock { get; set; }
    public decimal CurrentStockPrice { get; set; }
    public decimal CurrentStock { get; set; }
    public decimal ChangedStockPrice { get; set; }
    public decimal ChangedStock { get; set; }
    public bool CodeGeneratedChange { get; set; }
    public string TxtStockIDText { get; set; } = string.Empty;
    public string TxtOriginalPriceText { get; set; } = string.Empty;
    public string TxtQuantityPerUnitText { get; set; } = string.Empty;
    public string TxtProductNameText { get; set; } = string.Empty;
    public string TxtUnitText { get; set; } = string.Empty;
    public string TxtOriginalQuantityText { get; set; } = string.Empty;
    public string TxtCodeText { get; set; } = string.Empty;
    public string TxtNameText { get; set; } = string.Empty;
    public string LblNewQuantityText { get; set; } = string.Empty;
    public string LblCurrentQuantityText { get; set; } = string.Empty;
}
