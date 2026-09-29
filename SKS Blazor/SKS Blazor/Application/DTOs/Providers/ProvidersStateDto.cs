#nullable disable
using System;
using System.Collections.Generic;

namespace SKS_Blazor.Application.DTOs.Providers;

/// <summary>ProvidersState data transfer object (domain: Providers).</summary>
public class ProvidersStateDto
{
    public bool NewMode { get; set; }
    public bool EditMode { get; set; }
    public bool CancellingMode { get; set; }
}
