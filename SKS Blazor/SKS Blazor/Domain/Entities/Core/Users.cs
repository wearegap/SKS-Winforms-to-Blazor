#nullable disable
using System;
using System.Collections.Generic;

namespace SKS_Blazor.Domain.Entities.Core;

/// <summary>Port of <c>main:main.Users table (reference)</c>. Deterministically generated from the plan's schema.</summary>
public class Users
{
    public string Fullname { get; set; } = string.Empty;
    public string Level { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
}
