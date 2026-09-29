using SKS_Blazor.Infrastructure;
using Xunit;

namespace SKS_Blazor.Tests;

public class AppStateTests
{
    [Fact]
    public void AppState_DefaultConstruction_DoesNotThrow()
    {
        var appState = new AppState();
        Assert.NotNull(appState);
    }

    [Fact]
    public void AppState_StatusMessage_CanBeSetAndRead()
    {
        var appState = new AppState
        {
            StatusMessage = "Ready"
        };

        Assert.Equal("Ready", appState.StatusMessage);
    }
}
