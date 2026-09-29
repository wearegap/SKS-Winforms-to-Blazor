#nullable disable
using System;
using System.Collections.Generic;

namespace SKS_Blazor.Application.DTOs.Users;

/// <summary>UsersManageState data transfer object (domain: Users).</summary>
public class UsersManageStateDto
{
    public string CurrentEditedUser { get; set; } = string.Empty;
    public string CmdSaveText { get; set; } = string.Empty;
    public string TxtFullnameText { get; set; } = string.Empty;
    public string TxtPasswordText { get; set; } = string.Empty;
    public string TxtUsernameText { get; set; } = string.Empty;
    public string CboLevelText { get; set; } = string.Empty;
    public int CboLevelSelectedIndex { get; set; }
}
