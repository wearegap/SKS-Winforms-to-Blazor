using SKS_Blazor.Domain.Entities.Core;
namespace SKS_Blazor.Application.DTOs.Users;

/// <summary>Result DTO for user deletion operations. Port of <c>frmUsersManage.cmdDelete_Click</c>.</summary>
public class UsersManageDeleteResultDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}
