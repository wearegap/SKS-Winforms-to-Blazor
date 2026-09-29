#nullable disable
using System;
using System.Collections.Generic;

namespace SKS_Blazor.Application.DTOs.Request;

/// <summary>RequestApprovalState data transfer object (domain: Request).</summary>
public class RequestApprovalStateDto
{
    public string TxtContactLastNameText { get; set; } = string.Empty;
    public string TxtContactNameText { get; set; } = string.Empty;
    public string TxtCompanyNameText { get; set; } = string.Empty;
}
