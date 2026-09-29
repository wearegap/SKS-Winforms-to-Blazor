namespace SKS_Blazor.Application.DTOs.Reception;

/// <summary>
/// Filter criteria for <c>frmReceptionApproval.DoSearchReception</c> (frmOrderAproval.cs).
/// Each property maps to one legacy filter control.
/// </summary>
public class ReceptionApprovalFilterDto
{
    /// <summary>Provider picked through the Providers dialog; -1 means no provider filter (legacy default).</summary>
    public int ProviderId { get; set; } = -1;
    public string ProviderName { get; set; } = string.Empty;
    public string ContactName { get; set; } = string.Empty;
    public string ContactLastName { get; set; } = string.Empty;
    public string OrderID { get; set; } = string.Empty;
    public string ProductID { get; set; } = string.Empty;
    /// <summary>cmbStatus text; "All" disables the status filter.</summary>
    public string Status { get; set; } = "All";
    public bool FilterFromDate { get; set; }
    public bool FilterToDate { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}
