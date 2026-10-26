using SKS_Blazor.Application.DTOs.Login;

namespace SKS_Blazor.Application.Services.Login;

/// <summary>Port of <c>frmLogin</c> — authenticates users against the Users table.</summary>
public interface ILoginService
{
    /// <summary>
    /// Port of <c>frmLogin.cmdOk_Click</c>: validates <see cref="LoginStateDto.UserName"/> and
    /// <see cref="LoginStateDto.Password"/> and fills the user fields on success.
    /// </summary>
    Task<LoginStateDto> OkAsync(LoginStateDto state, CancellationToken ct = default);

    /// <summary>
    /// Resolves a user by username with no password check, for the auto-login the legacy app performs
    /// in <c>modMain.Main()</c>. Returns a state with <c>LoginSucceeded = false</c> if the row is gone.
    /// Never call this from a path that is supposed to authenticate anybody.
    /// </summary>
    Task<LoginStateDto> LoadUserAsync(string userName, CancellationToken ct = default);
}
