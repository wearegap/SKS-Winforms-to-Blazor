namespace SKS_Blazor.Application.DTOs.Login;

/// <summary>
/// Port of the <c>frmLogin</c> inputs and the <c>modMain</c> globals it sets on success
/// (UserId, UserFullname, UserLevel, CurrentUserAdmin).
/// </summary>
public class LoginStateDto
{
    // ── Inputs (txtUserName / txtPassword) ──
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;

    // ── Outcome ──
    public bool LoginSucceeded { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string UserFullname { get; set; } = string.Empty;
    public string UserLevel { get; set; } = string.Empty;
    public bool CurrentUserAdmin { get; set; }

    /// <summary>Legacy LogStatus text: "User : {Fullname} logged at {now}".</summary>
    public string StatusMessage { get; set; } = string.Empty;

    /// <summary>Legacy MessageBox text on failure.</summary>
    public string ErrorMessage { get; set; } = string.Empty;
}
