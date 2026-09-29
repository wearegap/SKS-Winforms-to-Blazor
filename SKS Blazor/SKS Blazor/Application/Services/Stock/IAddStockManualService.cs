using SKS_Blazor.Application.DTOs.Stock;
using SKS_Blazor.Application.DTOs.Core;
namespace SKS_Blazor.Application.Services.Stock;

public interface IAddStockManualService
{
    Task<List<ProductsDto>> DoSearchProductAsync(AddStockManualStateDto state, CancellationToken ct = default);
    Task<ProductsDto?> RetrieveDataProductAsync(AddStockManualStateDto state, CancellationToken ct = default);
    Task<AddStockManualStateDto> SaveAsync(AddStockManualStateDto state, object eventSender, CancellationToken ct = default);
    Task<AddStockManualStateDto> ValuesAsync(AddStockManualStateDto state, object eventSender, CancellationToken ct = default);
    Task<List<StockLogDto>> GetStockLogAsync(CancellationToken ct = default);
}
