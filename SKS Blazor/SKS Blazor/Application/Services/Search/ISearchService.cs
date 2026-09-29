using SKS_Blazor.Application.DTOs.Core;
using SKS_Blazor.Application.DTOs.Search;
using SKS_Blazor.Domain.Entities.Core;

namespace SKS_Blazor.Application.Services.Search;

/// <summary>Port of <c>frmSearch</c> — search service interface for Customers, Products, Providers.</summary>
public interface ISearchService
{
    /// <summary>Returns the list of searchable DB column names for the given table.</summary>
    List<string> GetSearchableFields(string table);

    /// <summary>
    /// Port of <c>frmSearch.Search</c> — synchronous initialization of search state.
    /// Sets SearchTable, CboSrchByText, LblSrchByText on the state DTO.
    /// </summary>
    void Search(SearchStateDto state, string table, string fieldToSearch, string itemToSearch);

    /// <summary>
    /// Port of <c>frmSearch.Search</c> + <c>cmdSearch_Click</c> — async version.
    /// Initializes search context and returns the updated state DTO.
    /// </summary>
    Task<SearchStateDto> SearchAsync(SearchStateDto state, string Table, string fieldToSearch, string itemToSearch, CancellationToken ct = default);

    /// <summary>
    /// Port of <c>frmSearch.SearchCriteriaCustomers</c> — synchronous version.
    /// Queries Customers where the specified field starts with the given value.
    /// </summary>
    List<CustomersDto> SearchCriteriaCustomers(string field, string value);

    /// <summary>
    /// Port of <c>frmSearch.SearchCriteriaCustomers</c> — async version.
    /// Queries Customers where the specified field starts with the given value.
    /// </summary>
    Task<List<CustomersDto>> SearchCriteriaCustomersAsync(string field, string value, CancellationToken ct = default);

    /// <summary>
    /// Port of <c>frmSearch.SearchCriteriaProducts</c> — synchronous version.
    /// Queries Products where the specified field starts with the given value.
    /// </summary>
    List<ProductsDto> SearchCriteriaProducts(string field, string value);

    /// <summary>
    /// Port of <c>frmSearch.SearchCriteriaProducts</c> — async version.
    /// Queries Products where the specified field starts with the given value.
    /// </summary>
    Task<List<ProductsDto>> SearchCriteriaProductsAsync(string field, string value, CancellationToken ct = default);

    /// <summary>
    /// Port of <c>frmSearch.SearchCriteriaProviders</c> — synchronous version.
    /// Queries Providers where the specified field starts with the given value.
    /// </summary>
    List<ProvidersDto> SearchCriteriaProviders(string field, string value);

    /// <summary>
    /// Port of <c>frmSearch.SearchCriteriaProviders</c> — async version.
    /// Queries Providers where the specified field starts with the given value.
    /// </summary>
    Task<List<ProvidersDto>> SearchCriteriaProvidersAsync(string field, string value, CancellationToken ct = default);
}
