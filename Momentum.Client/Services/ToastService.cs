namespace Momentum.Client.Services;

public enum ToastType { Success, Error, Warning, Info }

public record ToastMessage(Guid Id, string Message, ToastType Type, int DurationMs);

public class ToastService
{
    public event Action<ToastMessage>? OnShow;

    public void Show(string message, ToastType type = ToastType.Success)
    {
        var duration = type is ToastType.Error or ToastType.Warning ? 4500 : 3000;
        OnShow?.Invoke(new ToastMessage(Guid.NewGuid(), message, type, duration));
    }
}
