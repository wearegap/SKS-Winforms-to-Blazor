#nullable disable
using System;
using System.Collections.Generic;

namespace SKS_Blazor.Application.DTOs.Core;

/// <summary>Customers data transfer object (domain: Core).</summary>
public class CustomersDto
{
    public string BillingAddress { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string CompanyOrDepartment { get; set; } = string.Empty;
    public string ContactFirstName { get; set; } = string.Empty;
    public string ContactLastName { get; set; } = string.Empty;
    public string ContactTitle { get; set; } = string.Empty;
    public string CountryRegion { get; set; } = string.Empty;
    public int CustomerID { get; set; }
    public string EmailAddress { get; set; } = string.Empty;
    public string Extension { get; set; } = string.Empty;
    public string FaxNumber { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string StateOrProvince { get; set; } = string.Empty;
}
