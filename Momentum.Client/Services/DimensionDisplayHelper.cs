namespace Momentum.Client.Services;

using Momentum.Shared;

/// <summary>
/// Returns dimension display names and mobile-abbreviated labels.
/// Persisted names (Body/Mind/Spirit/Connections/Responsibilities) now match display names,
/// so GetDisplayName returns dim.Name directly — no translation needed.
/// Mobile abbreviations are applied only to the two longer names:
///   Connections → Con, Responsibilities → Rsp.
/// All other names are short enough to display unabbreviated on mobile.
/// </summary>
public static class DimensionDisplayHelper
{
    // Keyed by stable dimension ID so abbreviations survive any future name adjustments.
    private static readonly Dictionary<int, string> _mobileLabel = new()
    {
        [4] = "Con",
        [5] = "Rsp",
    };

    public static string GetDisplayName(CategoryDto dim) => dim.Name;

    public static string GetMobileLabel(CategoryDto dim) =>
        _mobileLabel.TryGetValue(dim.Id, out var label) ? label : dim.Name;

    // Overloads for CategoryTotalDto (used on Home and Balance pages)
    public static string GetDisplayName(CategoryTotalDto dim) => dim.CategoryName;

    public static string GetMobileLabel(CategoryTotalDto dim) =>
        _mobileLabel.TryGetValue(dim.CategoryId, out var label) ? label : dim.CategoryName;
}
