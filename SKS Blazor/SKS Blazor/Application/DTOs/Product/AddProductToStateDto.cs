#nullable disable
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using SKS_Blazor.Application.DTOs.Core;

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
    public bool SavedChanges { get; set; }
    public SaveResultInfo SaveResult { get; set; }
    public List<ProductsDto> AssociatedProducts { get; set; } = new();
    public List<string> CheckedProductIds { get; set; } = new();
    public AddProductToContextDto Context { get; set; }
}
