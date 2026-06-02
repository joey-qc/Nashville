namespace Momentum.Client.Services;

using Momentum.Shared;

/// <summary>
/// Maps stored dimension names (Physical/Mental/Spiritual/Social/Housekeeping) to
/// user-facing display names (Body/Mind/Spirit/Connections/Responsibilities) and
/// mobile-abbreviated labels (Body/Mind/Spirit/Con/Rsp).
/// Lookup uses stable dimension ID first; falls back to stored name for resilience.
/// </summary>
public static class DimensionDisplayHelper
{
    private static readonly Dictionary<int, (string Display, string Mobile)> _byId = new()
    {
        [1] = ("Body",             "Body"),
        [2] = ("Mind",             "Mind"),
        [3] = ("Spirit",           "Spirit"),
        [4] = ("Connections",      "Con"),
        [5] = ("Responsibilities", "Rsp"),
    };

    private static readonly Dictionary<string, (string Display, string Mobile)> _byName =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["Physical"]     = ("Body",             "Body"),
            ["Mental"]       = ("Mind",             "Mind"),
            ["Spiritual"]    = ("Spirit",           "Spirit"),
            ["Social"]       = ("Connections",      "Con"),
            ["Housekeeping"] = ("Responsibilities", "Rsp"),
        };

    public static string GetDisplayName(CategoryDto dim)
    {
        if (_byId.TryGetValue(dim.Id, out var byId))   return byId.Display;
        if (_byName.TryGetValue(dim.Name, out var byN)) return byN.Display;
        return dim.Name;
    }

    public static string GetMobileLabel(CategoryDto dim)
    {
        if (_byId.TryGetValue(dim.Id, out var byId))   return byId.Mobile;
        if (_byName.TryGetValue(dim.Name, out var byN)) return byN.Mobile;
        return GetDisplayName(dim);
    }

    // Overloads for CategoryTotalDto (used on Home and Balance pages)
    public static string GetDisplayName(CategoryTotalDto dim)
    {
        if (_byId.TryGetValue(dim.CategoryId, out var byId))   return byId.Display;
        if (_byName.TryGetValue(dim.CategoryName, out var byN)) return byN.Display;
        return dim.CategoryName;
    }

    public static string GetMobileLabel(CategoryTotalDto dim)
    {
        if (_byId.TryGetValue(dim.CategoryId, out var byId))   return byId.Mobile;
        if (_byName.TryGetValue(dim.CategoryName, out var byN)) return byN.Mobile;
        return GetDisplayName(dim);
    }
}
