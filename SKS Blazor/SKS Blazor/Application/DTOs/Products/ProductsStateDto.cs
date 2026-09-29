#nullable disable
using System;
using System.Collections.Generic;

namespace SKS_Blazor.Application.DTOs.Products;

/// <summary>ProductsState data transfer object (domain: Products).</summary>
public class ProductsStateDto
{
    public bool NewMode { get; set; }
    public bool EditMode { get; set; }
    public bool CancellingMode { get; set; }
    public int CmbCategorySelectedIndex { get; set; }
    public string TxtCategoryText { get; set; } = string.Empty;
}
