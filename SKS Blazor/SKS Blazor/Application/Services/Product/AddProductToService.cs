using System.Collections.Specialized;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SKS_Blazor.Application.DTOs.Product;
using SKS_Blazor.Application.DTOs.Core;
using SKS_Blazor.Domain.Entities.Products;
using SKS_Blazor.Infrastructure.Data;
using SKS_Blazor.Domain.Entities.Core;

namespace SKS_Blazor.Application.Services.Product;

/// <summary>
/// Port of <c>frmAddProductTo</c> — manages associating/disassociating products
/// with a customer or provider via junction tables (ProductsByCustomer / ProductsByProvider).
/// </summary>
public class AddProductToService : IAddProductToService
{
    private readonly IDbContextFactory<UpgradedDbContext> _dbFactory;
    private readonly ILogger<AddProductToService> _logger;

    public AddProductToService(
        IDbContextFactory<UpgradedDbContext> dbFactory,
        ILogger<AddProductToService> logger)
    {
        _dbFactory = dbFactory;
        _logger = logger;
    }

    /// <inheritdoc />
    /// <remarks>Port of <c>LoadData()</c> + <c>LoadProductsById()</c>.</remarks>
    public async Task<AddProductToStateDto> LoadDataAsync(AddProductToStateDto state, CancellationToken ct = default)
    {
        // Port of LoadData() — reset state
        state.EditingData = false;
        state.SavedChanges = false;
        state.SaveResult = null;
        state.ProductsStored = new OrderedDictionary(StringComparer.OrdinalIgnoreCase);
        state.ProductsToDelete = new OrderedDictionary(StringComparer.OrdinalIgnoreCase);
        state.ProductsToAdd = new OrderedDictionary(StringComparer.OrdinalIgnoreCase);
        state.AssociatedProducts = new List<ProductsDto>();
        state.CheckedProductIds = new List<string>();

        if (state.Context == null)
        {
            _logger.LogWarning("AddProductToService.LoadDataAsync called without Context set on state.");
            return state;
        }

        var context = state.Context;

        // Port of LoadProductsById() — load existing associations from junction table
        // Legacy SQL: Select p.ProductID, p.ProductName, p.UnitPrice, p.QuantityPerUnit, p.Unit
        //             from Products as p, {Table} as pb
        //             Where pb.{ColumnName} = {Id} And pb.ProductId = p.ProductId
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        List<SKS_Blazor.Domain.Entities.Core.Products> associatedProducts;

        if (string.Equals(context.Table, "ProductsByCustomer", StringComparison.OrdinalIgnoreCase))
        {
            associatedProducts = await db.ProductsByCustomers
                .Where(pb => pb.CustomerID == context.Id)
                .Join(db.Products,
                    pb => pb.ProductID,
                    p => p.ProductID,
                    (pb, p) => p)
                .AsNoTracking()
                .ToListAsync(ct);
        }
        else if (string.Equals(context.Table, "ProductsByProvider", StringComparison.OrdinalIgnoreCase))
        {
            associatedProducts = await db.ProductsByProviders
                .Where(pb => pb.ProviderID == context.Id)
                .Join(db.Products,
                    pb => pb.ProductID,
                    p => p.ProductID,
                    (pb, p) => p)
                .AsNoTracking()
                .ToListAsync(ct);
        }
        else
        {
            _logger.LogWarning("Unknown junction table: {Table}", context.Table);
            associatedProducts = new List<SKS_Blazor.Domain.Entities.Core.Products>();
        }

        _logger.LogInformation(
            "LoadDataAsync: Found {Count} associated products for {Table} ID={Id}",
            associatedProducts.Count, context.Table, context.Id);

        // Port of legacy loop: populate productsStored and build the display list
        foreach (var p in associatedProducts)
        {
            var productCode = p.ProductID ?? string.Empty;
            if (!string.IsNullOrEmpty(productCode) && !state.ProductsStored.Contains(productCode))
            {
                // Legacy: modFunctions.AddToCollection(productsStored, productCode)
                state.ProductsStored.Add(productCode, productCode);
            }

            // Legacy columns: ProductID (col 0), ProductName (col 1), UnitPrice (col 2),
            //   then combined QuantityPerUnit + Unit (col 3)
            state.AssociatedProducts.Add(MapEntityToDto(p));
        }

        return state;
    }

    /// <inheritdoc />
    /// <remarks>Port of <c>DoSearchProduct()</c>.</remarks>
    public async Task<List<ProductsDto>> DoSearchProductAsync(AddProductToStateDto state, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        // Port of DoSearchProduct() — build filter from txtCode and txtName
        // Legacy SQL: Select ProductID, ProductName, UnitPrice, UnitsInStock, UnitsOnOrder,
        //             QuantityPerUnit, Unit from Products Where {filter}
        IQueryable<SKS_Blazor.Domain.Entities.Core.Products> query = db.Products.AsNoTracking();

        // Legacy: if (!String.IsNullOrEmpty(txtCode.Text))
        //           filter = $"ProductId LIKE '%{txtCode.Text}%'";
        if (!string.IsNullOrEmpty(state.TxtCodeText))
        {
            var codeFilter = state.TxtCodeText;
            query = query.Where(p => p.ProductID != null && EF.Functions.Like(p.ProductID, "%" + codeFilter + "%"));
        }

        // Legacy: if (!String.IsNullOrEmpty(txtName.Text))
        //           filter = $"{filter}ProductName LIKE '%{txtName.Text}%'";
        if (!string.IsNullOrEmpty(state.TxtNameText))
        {
            var nameFilter = state.TxtNameText;
            query = query.Where(p => p.ProductName != null && EF.Functions.Like(p.ProductName, "%" + nameFilter + "%"));
        }

        var entities = await query.ToListAsync(ct);

        _logger.LogInformation("DoSearchProductAsync: Found {Count} products matching criteria", entities.Count);

        // Port of legacy loop: iterate rs records, build list items
        var result = new List<ProductsDto>(entities.Count);
        foreach (var p in entities)
        {
            result.Add(MapEntityToDto(p));
        }

        return result;
    }

    /// <inheritdoc />
    /// <remarks>Port of <c>AddProductToSet()</c>.</remarks>
    public async Task<AddProductToStateDto> AddProductToSetAsync(AddProductToStateDto state, CancellationToken ct = default)
    {
        // Port of AddProductToSet()
        // Legacy: if (!(lvProducts.FocusedItem is null)) — check we have a selected product
        var currentIdProduct = state.CurrentIdProduct;
        if (string.IsNullOrEmpty(currentIdProduct))
        {
            return state;
        }

        // Legacy: search lvProductsBy for existing entry with same ProductID
        bool found = false;
        int insertIndex = 0;
        if (state.AssociatedProducts != null)
        {
            for (int i = 0; i < state.AssociatedProducts.Count; i++)
            {
                var existingId = state.AssociatedProducts[i].ProductID ?? string.Empty;
                if (string.Equals(existingId, currentIdProduct, StringComparison.OrdinalIgnoreCase))
                {
                    // Legacy: lvProductsBy.Items[lvProductsBy.Items[i].Index].Selected = true;
                    found = true;
                    break;
                }
                else if (string.Compare(existingId, currentIdProduct, StringComparison.Ordinal) > 0)
                {
                    // Legacy: break (insert position found — sorted order)
                    insertIndex = i;
                    break;
                }
                insertIndex = i + 1;
            }
        }

        if (!found)
        {
            state.EditingData = true;

            bool existsInStored = state.ProductsStored != null && state.ProductsStored.Contains(currentIdProduct);
            bool existsInToDelete = state.ProductsToDelete != null && state.ProductsToDelete.Contains(currentIdProduct);

            if (!existsInStored)
            {
                // Legacy: product was NOT originally in DB
                if (existsInToDelete)
                {
                    // Legacy: productsToDelete.Remove(currentIdProduct)
                    state.ProductsToDelete!.Remove(currentIdProduct);
                }
                else
                {
                    // Legacy: modFunctions.AddToCollection(productsToAdd, currentIdProduct)
                    state.ProductsToAdd ??= new OrderedDictionary(StringComparer.OrdinalIgnoreCase);
                    if (!state.ProductsToAdd.Contains(currentIdProduct))
                    {
                        state.ProductsToAdd.Add(currentIdProduct, currentIdProduct);
                    }
                }
            }
            else
            {
                // Legacy: product WAS originally in DB
                if (existsInToDelete)
                {
                    // Legacy: productsToDelete.Remove(currentIdProduct) — un-delete
                    state.ProductsToDelete!.Remove(currentIdProduct);
                }
            }

            // Legacy: load product details and insert into lvProductsBy at sorted position
            // x = lvProductsBy.Items.Insert(i - 1, currentIdProduct);
            // SubItem[1] = ProductName, SubItem[2] = UnitPrice,
            // SubItem[3] = QuantityPerUnit + Unit
            var productDto = await LoadProductsByIdAsync(state, ct);
            if (productDto != null)
            {
                state.AssociatedProducts ??= new List<ProductsDto>();
                if (insertIndex > state.AssociatedProducts.Count)
                {
                    insertIndex = state.AssociatedProducts.Count;
                }
                state.AssociatedProducts.Insert(insertIndex, productDto);
            }
        }

        return state;
    }

    /// <inheritdoc />
    /// <remarks>Port of <c>cmdRemove_Click()</c>.</remarks>
    public async Task<AddProductToStateDto> RemoveAsync(AddProductToStateDto state, object eventSender, CancellationToken ct = default)
    {
        // Port of cmdRemove_Click()
        // Legacy iterates lvProductsBy in REVERSE checking .Checked on each item
        if (state.AssociatedProducts == null || state.CheckedProductIds == null || state.CheckedProductIds.Count == 0)
        {
            return state;
        }

        var checkedIds = new HashSet<string>(state.CheckedProductIds, StringComparer.OrdinalIgnoreCase);

        // Legacy: for (modMain.i = lvProductsBy.Items.Count; modMain.i >= 1; modMain.i--)
        for (int i = state.AssociatedProducts.Count - 1; i >= 0; i--)
        {
            var productIdToDelete = state.AssociatedProducts[i].ProductID ?? string.Empty;

            if (!checkedIds.Contains(productIdToDelete))
            {
                continue;
            }

            // Legacy: if (lvProductsBy.Items[modMain.i - 1].Checked)
            bool existsInStored = state.ProductsStored != null && state.ProductsStored.Contains(productIdToDelete);
            bool existsInToAdd = state.ProductsToAdd != null && state.ProductsToAdd.Contains(productIdToDelete);

            if (existsInStored)
            {
                // Legacy: if (modFunctions.Exists(productsStored, productIdToDelete))
                if (existsInToAdd)
                {
                    // Legacy: if (modFunctions.Exists(productsToAdd, productIdToDelete))
                    //           productsToDelete.Remove(productIdToDelete)
                    if (state.ProductsToDelete != null && state.ProductsToDelete.Contains(productIdToDelete))
                    {
                        state.ProductsToDelete.Remove(productIdToDelete);
                    }
                }
                else
                {
                    // Legacy: modFunctions.AddToCollection(productsToDelete, productIdToDelete)
                    state.ProductsToDelete ??= new OrderedDictionary(StringComparer.OrdinalIgnoreCase);
                    if (!state.ProductsToDelete.Contains(productIdToDelete))
                    {
                        state.ProductsToDelete.Add(productIdToDelete, productIdToDelete);
                    }
                }
            }
            else
            {
                // Legacy: product was NOT originally in DB — only in pending adds
                // if (modFunctions.Exists(productsToAdd, currentIdProduct))
                //   productsToAdd.Remove(currentIdProduct)
                if (existsInToAdd)
                {
                    state.ProductsToAdd!.Remove(productIdToDelete);
                }
            }

            // Legacy: lvProductsBy.Items.RemoveAt(modMain.i - 1)
            state.AssociatedProducts.RemoveAt(i);
            state.EditingData = true;
        }

        // Clear the checked list after processing
        state.CheckedProductIds.Clear();

        await Task.CompletedTask; // maintain async contract
        return state;
    }

    /// <inheritdoc />
    /// <remarks>Port of <c>cmdSave_Click()</c>.</remarks>
    public async Task<AddProductToStateDto> SaveAsync(AddProductToStateDto state, object eventSender, CancellationToken ct = default)
    {
        // Port of cmdSave_Click()
        state.SaveResult = null;

        int toAddCount = state.ProductsToAdd?.Count ?? 0;
        int toDeleteCount = state.ProductsToDelete?.Count ?? 0;

        // Legacy: if (productsToAdd.Count == 0 && productsToDelete.Count == 0)
        //           MessageBox.Show("No data to be saved", "No data modified", ...)
        //           this.Close(); return;
        if (toAddCount == 0 && toDeleteCount == 0)
        {
            state.EditingData = false;
            state.SaveResult = new SaveResultInfo
            {
                Success = true,
                Message = "No data to be saved",
                Title = "No data modified",
                NoChanges = true
            };
            return state;
        }

        // Pre-save validation: context must be set
        if (state.Context == null)
        {
            _logger.LogError("SaveAsync called without Context on state.");
            state.SaveResult = new SaveResultInfo
            {
                Success = false,
                Message = "Internal error: missing context configuration.",
                Title = "Error"
            };
            return state;
        }

        var context = state.Context;

        // Validate table name to prevent unexpected table references
        if (!string.Equals(context.Table, "ProductsByCustomer", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(context.Table, "ProductsByProvider", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogError("SaveAsync: invalid junction table name '{Table}'", context.Table);
            state.SaveResult = new SaveResultInfo
            {
                Success = false,
                Message = $"An error has occurred: unknown table '{context.Table}'.",
                Title = "Error"
            };
            return state;
        }

        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        await using var transaction = await db.Database.BeginTransactionAsync(ct);

        int retries = 0;
        retry:
        try
        {
            // Port of: foreach (string productCodeIterator in productsToAdd.Values)
            //   modConnection.ExecuteSql($"Insert into {Table}({ColumnName}, ProductID) Values ({Id}, '{productCode}')");
            if (state.ProductsToAdd != null && state.ProductsToAdd.Count > 0)
            {
                foreach (string? productCode in state.ProductsToAdd.Values)
                {
                    if (string.IsNullOrEmpty(productCode)) continue;

                    if (string.Equals(context.Table, "ProductsByCustomer", StringComparison.OrdinalIgnoreCase))
                    {
                        db.ProductsByCustomers.Add(new ProductsByCustomer
                        {
                            CustomerID = context.Id,
                            ProductID = productCode
                        });
                    }
                    else
                    {
                        db.ProductsByProviders.Add(new ProductsByProvider
                        {
                            ProviderID = context.Id,
                            ProductID = productCode
                        });
                    }
                }
            }

            // Port of: foreach (string productCodeIterator2 in productsToDelete.Values)
            //   modConnection.ExecuteSql($"Delete from {Table} Where {ColumnName} = {Id} And ProductID = '{productCode}'");
            if (state.ProductsToDelete != null && state.ProductsToDelete.Count > 0)
            {
                var productIdsToDelete = new List<string>();
                foreach (string? productCode in state.ProductsToDelete.Values)
                {
                    if (!string.IsNullOrEmpty(productCode))
                    {
                        productIdsToDelete.Add(productCode);
                    }
                }

                if (productIdsToDelete.Count > 0)
                {
                    // Batch load entities to delete (avoid N+1)
                    if (string.Equals(context.Table, "ProductsByCustomer", StringComparison.OrdinalIgnoreCase))
                    {
                        var entitiesToRemove = await db.ProductsByCustomers
                            .Where(pb => pb.CustomerID == context.Id
                                         && pb.ProductID != null
                                         && productIdsToDelete.Contains(pb.ProductID))
                            .ToListAsync(ct);
                        db.ProductsByCustomers.RemoveRange(entitiesToRemove);
                    }
                    else
                    {
                        var entitiesToRemove = await db.ProductsByProviders
                            .Where(pb => pb.ProviderID == context.Id
                                         && pb.ProductID != null
                                         && productIdsToDelete.Contains(pb.ProductID))
                            .ToListAsync(ct);
                        db.ProductsByProviders.RemoveRange(entitiesToRemove);
                    }
                }
            }

            // Batch SaveChangesAsync (all adds and removes in one call)
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

            _logger.LogInformation(
                "SaveAsync: Added {AddCount} and removed {DeleteCount} product associations for {Table} ID={Id}",
                toAddCount, toDeleteCount, context.Table, context.Id);

            // Legacy: SavedChanges = true; editingData = false;
            state.EditingData = false;
            state.SavedChanges = true;
            state.SaveResult = new SaveResultInfo
            {
                Success = true,
                Message = "Data was successfully saved",
                Title = "New data",
                NoChanges = false
            };
        }
        catch (DbUpdateConcurrencyException ex) when (retries++ < 3)
        {
            // LOCKRETRY: retry up to 3 times on concurrency conflicts
            _logger.LogWarning(ex,
                "Concurrency conflict (attempt {Attempt}) saving product associations for {Table} ID={Id}",
                retries, context.Table, context.Id);
            await Task.Delay(100 * retries, ct);
            goto retry;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            await transaction.RollbackAsync(ct);
            _logger.LogError(ex,
                "Concurrency error after {MaxRetries} retries saving product associations for {Table} ID={Id}",
                retries, context.Table, context.Id);

            // Port of legacy error handler:
            // MessageBox.Show($"An error has occurred adding the data.\nError: ({Err.Number}) {Err.Description}", ...)
            state.SaveResult = new SaveResultInfo
            {
                Success = false,
                Message = $"A concurrency error has occurred saving the data.{Environment.NewLine}Error: {ex.Message}",
                Title = "Error"
            };
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(ct);
            _logger.LogError(ex,
                "Error saving product associations for {Table} ID={Id}",
                context.Table, context.Id);

            state.SaveResult = new SaveResultInfo
            {
                Success = false,
                Message = $"An error has occurred adding the data.{Environment.NewLine}Error: {ex.Message}",
                Title = "Error"
            };
        }

        return state;
    }

    /// <inheritdoc />
    /// <remarks>Port of <c>LoadProductsById()</c> — loads a single product by
    /// <c>state.CurrentIdProduct</c> for insertion into the associated products list.</remarks>
    public async Task<ProductsDto?> LoadProductsByIdAsync(AddProductToStateDto state, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(state.CurrentIdProduct))
        {
            return null;
        }

        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var entity = await db.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.ProductID == state.CurrentIdProduct, ct);

        if (entity == null)
        {
            _logger.LogWarning("LoadProductsByIdAsync: Product '{ProductId}' not found", state.CurrentIdProduct);
            return null;
        }

        return MapEntityToDto(entity);
    }

    /// <summary>
    /// Maps a Products entity to ProductsDto with null-coalescing for all nullable fields.
    /// </summary>
    private static ProductsDto MapEntityToDto(SKS_Blazor.Domain.Entities.Core.Products entity)
    {
        return new ProductsDto
        {
            ProductID = entity.ProductID ?? string.Empty,
            ProductName = entity.ProductName ?? string.Empty,
            UnitPrice = entity.UnitPrice,
            UnitsInStock = entity.UnitsInStock,
            UnitsOnOrder = entity.UnitsOnOrder,
            QuantityPerUnit = entity.QuantityPerUnit,
            Unit = entity.Unit ?? string.Empty,
            CategoryID = entity.CategoryID,
            Discontinued = entity.Discontinued,
            LeadTime = entity.LeadTime ?? string.Empty,
            ProductDescription = entity.ProductDescription ?? string.Empty,
            ReorderLevel = entity.ReorderLevel,
            SerialNumber = entity.SerialNumber ?? string.Empty,
            SupplierID = entity.SupplierID
        };
    }
}
