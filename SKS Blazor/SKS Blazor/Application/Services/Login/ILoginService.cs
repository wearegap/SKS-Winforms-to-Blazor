using SKS_Blazor.Application.DTOs.Login;
using SKS_Blazor.Domain.Entities.Core;

namespace SKS_Blazor.Application.Services.Login;

/// <summary>
/// Port of <c>frmLogin</c> — authenticates users against the Users table.
/// </summary>
public interface ILoginService
{
    /// <summary>
    /// Validates the supplied username and password against the Users table.
    /// Port of <c>cmdOk_Click</c> from frmLogin.
    /// </summary>
    /// <param name="state">The login state DTO (currently unused; kept for interface contract).</param>
    /// <param name="eventSender">The event sender object (unused in Blazor context).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A <see cref="LoginStateDto"/> indicating success/failure.</returns>
    Task<LoginStateDto> OkAsync(LoginStateDto state, object eventSender, CancellationToken ct = default);
}
