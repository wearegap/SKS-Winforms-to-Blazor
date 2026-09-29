#nullable disable
using System;
using System.Collections.Generic;

namespace SKS_Blazor.Application.DTOs.Customers;

/// <summary>CustomersState data transfer object (domain: Customers).</summary>
public class CustomersStateDto
{
    public bool NewMode { get; set; }
    public bool EditMode { get; set; }
    public bool CancellingMode { get; set; }
}
