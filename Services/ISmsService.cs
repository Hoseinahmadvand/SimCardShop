namespace SimCardShop.Services;

public record SmsSendResult(bool Success, string? ErrorMessage = null);

/// <summary>
/// انتزاع ارسال پیامک — طراحی‌شده تا بعداً بتوان هر پنل پیامکی واقعی
/// (کاوه‌نگار، ملی‌پیامک، فراپیامک و ...) را با پیاده‌سازی این اینترفیس متصل کرد.
/// </summary>
public interface ISmsService
{
    Task<SmsSendResult> SendAsync(string mobile, string message, CancellationToken cancellationToken = default);
}
