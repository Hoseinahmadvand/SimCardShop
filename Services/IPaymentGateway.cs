namespace SimCardShop.Services;

public record PaymentRequestResult(bool Success, string? Authority = null, string? PaymentUrl = null, string? ErrorMessage = null);

public record PaymentVerifyResult(bool Success, string? RefId = null, string? ErrorMessage = null);

/// <summary>
/// انتزاع درگاه پرداخت — طراحی‌شده تا هر درگاه ایرانی (زرین‌پال، آیدی‌پی، زیبال و ...)
/// بتواند بعداً با پیاده‌سازی همین اینترفیس جایگزین یا به‌عنوان گزینه دوم اضافه شود.
/// </summary>
public interface IPaymentGateway
{
    /// <summary>نام درگاه، برای ثبت روی سفارش (مثلاً "ZarinPal")</summary>
    string Name { get; }

    /// <summary>
    /// ایجاد یک تراکنش پرداخت جدید و دریافت آدرس هدایت کاربر به درگاه.
    /// مبلغ به تومان دریافت می‌شود؛ تبدیل واحد (مثلاً به ریال) داخل پیاده‌سازی هر درگاه انجام می‌شود.
    /// </summary>
    Task<PaymentRequestResult> RequestPaymentAsync(
        long amountToman,
        string description,
        string callbackUrl,
        string? mobile = null,
        string? email = null,
        CancellationToken cancellationToken = default);

    /// <summary>تایید نهایی یک تراکنش پس از بازگشت کاربر از درگاه</summary>
    Task<PaymentVerifyResult> VerifyPaymentAsync(
        string authority,
        long amountToman,
        CancellationToken cancellationToken = default);
}
