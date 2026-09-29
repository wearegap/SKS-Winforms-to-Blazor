#nullable disable
using System;
using System.Collections.Generic;

namespace SKS_Blazor.Application.DTOs.Order;

/// <summary>OrderReceptionState data transfer object (domain: Order).</summary>
public class OrderReceptionStateDto
{
    public int CurrentIdProvider { get; set; }
    public bool EditingData { get; set; }
    public decimal CurrentSubTotal { get; set; }
    public decimal CurrentTotal { get; set; }
    public decimal CurrentTax { get; set; }
    public decimal CurrentFreightCharge { get; set; }
    public decimal CurrentTotalTax { get; set; }
    public string TxtNotesText { get; set; } = string.Empty;
    public string TxtSubTotalText { get; set; } = string.Empty;
    public string TxtTotalText { get; set; } = string.Empty;
    public string TxtTotalTaxText { get; set; } = string.Empty;
    public string TxtFreightChargeText { get; set; } = string.Empty;
    public string TxtSalesTaxText { get; set; } = string.Empty;
    public string TxtEntryText { get; set; } = string.Empty;
    public string TxtProviderNameText { get; set; } = string.Empty;
    public string TxtContactLastNameText { get; set; } = string.Empty;
    public string TxtContactNameText { get; set; } = string.Empty;
    public string TxtProviderContactText { get; set; } = string.Empty;
    public string TxtProviderCompanyText { get; set; } = string.Empty;
    /// <summary>Logged-in user (legacy modMain.UserId), set by the page before SaveAsync.</summary>
    public string CurrentUserId { get; set; } = string.Empty;
}
