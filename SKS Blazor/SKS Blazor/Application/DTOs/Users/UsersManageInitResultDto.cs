using SKS_Blazor.Application.DTOs.Core;
using SKS_Blazor.Application.DTOs.Users;
using SKS_Blazor.Domain.Entities.Core;

namespace SKS_Blazor.Application.DTOs.Users;

/// <summary>Result DTO for user management initialization. Port of <c>frmUsersManage.Form_Load</c>.</summary>
public class UsersManageInitResultDto
{
    public List<LevelsDto> Levels { get; set; } = new();
    public UsersManageStateDto State { get; set; } = new();
    public List<UsersDto> Users { get; set; } = new();
}
