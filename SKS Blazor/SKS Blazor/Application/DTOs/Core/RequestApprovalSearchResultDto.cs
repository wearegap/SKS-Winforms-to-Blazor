#nullable disable
namespace SKS_Blazor.Application.DTOs.Core;

public class RequestApprovalSearchResultDto
{
    public string Rows { get; set; } = string.Empty;
    public string StatusMessage { get; set; } = string.Empty;
    public int RecordCount { get; set; }
}
