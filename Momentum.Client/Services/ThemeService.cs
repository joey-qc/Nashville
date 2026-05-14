namespace Momentum.Client.Services;

public class ThemeService
{
    public bool IsDarkMode { get; private set; }
    public event Action? ThemeChanged;

    public void Apply(string theme)
    {
        var dark = theme == "dark";
        if (IsDarkMode == dark) return;
        IsDarkMode = dark;
        ThemeChanged?.Invoke();
    }
}
