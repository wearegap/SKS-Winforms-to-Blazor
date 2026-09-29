namespace SKS_Blazor.Application.DTOs.Request;

/// <summary>
/// Filter criteria for <c>frmRequestApproval.DoSearchRequest</c> (frmRequestAproval.cs).
/// Each property maps to one legacy filter control. The customer id is passed separately.
/// </summary>
public class RequestApprovalFilterDto
{
    public string CompanyNameText { get; set; } = string.Empty;
    public string ContactNameText { get; set; } = string.Empty;
    public string ContactLastNameText { get; set; } = string.Empty;
    public string OrderIDText { get; set; } = string.Empty;
    public string ProductIDText { get; set; } = string.Empty;
    /// <summary>cmbStatus text; "All" disables the status filter.</summary>
    public string StatusFilter { get; set; } = "All";
    public bool FromDateEnabled { get; set; }
    public bool ToDateEnabled { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}
