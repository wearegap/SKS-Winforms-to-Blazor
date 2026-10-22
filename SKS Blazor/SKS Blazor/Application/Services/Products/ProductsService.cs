using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SKS_Blazor.Application.DTOs.Core;
using SKS_Blazor.Application.DTOs.Products;
using SKS_Blazor.Infrastructure.Data;
using SKS_Blazor.Domain.Entities.Core;
using SKS_Blazor.Domain.Entities.Products;

namespace SKS_Blazor.Application.Services.Products;

/// <summary>
/// Port of <c>frmProducts</c> — Products management business logic.
/// Handles CRUD operations for the Products table, category resolution,
/// and junction table queries for ProductsByCustomer / ProductsByProvider.
/// </summary>
public class ProductsService : IProductsService
{
    private readonly IDbContextFactory<UpgradedDbContext> _dbFactory;
    private readonly ILogger<ProductsService> _logger;

    public ProductsService(
        IDbContextFactory<UpgradedDbContext> dbFactory,
        ILogger<ProductsService> logger)
    {
        _dbFactory = dbFactory;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<ProductsStateDto> CategoryAsync(
        ProductsStateDto state,
        object eventSender,
        CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        // Load categories ordered by name (mirrors modFunctions.LoadCombo("Categories", cmbCategory, "CategoryName", "CategoryID"))
        var categories = await db.Categories
            .AsNoTracking()
            .OrderBy(c => c.CategoryName)
            .ToListAsync(ct);

        var senderName = eventSender?.ToString() ?? string.Empty;

        if (string.Equals(senderName, "ComboChanged", StringComparison.OrdinalIgnoreCase))
        {
            // Port of cmbCategory_SelectedIndexChanged:
            if (categories.Count == 0 || state.CmbCategorySelectedIndex < 0 || state.CmbCategorySelectedIndex >= categories.Count)
            {
                return state;
            }

            var selectedCategory = categories[state.CmbCategorySelectedIndex];
            state.TxtCategoryText = selectedCategory.CategoryID.ToString(CultureInfo.InvariantCulture);
        }
        else if (string.Equals(senderName, "TextChanged", StringComparison.OrdinalIgnoreCase))
        {
            // Port of txtCategory_TextChanged:
            if (string.IsNullOrEmpty(state.TxtCategoryText))
            {
                state.CmbCategorySelectedIndex = -1;
                return state;
            }

            if (!double.TryParse(state.TxtCategoryText, NumberStyles.Any, CultureInfo.InvariantCulture, out var targetId))
            {
                state.CmbCategorySelectedIndex = -1;
                return state;
            }

            int foundIndex = -1;
            for (int i = 0; i < categories.Count; i++)
            {
                if (categories[i].CategoryID == (int)targetId)
                {
                    foundIndex = i;
                    break;
                }
            }

            state.CmbCategorySelectedIndex = foundIndex >= 0 ? foundIndex : -1;
        }

        _logger.LogDebug("CategoryAsync completed. Sender={Sender}, SelectedIndex={Index}, CategoryText={Text}",
            senderName, state.CmbCategorySelectedIndex, state.TxtCategoryText);

        return state;
    }

    /// <inheritdoc />
    public async Task SaveProductAsync(ProductSaveDto dto, CancellationToken ct = default)
    {
        if (dto == null)
            throw new ArgumentNullException(nameof(dto));

        // Pre-save validation: ProductID is the PK and is TEXT NOT NULL — must be provided
        if (string.IsNullOrWhiteSpace(dto.ProductID))
        {
            throw new InvalidOperationException("Product ID is required.");
        }

        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        await using var transaction = await db.Database.BeginTransactionAsync(ct);

        int retries = 0;
        retry:
        try
        {
            if (dto.IsNew)
            {
                // Port of "Add" case:
                var existingCheck = await db.Products
                    .AsNoTracking()
                    .AnyAsync(p => p.ProductID == dto.ProductID, ct);

                if (existingCheck)
                {
                    throw new InvalidOperationException(
                        $"A product with ID '{dto.ProductID}' already exists.");
                }

                var newProduct = new Domain.Entities.Core.Products
                {
                    ProductID = dto.ProductID,
                    ProductName = dto.ProductName,
                    ProductDescription = dto.ProductDescription,
                    CategoryID = dto.CategoryID,
                    UnitPrice = dto.UnitPrice,
                    QuantityPerUnit = dto.QuantityPerUnit,
                    UnitsInStock = dto.UnitsInStock ?? 0,
                    UnitsOnOrder = dto.UnitsOnOrder ?? 0,
                    Discontinued = dto.Discontinued,
                    LeadTime = dto.LeadTime,
                    SerialNumber = dto.SerialNumber,
                    SupplierID = dto.SupplierID,
                    Unit = dto.Unit,
                    ReorderLevel = dto.ReorderLevel
                };

                db.Products.Add(newProduct);
                await db.SaveChangesAsync(ct);

                _logger.LogInformation("New product created: {ProductID} - {ProductName}",
                    dto.ProductID, dto.ProductName);
            }
            else
            {
                // Port of "Save" case for existing (Edit mode):
                var existing = await db.Products
                    .FirstOrDefaultAsync(p => p.ProductID == dto.ProductID, ct);

                if (existing == null)
                {
                    throw new InvalidOperationException(
                        $"Product '{dto.ProductID}' not found for update.");
                }

                existing.ProductName = dto.ProductName;
                existing.ProductDescription = dto.ProductDescription;
                existing.CategoryID = dto.CategoryID;
                existing.UnitPrice = dto.UnitPrice;
                existing.QuantityPerUnit = dto.QuantityPerUnit;
                existing.UnitsInStock = dto.UnitsInStock;
                existing.UnitsOnOrder = dto.UnitsOnOrder;
                existing.Discontinued = dto.Discontinued;
                existing.LeadTime = dto.LeadTime;
                existing.SerialNumber = dto.SerialNumber;
                existing.SupplierID = dto.SupplierID;
                existing.Unit = dto.Unit;
                existing.ReorderLevel = dto.ReorderLevel;

                await db.SaveChangesAsync(ct);

                _logger.LogInformation("Product updated: {ProductID} - {ProductName}",
                    dto.ProductID, dto.ProductName);
            }

            await transaction.CommitAsync(ct);
        }
        catch (DbUpdateConcurrencyException ex) when (retries++ < 3)
        {
            _logger.LogWarning(ex,
                "Concurrency conflict saving product {ProductID}, retry {Retry}/3",
                dto.ProductID, retries);

            foreach (var entry in ex.Entries)
            {
                await entry.ReloadAsync(ct);
            }

            goto retry;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            await transaction.RollbackAsync(ct);
            _logger.LogError(ex,
                "Concurrency conflict saving product {ProductID} — retries exhausted",
                dto.ProductID);
            throw new InvalidOperationException(
                "The product was modified by another user. Please reload and try again.", ex);
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            await transaction.RollbackAsync(ct);
            _logger.LogError(ex, "Error saving product {ProductID}", dto.ProductID);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task DeleteProductAsync(string productId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(productId))
        {
            throw new ArgumentException("Product ID is required for deletion.", nameof(productId));
        }

        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        await using var transaction = await db.Database.BeginTransactionAsync(ct);

        int retries = 0;
        retry:
        try
        {
            var product = await db.Products
                .FirstOrDefaultAsync(p => p.ProductID == productId, ct);

            if (product == null)
            {
                _logger.LogWarning("Product {ProductID} not found for deletion — may have been deleted already",
                    productId);
                await transaction.RollbackAsync(ct);
                return;
            }

            // Also clean up junction table entries referencing this product
            var customerLinks = await db.ProductsByCustomers
                .Where(pc => pc.ProductID == productId)
                .ToListAsync(ct);

            if (customerLinks.Count > 0)
            {
                db.ProductsByCustomers.RemoveRange(customerLinks);
                _logger.LogInformation("Removing {Count} ProductsByCustomer links for product {ProductID}",
                    customerLinks.Count, productId);
            }

            var providerLinks = await db.ProductsByProviders
                .Where(pp => pp.ProductID == productId)
                .ToListAsync(ct);

            if (providerLinks.Count > 0)
            {
                db.ProductsByProviders.RemoveRange(providerLinks);
                _logger.LogInformation("Removing {Count} ProductsByProvider links for product {ProductID}",
                    providerLinks.Count, productId);
            }

            db.Products.Remove(product);
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

            _logger.LogInformation("Product deleted: {ProductID}", productId);
        }
        catch (DbUpdateConcurrencyException ex) when (retries++ < 3)
        {
            _logger.LogWarning(ex,
                "Concurrency conflict deleting product {ProductID}, retry {Retry}/3",
                productId, retries);

            foreach (var entry in ex.Entries)
            {
                await entry.ReloadAsync(ct);
            }

            goto retry;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            await transaction.RollbackAsync(ct);
            _logger.LogError(ex,
                "Concurrency conflict deleting product {ProductID} — retries exhausted",
                productId);
            throw new InvalidOperationException(
                "The product was modified by another user. Please reload and try again.", ex);
        }
        catch (Exception ex) when (ex is not InvalidOperationException and not ArgumentException)
        {
            await transaction.RollbackAsync(ct);
            _logger.LogError(ex, "Error deleting product {ProductID}", productId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<List<ProductsByCustomerDto>> GetProductsByCustomerAsync(CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var entities = await db.ProductsByCustomers
            .AsNoTracking()
            .ToListAsync(ct);

        var results = new List<ProductsByCustomerDto>(entities.Count);
        foreach (var e in entities)
        {
            results.Add(new ProductsByCustomerDto
            {
                ID = e.ID,
                ProductID = e.ProductID ?? string.Empty,
                CustomerID = e.CustomerID
            });
        }

        _logger.LogDebug("Loaded {Count} ProductsByCustomer records", results.Count);
        return results;
    }

    /// <inheritdoc />
    public async Task<List<ProductsByProviderDto>> GetProductsByProviderAsync(CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var entities = await db.ProductsByProviders
            .AsNoTracking()
            .ToListAsync(ct);

        var results = new List<ProductsByProviderDto>(entities.Count);
        foreach (var e in entities)
        {
            results.Add(new ProductsByProviderDto
            {
                ID = e.ID,
                ProductID = e.ProductID ?? string.Empty,
                ProviderID = e.ProviderID
            });
        }

        _logger.LogDebug("Loaded {Count} ProductsByProvider records", results.Count);
        return results;
    }

    /// <inheritdoc />
    public async Task<List<CategoriesDto>> LoadCategoriesAsync(CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var entities = await db.Categories
            .AsNoTracking()
            .OrderBy(c => c.CategoryName)
            .ToListAsync(ct);

        var results = new List<CategoriesDto>(entities.Count);
        foreach (var e in entities)
        {
            results.Add(new CategoriesDto
            {
                CategoryID = e.CategoryID,
                CategoryName = e.CategoryName ?? string.Empty
            });
        }

        _logger.LogDebug("Loaded {Count} categories", results.Count);
        return results;
    }

    /// <inheritdoc />
    public async Task<List<ProductsDto>> LoadProductsAsync(CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var entities = await db.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .OrderBy(p => p.ProductName)
            .ToListAsync(ct);

        var results = new List<ProductsDto>(entities.Count);
        foreach (var e in entities)
        {
            results.Add(MapEntityToDto(e));
        }

        _logger.LogDebug("Loaded {Count} products", results.Count);
        return results;
    }

    /// <inheritdoc />
    public async Task<ProductsDto?> GetProductByIdAsync(string productId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(productId))
            return null;

        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var entity = await db.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.ProductID == productId, ct);

        if (entity == null)
        {
            _logger.LogDebug("Product {ProductID} not found", productId);
            return null;
        }

        return MapEntityToDto(entity);
    }

    /// <summary>
    /// Maps a Products entity to a ProductsDto. Materialized in-memory — safe to coalesce nullables.
    /// </summary>
    private static ProductsDto MapEntityToDto(Domain.Entities.Core.Products e)
    {
        return new ProductsDto
        {
            ProductID = e.ProductID,
            ProductName = e.ProductName ?? string.Empty,
            ProductDescription = e.ProductDescription ?? string.Empty,
            CategoryID = e.CategoryID,
            UnitPrice = e.UnitPrice,
            QuantityPerUnit = e.QuantityPerUnit,
            UnitsInStock = e.UnitsInStock,
            UnitsOnOrder = e.UnitsOnOrder,
            ReorderLevel = e.ReorderLevel,
            Discontinued = e.Discontinued,
            LeadTime = e.LeadTime ?? string.Empty,
            SerialNumber = e.SerialNumber ?? string.Empty,
            SupplierID = e.SupplierID,
            Unit = e.Unit ?? string.Empty
        };
    }
}
