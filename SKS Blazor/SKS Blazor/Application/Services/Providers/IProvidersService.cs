using SKS_Blazor.Application.DTOs.Providers;
using SKS_Blazor.Domain.Entities.Core;

namespace SKS_Blazor.Application.Services.Providers;

/// <summary>
/// Service contract for the Providers management screen.
/// Port of <c>frmProviders</c> persistence operations.
/// </summary>
public interface IProvidersService
{
    /// <summary>
    /// Validates required provider fields and persists (insert or update) the provider record.
    /// Port of <c>frmProviders.Save()</c> + <c>Toolbar1_ButtonClick("Save")</c>.
    /// </summary>
    /// <param name="state">Provider data and mode flags.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Updated state with NewMode/EditMode reset on success; validation errors surfaced via exception.</returns>
    async Task<ProvidersStateDto> SaveAsync(ProvidersStateDto state, CancellationToken ct = default);

    /// <summary>
    /// Deletes the provider identified by the current state's ProviderID.
    /// Port of <c>Toolbar1_ButtonClick("Delete")</c>.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    async Task DeleteAsync(CancellationToken ct = default);
}
