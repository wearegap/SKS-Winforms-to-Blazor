using Microsoft.EntityFrameworkCore;
using SKS_Blazor.Application.DTOs.Core;
using SKS_Blazor.Application.DTOs.Providers;
using SKS_Blazor.Infrastructure.Data;
using ProviderEntity = SKS_Blazor.Domain.Entities.Core.Providers;

namespace SKS_Blazor.Application.Services.Providers;

/// <summary>
/// Port of <c>frmProviders</c> (frmProviders.cs). The legacy form bound a data control
/// (dcProviders) to the Providers table; these methods replace its Recordset operations.
/// Field validation stays in the page, which ports the legacy <c>TextBoxEmpty(txtField[n])</c> checks.
/// </summary>
public class ProvidersService : IProvidersService
{
    private readonly IDbContextFactory<UpgradedDbContext> _dbFactory;
    private readonly ILogger<ProvidersService> _logger;

    public ProvidersService(IDbContextFactory<UpgradedDbContext> dbFactory, ILogger<ProvidersService> logger)
    {
        _dbFactory = dbFactory;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<List<ProvidersDto>> GetAllProvidersAsync(CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var entities = await db.Providers.AsNoTracking().OrderBy(p => p.ProviderID).ToListAsync(ct);
        return entities.Select(MapToDto).ToList();
    }

    /// <inheritdoc />
    public async Task<List<ProvidersDto>> SearchProvidersByNameAsync(string value, CancellationToken ct = default)
    {
        var term = (value ?? string.Empty).Trim();
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        // Legacy: Select * from Providers where ProviderName LIKE 'value%' (SQLite LIKE is case-insensitive for ASCII).
        var entities = await db.Providers.AsNoTracking()
            .Where(p => p.ProviderName != null && EF.Functions.Like(p.ProviderName, term + "%"))
            .OrderBy(p => p.ProviderID)
            .ToListAsync(ct);
        _logger.LogInformation("Provider search '{Term}' returned {Count} records", term, entities.Count);
        return entities.Select(MapToDto).ToList();
    }

    /// <inheritdoc />
    public async Task<ProvidersDto> CreateProviderAsync(ProvidersDto provider, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(provider);
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var entity = new ProviderEntity();
        CopyToEntity(provider, entity);
        db.Providers.Add(entity);
        await db.SaveChangesAsync(ct);
        _logger.LogInformation("Provider {ProviderID} ({ProviderName}) created", entity.ProviderID, entity.ProviderName);
        return MapToDto(entity);
    }

    /// <inheritdoc />
    public async Task UpdateProviderAsync(ProvidersDto provider, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(provider);
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var entity = await db.Providers.FirstOrDefaultAsync(p => p.ProviderID == provider.ProviderID, ct)
            ?? throw new InvalidOperationException(
                $"Provider {provider.ProviderID} was not found. It may have been deleted by another user.");
        CopyToEntity(provider, entity);
        await db.SaveChangesAsync(ct);
        _logger.LogInformation("Provider {ProviderID} ({ProviderName}) updated", entity.ProviderID, entity.ProviderName);
    }

    /// <inheritdoc />
    public async Task DeleteProviderAsync(int providerId, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var entity = await db.Providers.FirstOrDefaultAsync(p => p.ProviderID == providerId, ct)
            ?? throw new InvalidOperationException($"Provider {providerId} was not found. It may have already been deleted.");

        // Orders.db has no FK constraints, so guard the references the legacy data control would have orphaned.
        if (await db.OrderReceptions.AsNoTracking().AnyAsync(o => o.ProviderID == providerId, ct))
            throw new InvalidOperationException(
                $"Cannot delete provider {providerId} because order receptions reference it.");

        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var associations = await db.ProductsByProviders.Where(x => x.ProviderID == providerId).ToListAsync(ct);
        db.ProductsByProviders.RemoveRange(associations);
        db.Providers.Remove(entity);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        _logger.LogInformation("Provider {ProviderID} deleted with {Count} product associations", providerId, associations.Count);
    }

    /// <inheritdoc />
    public Task<ProvidersStateDto> SaveAsync(ProvidersStateDto state, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(state);
        state.EditMode = false;
        state.NewMode = false;
        return Task.FromResult(state);
    }

    private static void CopyToEntity(ProvidersDto dto, ProviderEntity entity)
    {
        entity.ProviderName = dto.ProviderName;
        entity.ContactFirstName = dto.ContactFirstName;
        entity.ContactLastName = dto.ContactLastName;
        entity.ContactTitle = dto.ContactTitle;
        entity.Address = dto.Address;
        entity.City = dto.City;
        entity.StateOrProvince = dto.StateOrProvince;
        entity.PostalCode = dto.PostalCode;
        entity.CountryRegion = dto.CountryRegion;
        entity.PhoneNumber = dto.PhoneNumber;
        entity.Extension = dto.Extension;
        entity.FaxNumber = dto.FaxNumber;
        entity.EmailAddress = dto.EmailAddress;
        entity.PaymentTerms = dto.PaymentTerms;
        entity.Notes = dto.Notes;
    }

    private static ProvidersDto MapToDto(ProviderEntity e) => new()
    {
        ProviderID = e.ProviderID,
        ProviderName = e.ProviderName ?? string.Empty,
        ContactFirstName = e.ContactFirstName ?? string.Empty,
        ContactLastName = e.ContactLastName ?? string.Empty,
        ContactTitle = e.ContactTitle ?? string.Empty,
        Address = e.Address ?? string.Empty,
        City = e.City ?? string.Empty,
        StateOrProvince = e.StateOrProvince ?? string.Empty,
        PostalCode = e.PostalCode ?? string.Empty,
        CountryRegion = e.CountryRegion ?? string.Empty,
        PhoneNumber = e.PhoneNumber ?? string.Empty,
        Extension = e.Extension ?? string.Empty,
        FaxNumber = e.FaxNumber ?? string.Empty,
        EmailAddress = e.EmailAddress ?? string.Empty,
        PaymentTerms = e.PaymentTerms ?? string.Empty,
        Notes = e.Notes ?? string.Empty
    };
}
