namespace BlazorApp1.Services;

public class ToastService
{
    public event Action<ToastMessage>? OnShow;
    
    public void ShowSuccess(string message, string? title = null)
    {
        OnShow?.Invoke(new ToastMessage
        {
            Title = title ?? "Success",
            Message = message,
            Type = ToastType.Success
        });
    }
    
    public void ShowError(string message, string? title = null)
    {
        OnShow?.Invoke(new ToastMessage
        {
            Title = title ?? "Error",
            Message = message,
            Type = ToastType.Error
        });
    }
    
    public void ShowWarning(string message, string? title = null)
    {
        OnShow?.Invoke(new ToastMessage
        {
            Title = title ?? "Warning",
            Message = message,
            Type = ToastType.Warning
        });
    }
    
    public void ShowInfo(string message, string? title = null)
    {
        OnShow?.Invoke(new ToastMessage
        {
            Title = title ?? "Info",
            Message = message,
            Type = ToastType.Info
        });
    }
}

public class ToastMessage
{
    public string Title { get; set; } = "";
    public string Message { get; set; } = "";
    public ToastType Type { get; set; }
}

public enum ToastType
{
    Success,
    Error,
    Warning,
    Info
}
