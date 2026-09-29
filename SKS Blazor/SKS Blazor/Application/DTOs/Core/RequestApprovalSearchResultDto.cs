#nullable disable
namespace SKS_Blazor.Application.DTOs.Core;

public class RequestApprovalSearchResultDto
{
    public List<RequestApprovalGridRowDto> Rows { get; set; } = new();
    public string StatusMessage { get; set; } = string.Empty;
    public int RecordCount { get; set; }
}
