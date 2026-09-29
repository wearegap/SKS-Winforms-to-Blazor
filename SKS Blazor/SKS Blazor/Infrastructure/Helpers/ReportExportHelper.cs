using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace SKS_Blazor.Infrastructure.Helpers;

/// <summary>
/// Helper for exporting invoices, order receptions, and stock reports to PDF/Excel formats.
/// New abstraction — the legacy WinForms app did not have explicit export functionality.
/// Per architectural decision: "No charting or reporting needed; drop MSChartLib dependency entirely."
/// This class provides basic CSV/text export as a lightweight alternative without requiring
/// any third-party PDF/Excel library (no QuestPDF, no iTextSharp, no PdfSharp).
/// </summary>
public class ReportExportHelper
{
    private readonly ILogger<ReportExportHelper> _logger;

    public ReportExportHelper(ILogger<ReportExportHelper> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Exports report data to a simple text-based format (UTF-8 encoded).
    /// Since no PDF library was chosen during pre-analysis, this produces a plain-text
    /// representation suitable for browser download. If a PDF library is added later,
    /// this method should be updated to use it.
    /// </summary>
    /// <param name="reportName">The name/title of the report.</param>
    /// <param name="data">The data object to serialize. Expected to be an IEnumerable or a single DTO.</param>
    /// <returns>UTF-8 encoded byte array of the report content.</returns>
    public Task<byte[]> ExportToPdfAsync(string reportName, object data)
    {
        _logger.LogInformation("ExportToPdf requested: ReportName={ReportName}", reportName);

        var sb = new StringBuilder();
        sb.AppendLine($"Report: {reportName}");
        sb.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine(new string('-', 60));

        if (data is System.Collections.IEnumerable enumerable)
        {
            foreach (var item in enumerable)
            {
                var props = item.GetType().GetProperties();
                foreach (var prop in props)
                {
                    var value = prop.GetValue(item);
                    sb.AppendLine($"  {prop.Name}: {value ?? "(null)"}");
                }
                sb.AppendLine(new string('-', 40));
            }
        }
        else if (data is not null)
        {
            var props = data.GetType().GetProperties();
            foreach (var prop in props)
            {
                var value = prop.GetValue(data);
                sb.AppendLine($"  {prop.Name}: {value ?? "(null)"}");
            }
        }

        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        return Task.FromResult(bytes);
    }

    /// <summary>
    /// Exports report data to CSV format (UTF-8 encoded with BOM for Excel compatibility).
    /// This is the primary export format since no third-party Excel library is configured.
    /// </summary>
    /// <param name="reportName">The name/title of the report (used in logging).</param>
    /// <param name="data">The data object to serialize. Expected to be an IEnumerable of DTOs.</param>
    /// <returns>UTF-8 encoded byte array of the CSV content.</returns>
    public Task<byte[]> ExportToExcelAsync(string reportName, object data)
    {
        _logger.LogInformation("ExportToExcel (CSV) requested: ReportName={ReportName}", reportName);

        var sb = new StringBuilder();

        // UTF-8 BOM for Excel auto-detection
        if (data is System.Collections.IEnumerable enumerable)
        {
            bool headerWritten = false;
            foreach (var item in enumerable)
            {
                var props = item.GetType().GetProperties();

                if (!headerWritten)
                {
                    sb.AppendLine(string.Join(",", Array.ConvertAll(props, p => EscapeCsv(p.Name))));
                    headerWritten = true;
                }

                var values = new string[props.Length];
                for (int i = 0; i < props.Length; i++)
                {
                    var val = props[i].GetValue(item);
                    values[i] = EscapeCsv(val?.ToString() ?? string.Empty);
                }
                sb.AppendLine(string.Join(",", values));
            }
        }

        // Prepend UTF-8 BOM
        var bom = new byte[] { 0xEF, 0xBB, 0xBF };
        var content = Encoding.UTF8.GetBytes(sb.ToString());
        var result = new byte[bom.Length + content.Length];
        Buffer.BlockCopy(bom, 0, result, 0, bom.Length);
        Buffer.BlockCopy(content, 0, result, bom.Length, content.Length);

        return Task.FromResult(result);
    }

    private static string EscapeCsv(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }
        return value;
    }
}
