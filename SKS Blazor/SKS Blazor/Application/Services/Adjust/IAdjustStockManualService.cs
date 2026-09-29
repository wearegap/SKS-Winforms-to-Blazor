using SKS_Blazor.Application.DTOs.Core;
using SKS_Blazor.Application.DTOs.Adjust;
namespace SKS_Blazor.Application.Services.Adjust;

public interface IAdjustStockManualService
{
    Task<List<ProductsDto>> DoSearchProductAsync(AdjustStockManualStateDto state, CancellationToken ct = default);
    Task<List<StocksDto>> DoSearchStocksAsync(AdjustStockManualStateDto state, CancellationToken ct = default);
    Task<AdjustStockManualStateDto> SaveAsync(AdjustStockManualStateDto state, object eventSender, CancellationToken ct = default);
    Task<AdjustStockManualStateDto> ValuesAsync(AdjustStockManualStateDto state, object eventSender, CancellationToken ct = default);
}
