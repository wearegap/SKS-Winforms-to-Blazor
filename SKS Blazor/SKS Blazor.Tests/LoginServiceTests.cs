using Microsoft.Extensions.Logging.Abstractions;
using SKS_Blazor.Application.DTOs.Login;
using SKS_Blazor.Application.Services.Login;
using Xunit;

namespace SKS_Blazor.Tests;

/// <summary>Port of frmLogin.cmdOk_Click against the real Users table.</summary>
public class LoginServiceTests : IDisposable
{
    private readonly SqliteDbFixture _db = new();

    public void Dispose() => _db.Dispose();

    private LoginService Service() => new(_db, NullLogger<LoginService>.Instance);

    [Fact]
    public async Task ValidAdministrator_SetsUserFields()
    {
        var state = await Service().OkAsync(new LoginStateDto { UserName = "acantillo", Password = "123" });

        Assert.True(state.LoginSucceeded);
        Assert.Equal("acantillo", state.UserId);
        Assert.Equal("Allan Cantillo", state.UserFullname);
        Assert.Equal("Administrator", state.UserLevel);
        Assert.True(state.CurrentUserAdmin);
        Assert.StartsWith("User : Allan Cantillo logged at ", state.StatusMessage);
        Assert.Empty(state.Password); // not kept in circuit state
    }

    [Fact]
    public async Task Seller_IsNotAdmin()
    {
        var state = await Service().OkAsync(new LoginStateDto { UserName = "cbastos", Password = "cbastos" });

        Assert.True(state.LoginSucceeded);
        Assert.Equal("Seller", state.UserLevel);
        Assert.False(state.CurrentUserAdmin);
    }

    [Theory]
    [InlineData("acantillo", "wrong")]
    [InlineData("nobody", "123")]
    [InlineData("", "123")]
    [InlineData("acantillo", "")]
    public async Task InvalidCredentials_FailWithLegacyMessage(string user, string password)
    {
        var state = await Service().OkAsync(new LoginStateDto { UserName = user, Password = password });

        Assert.False(state.LoginSucceeded);
        Assert.Equal("Invalid 'Username' or 'Password', please try again!", state.ErrorMessage);
        Assert.Empty(state.UserId);
    }
}
