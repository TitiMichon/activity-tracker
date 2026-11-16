using System.Globalization;
using System.Text.RegularExpressions;
using ActivityTracker.Core.Models;

namespace ActivityTracker.Core.Services;

/// <summary>Resolves how an entry is displayed: entry override → option → activity.</summary>
public static partial class EntryAppearance
{
    public static string Icon(Activity activity, Entry entry) =>
        NonEmpty(entry.Icon) ?? NonEmpty(activity.FindOption(entry.OptionId)?.Icon) ?? activity.Icon;

    public static string Color(Activity activity, Entry entry) =>
        NonEmpty(entry.Color) ?? NonEmpty(activity.FindOption(entry.OptionId)?.Color) ?? activity.Color;

    /// <summary>Short value text, e.g. "30 min", "Great" or "✓".</summary>
    public static string ValueText(Activity activity, Entry entry) => activity.Kind switch
    {
        ActivityKind.Quantity => $"{(entry.Value ?? 0).ToString("0.##", CultureInfo.InvariantCulture)} {activity.Unit}".TrimEnd(),
        ActivityKind.Scale => activity.FindOption(entry.OptionId)?.Label ?? "?",
        _ => "✓",
    };

    public static bool IsHexColor(string? value) => value is not null && HexColor().IsMatch(value);

    /// <summary>Black or white, whichever reads best on the given background.</summary>
    public static string ContrastText(string hex)
    {
        if (!IsHexColor(hex))
        {
            return "#000000";
        }

        var h = hex.TrimStart('#');
        if (h.Length == 3)
        {
            h = string.Concat(h.Select(c => $"{c}{c}"));
        }

        var r = int.Parse(h[..2], NumberStyles.HexNumber);
        var g = int.Parse(h[2..4], NumberStyles.HexNumber);
        var b = int.Parse(h[4..6], NumberStyles.HexNumber);
        var luminance = (0.299 * r) + (0.587 * g) + (0.114 * b);
        return luminance > 160 ? "#111111" : "#ffffff";
    }

    private static string? NonEmpty(string? s) => string.IsNullOrWhiteSpace(s) ? null : s;

    [GeneratedRegex("^#([0-9a-fA-F]{3}|[0-9a-fA-F]{6})$")]
    private static partial Regex HexColor();
}
