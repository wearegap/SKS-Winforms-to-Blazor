using SKS_Blazor.Application.DTOs.Core;
using SKS_Blazor.Application.DTOs.Providers;

namespace SKS_Blazor.Application.Services.Providers;

/// <summary>
/// Port of <c>frmProviders</c> — provider CRUD and search.
/// The legacy form bound a data control (dcProviders) to the Providers table;
/// these methods replace its Recordset operations.
/// </summary>
public interface IProvidersService
{
    /// <summary>Port of the dcProviders initial load (Form_Load): all providers ordered by ProviderID.</summary>
    Task<List<ProvidersDto>> GetAllProvidersAsync(CancellationToken ct = default);

    /// <summary>Port of the commented legacy <c>SearchCriteria</c>: <c>ProviderName LIKE 'value%'</c>.</summary>
    Task<List<ProvidersDto>> SearchProvidersByNameAsync(string value, CancellationToken ct = default);

    /// <summary>Port of "Add" then "Save" (Recordset.AddNew + Update). Returns the DTO with the generated ProviderID.</summary>
    Task<ProvidersDto> CreateProviderAsync(ProvidersDto provider, CancellationToken ct = default);

    /// <summary>Port of "Edit" then "Save" (Recordset.Update).</summary>
    Task UpdateProviderAsync(ProvidersDto provider, CancellationToken ct = default);

    /// <summary>Port of "Delete" (Recordset.Delete + Requery). Confirmation is a page concern.</summary>
    Task DeleteProviderAsync(int providerId, CancellationToken ct = default);

    /// <summary>Port of the tail of legacy <c>Save()</c>: EditMode = false; NewMode = false.</summary>
    Task<ProvidersStateDto> SaveAsync(ProvidersStateDto state, CancellationToken ct = default);
}
