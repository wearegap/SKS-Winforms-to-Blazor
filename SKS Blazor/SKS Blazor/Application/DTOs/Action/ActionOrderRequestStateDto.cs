#nullable disable
using System;
using System.Collections.Generic;

namespace SKS_Blazor.Application.DTOs.Action;

/// <summary>ActionOrderRequestState data transfer object (domain: Action).</summary>
public class ActionOrderRequestStateDto
{
    /// <summary>Order being shown or acted on (legacy frmActionOrderRequest.OrderId).</summary>
    public int OrderID { get; set; }
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
