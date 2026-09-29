#nullable disable
using System;
using System.Collections.Generic;

namespace SKS_Blazor.Application.DTOs.Login;

/// <summary>LoginState data transfer object (domain: Login).</summary>
public class LoginStateDto
{
    public bool LoginSucceeded { get; set; }
}
