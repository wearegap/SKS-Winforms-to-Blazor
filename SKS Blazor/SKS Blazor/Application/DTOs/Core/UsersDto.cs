#nullable disable
using System;
using System.Collections.Generic;

namespace SKS_Blazor.Application.DTOs.Core;

/// <summary>Users data transfer object (domain: Core).</summary>
public class UsersDto
{
    public string Fullname { get; set; } = string.Empty;
    public string Level { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
}
