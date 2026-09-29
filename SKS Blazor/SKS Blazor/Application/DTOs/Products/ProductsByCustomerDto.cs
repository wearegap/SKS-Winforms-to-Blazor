#nullable disable
using System;
using System.Collections.Generic;

namespace SKS_Blazor.Application.DTOs.Products;

/// <summary>ProductsByCustomer data transfer object (domain: Products).</summary>
public class ProductsByCustomerDto
{
    public int? CustomerID { get; set; }
    public int ID { get; set; }
    public string ProductID { get; set; } = string.Empty;
}
