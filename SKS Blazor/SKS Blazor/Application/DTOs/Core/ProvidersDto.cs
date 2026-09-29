#nullable disable
using System;
using System.Collections.Generic;

namespace SKS_Blazor.Application.DTOs.Core;

/// <summary>Providers data transfer object (domain: Core).</summary>
public class ProvidersDto
{
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string ContactFirstName { get; set; } = string.Empty;
    public string ContactLastName { get; set; } = string.Empty;
    public string ContactTitle { get; set; } = string.Empty;
    public string CountryRegion { get; set; } = string.Empty;
    public string EmailAddress { get; set; } = string.Empty;
    public string Extension { get; set; } = string.Empty;
    public string FaxNumber { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public string PaymentTerms { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public int ProviderID { get; set; }
    public string ProviderName { get; set; } = string.Empty;
    public string StateOrProvince { get; set; } = string.Empty;
}
