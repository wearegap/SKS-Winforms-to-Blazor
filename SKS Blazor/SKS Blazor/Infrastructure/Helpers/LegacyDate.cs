using System.Globalization;

namespace SKS_Blazor.Infrastructure.Helpers;

/// <summary>
/// The one boundary for the TEXT date columns in Orders.db.
/// <para>
/// The legacy data holds three shapes in the same columns: Unix epoch milliseconds as a 13-digit
/// string (the seed rows, e.g. <c>1361232000000</c>), <c>MM/dd/yyyy</c> (written by frmOrderRequest via
/// <c>ToString("MM/dd/yyyy")</c>), and <c>M/d/yyyy</c> (written through <c>DateTimeHelper.ToString(DateTime.Today)</c>).
/// Readers must parse all of them; writers use <see cref="StorageFormat"/> so new rows match what the
/// legacy app wrote last. Never compare these columns as strings in SQL — parse in memory instead.
/// </para>
/// </summary>
public static class LegacyDate
{
    /// <summary>Format for new writes, invariant culture (legacy frmOrderRequest.cs:182).</summary>
    public const string StorageFormat = "MM/dd/yyyy";

    private static readonly string[] KnownFormats =
    {
        "MM/dd/yyyy", "M/d/yyyy", "yyyy-MM-dd",
        "MM/dd/yyyy HH:mm:ss", "M/d/yyyy h:mm:ss tt", "yyyy-MM-dd HH:mm:ss"
    };

    /// <summary>Parses any legacy shape to a date (time dropped). Returns null for empty or unrecognised values.</summary>
    public static DateTime? Parse(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        var s = raw.Trim();

        // Epoch milliseconds. Seed values are UTC midnights; converting to local time would shift the day
        // in the Americas, so take the UTC calendar date.
        if (s.Length is 12 or 13 && s.All(char.IsDigit) && long.TryParse(s, NumberStyles.None, CultureInfo.InvariantCulture, out var ms))
            return DateTimeOffset.FromUnixTimeMilliseconds(ms).UtcDateTime.Date;

        if (DateTime.TryParseExact(s, KnownFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
            return d.Date;

        if (DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out d))
            return d.Date;

        return null;
    }

    /// <summary>Formats a date for storage.</summary>
    public static string Format(DateTime value) => value.ToString(StorageFormat, CultureInfo.InvariantCulture);

    /// <summary>Today's date in storage format.</summary>
    public static string Today() => Format(DateTime.Today);

    /// <summary>Stored value normalised for display; unparseable values are shown as stored.</summary>
    public static string Display(string? raw) => Parse(raw) is { } d ? Format(d) : raw ?? string.Empty;

    /// <summary>True when the stored value parses and falls within the optional inclusive bounds.</summary>
    public static bool InRange(string? raw, DateTime? from, DateTime? to)
    {
        if (Parse(raw) is not { } d) return false;
        if (from is { } f && d < f.Date) return false;
        if (to is { } t && d > t.Date) return false;
        return true;
    }
}
