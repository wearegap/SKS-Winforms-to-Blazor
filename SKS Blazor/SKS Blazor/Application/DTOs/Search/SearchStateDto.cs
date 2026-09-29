#nullable disable
using System;
using System.Collections.Generic;

namespace SKS_Blazor.Application.DTOs.Search;

/// <summary>SearchState data transfer object (domain: Search).</summary>
public class SearchStateDto
{
    public string SearchTable { get; set; } = string.Empty;
    public string CboSrchByText { get; set; } = string.Empty;
    public string LblSrchByText { get; set; } = string.Empty;
}
