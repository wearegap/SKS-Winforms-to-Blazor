namespace SKS_Blazor.Application.Services.About;

public interface IAboutService
{
    Task InitializeAsync(CancellationToken ct = default);
}
