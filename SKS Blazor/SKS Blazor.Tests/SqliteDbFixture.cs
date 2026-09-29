using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SKS_Blazor.Infrastructure.Data;

namespace SKS_Blazor.Tests;

/// <summary>
/// A private copy of the real legacy Orders.db per test. EnsureCreated would emit the EF model's
/// schema (different column affinities and defaults) and none of the rows the date and aggregate
/// tests depend on, so the fixture copies the tracked file instead.
/// </summary>
public sealed class SqliteDbFixture : IDbContextFactory<UpgradedDbContext>, IDisposable
{
    private readonly string _path = Path.Combine(Path.GetTempPath(), $"sks-test-{Guid.NewGuid():N}.db");

    public SqliteDbFixture()
    {
        File.Copy(Path.Combine(AppContext.BaseDirectory, "TestData", "Orders.db"), _path);
    }

    public UpgradedDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<UpgradedDbContext>()
            .UseSqlite($"Data Source={_path};Mode=ReadWrite")
            .Options);

    public void Dispose()
    {
        SqliteConnection.ClearAllPools(); // pooled connections keep the file open
        File.Delete(_path);
    }
}
