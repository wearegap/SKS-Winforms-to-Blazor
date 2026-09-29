#nullable disable
using System;
using System.Collections.Generic;

namespace SKS_Blazor.Application.DTOs.Product;

/// <summary>AddProductToState data transfer object (domain: Product).</summary>
public class AddProductToStateDto
{
    public OrderedDictionary ProductsStored { get; set; }
    public OrderedDictionary ProductsToDelete { get; set; }
    public OrderedDictionary ProductsToAdd { get; set; }
    public bool EditingData { get; set; }
    public string CurrentIdProduct { get; set; } = string.Empty;
    public string TxtNameText { get; set; } = string.Empty;
    public string TxtCodeText { get; set; } = string.Empty;
}
