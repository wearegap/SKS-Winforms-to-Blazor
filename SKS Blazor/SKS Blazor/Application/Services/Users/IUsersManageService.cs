using SKS_Blazor.Application.DTOs.Core;
using SKS_Blazor.Application.DTOs.Users;
using SKS_Blazor.Domain.Entities.Core;

namespace SKS_Blazor.Application.Services.Users;

/// <summary>Port of <c>frmUsersManage</c> — user account CRUD operations.</summary>
public interface IUsersManageService
{
    /// <summary>Load all users (Username, Fullname, Level). Port of <c>LoadUsers()</c>.</summary>
    Task<List<UsersDto>> LoadUsersAsync(CancellationToken ct = default);

    /// <summary>Load all users — identical to LoadUsersAsync. Port of <c>LoadUsersAvoidingWith()</c>.</summary>
    Task<List<UsersDto>> LoadUsersAvoidingWithAsync(CancellationToken ct = default);

    /// <summary>Reset form state fields. Port of <c>ClearFields()</c>.</summary>
    Task<UsersManageStateDto> ClearFieldsAsync(UsersManageStateDto state, CancellationToken ct = default);

    /// <summary>Delete a user by username. Port of <c>cmdDelete_Click</c>. eventSender is the selected username (string).</summary>
    Task DeleteAsync(object eventSender, CancellationToken ct = default);

    /// <summary>Load a user for editing. Port of <c>cmdEdit_Click</c>. eventSender is the selected username (string).</summary>
    Task<UsersManageStateDto> EditAsync(UsersManageStateDto state, object eventSender, CancellationToken ct = default);

    /// <summary>Save (insert or update) a user. Port of <c>cmdSave_Click</c>. eventSender indicates if current user is admin (bool).</summary>
    Task SaveAsync(UsersManageStateDto state, object eventSender, CancellationToken ct = default);

    /// <summary>Initialize the form — load levels, set initial state, load users. Port of <c>Form_Load</c>.</summary>
    Task<UsersManageStateDto> InitializeAsync(UsersManageStateDto state, CancellationToken ct = default);
}
