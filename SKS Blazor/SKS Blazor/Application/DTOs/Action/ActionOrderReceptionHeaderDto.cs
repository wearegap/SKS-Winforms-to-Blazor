namespace SKS_Blazor.Application.DTOs.Action;

/// <summary>Header information for an order reception displayed on the action screen.</summary>
public class ActionOrderReceptionHeaderDto
{
    public int OrderID { get; set; }
    public string OrderDate { get; set; } = string.Empty;
    public string ReceivedByFullname { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public decimal FreightCharge { get; set; }
    public decimal SalesTaxRate { get; set; }
    public string ProviderCompany { get; set; } = string.Empty;
    public string ProviderContact { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string ChangedDate { get; set; } = string.Empty;
    public string ChangedBy { get; set; } = string.Empty;
    public bool IsReceived { get; set; }
    public bool ShowChangedFields { get; set; }
    public bool ApproveEnabled { get; set; }
    public bool CancelEnabled { get; set; }
    public string ChangedDateLabel { get; set; } = string.Empty;
    public string ChangedByLabel { get; set; } = string.Empty;
}
