using SKS_Blazor.Application.DTOs.Users;
using SKS_Blazor.Domain.Entities.Core;

namespace SKS_Blazor.Application.DTOs.Users;

/// <summary>Result DTO for user save (insert/update) operations. Port of <c>frmUsersManage.cmdSave_Click</c>.</summary>
public class UsersManageSaveResultDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public bool ShouldClose { get; set; }
    public UsersManageStateDto UpdatedState { get; set; } = new();
}
