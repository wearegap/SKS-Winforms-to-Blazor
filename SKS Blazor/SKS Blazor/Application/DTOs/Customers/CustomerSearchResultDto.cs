using SKS_Blazor.Domain.Entities.Core;
using SKS_Blazor.Application.DTOs.Core;
namespace SKS_Blazor.Application.DTOs.Customers;

/// <summary>
/// DTO representing a customer search result row.
/// Used by <see cref="SKS_Blazor.Application.Services.Customers.ICustomersService.SearchCriteriaProductsAsync"/>
/// internally, though the contract returns <c>ProductsDto</c> per the plan signature.
/// </summary>
public class CustomerSearchResultDto
{
    public int CustomerID { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string ContactFirstName { get; set; } = string.Empty;
    public string ContactLastName { get; set; } = string.Empty;
    public string ContactTitle { get; set; } = string.Empty;
    public string BillingAddress { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string StateOrProvince { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string CountryRegion { get; set; } = string.Empty;
    public string CompanyOrDepartment { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Extension { get; set; } = string.Empty;
    public string FaxNumber { get; set; } = string.Empty;
    public string EmailAddress { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
}
