namespace Momentum.Client.Services;

/// <summary>
/// Broadcasts cold-start retry state so UI components can show a friendly
/// "waking up the server" message while the Azure Free tier app spins up.
/// </summary>
public class ColdStartService
{
    public bool IsRetrying { get; private set; }
    public string? StatusMessage { get; private set; }

    /// <summary>Fired on the calling thread whenever retry state changes.</summary>
    public event Action? OnChanged;

    public void SetRetrying(string message)
    {
        IsRetrying = true;
        StatusMessage = message;
        OnChanged?.Invoke();
    }

    public void SetIdle()
    {
        if (!IsRetrying && StatusMessage is null) return; // already idle — skip noise
        IsRetrying = false;
        StatusMessage = null;
        OnChanged?.Invoke();
    }
}
