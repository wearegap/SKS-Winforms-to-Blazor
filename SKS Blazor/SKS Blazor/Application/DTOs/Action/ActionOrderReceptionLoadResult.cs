using SKS_Blazor.Application.DTOs.Action;
namespace SKS_Blazor.Application.DTOs.Action;

/// <summary>Result of loading order reception data for the action/approval screen.</summary>
public class ActionOrderReceptionLoadResult
{
    public bool OrderFound { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public ActionOrderReceptionHeaderDto Header { get; set; } = new();
    public List<ActionOrderReceptionDetailDto> Details { get; set; } = new();
    public ActionOrderReceptionStateDto Totals { get; set; } = new();
}
