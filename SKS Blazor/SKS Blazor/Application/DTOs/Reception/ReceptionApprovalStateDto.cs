#nullable disable
using System;
using System.Collections.Generic;

namespace SKS_Blazor.Application.DTOs.Reception;

/// <summary>ReceptionApprovalState data transfer object (domain: Reception).</summary>
public class ReceptionApprovalStateDto
{
    public string TxtContactLastNameText { get; set; } = string.Empty;
    public string TxtContactNameText { get; set; } = string.Empty;
    public string TxtProviderNameText { get; set; } = string.Empty;
    public List<SKS_Blazor.Application.DTOs.Core.ReceptionApprovalGridRowDto> GridRows { get; set; } = new();
}
