#nullable disable
using System;
using System.Collections.Generic;

namespace SKS_Blazor.Application.DTOs.Core;

/// <summary>Categories data transfer object (domain: Core).</summary>
public class CategoriesDto
{
    public int CategoryID { get; set; }
    public string CategoryName { get; set; } = string.Empty;
}
