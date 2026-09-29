#nullable disable
using System;
using System.Collections.Generic;

namespace SKS_Blazor.Application.DTOs.Action;

/// <summary>
/// State for <c>frmActionOrderReception</c>. The page sets <see cref="OrderID"/> and
/// <see cref="CurrentUserId"/>; the service fills the header, details, labels and totals.
/// </summary>
public class ActionOrderReceptionStateDto
{
    // ── Inputs set by the page ──
    public int OrderID { get; set; }
    public string CurrentUserId { get; set; } = string.Empty;

    // ── Load / action outcome ──
    public bool OrderFound { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public string SuccessMessage { get; set; } = string.Empty;

    // ── Header (legacy LoadData) ──
    public string OrderDate { get; set; } = string.Empty;
    public string ReceivedByFullname { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public decimal FreightCharge { get; set; }
    public decimal SalesTaxRate { get; set; }
    public string ProviderCompany { get; set; } = string.Empty;
    public string ProviderContact { get; set; } = string.Empty;
    public string ChangedDate { get; set; } = string.Empty;
    public string ChangedBy { get; set; } = string.Empty;

    // ── Visibility / labels (legacy LoadData) ──
    public bool IsReceived { get; set; }
    public bool ShowChangedFields { get; set; }
    public bool ApproveEnabled { get; set; }
    public bool CancelEnabled { get; set; }
    public string ChangedDateLabel { get; set; } = string.Empty;
    public string ChangedByLabel { get; set; } = string.Empty;

    // ── Details (legacy LoadDetails) ──
    public List<ActionOrderReceptionDetailDto> Details { get; set; } = new();

    // ── Totals (legacy AddToTotals / DisplayTotals) ──
    public decimal CurrentSubTotal { get; set; }
    public decimal CurrentTotal { get; set; }
    public decimal CurrentTax { get; set; }
    public decimal CurrentFreightCharge { get; set; }
    public decimal CurrentTotalTax { get; set; }
    public string TxtStatusText { get; set; } = string.Empty;
    public string TxtSubTotalText { get; set; } = string.Empty;
    public string TxtTotalText { get; set; } = string.Empty;
    public string TxtTotalTaxText { get; set; } = string.Empty;
}
