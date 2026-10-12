using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using SimCardShop.Data;

namespace SimCardShop.Services;

/// <summary>
/// اتصال به درگاه پرداخت زرین‌پال از طریق REST API نسخه ۴ (JSON).
/// مستندات رسمی: https://www.zarinpal.com/docs/paymentGateway/
///
/// نکات پیاده‌سازی:
/// - قیمت‌ها در کل پروژه به «تومان» ذخیره و نمایش داده می‌شوند، اما API زرین‌پال مبلغ را
///   به «ریال» دریافت می‌کند؛ به همین دلیل هنگام ارسال، مبلغ ضربدر ۱۰ می‌شود.
/// - در صورت خالی بودن Merchant ID در تنظیمات سایت، درخواست با پیام مشخص رد می‌شود
///   تا صفحه فراخوان بتواند به‌صورت شفاف به کاربر/مدیر اطلاع دهد (بدون کرش کردن سایت).
/// - کد ۱۰۰ در پاسخ زرین‌پال یعنی موفقیت. کد ۱۰۱ در تایید تراکنش یعنی «قبلاً تایید شده»
///   که آن هم موفق تلقی می‌شود (مثلاً اگر کاربر صفحه بازگشت را رفرش کند).
/// </summary>
public class ZarinPalGateway : IPaymentGateway
{
    private const string ProdBase = "https://payment.zarinpal.com/pg/v4/payment";
    private const string SandboxBase = "https://sandbox.zarinpal.com/pg/v4/payment";
    private const string ProdStartPay = "https://payment.zarinpal.com/pg/StartPay";
    private const string SandboxStartPay = "https://sandbox.zarinpal.com/pg/StartPay";

    private readonly ApplicationDbContext _db;
    private readonly HttpClient _httpClient;
    private readonly ILogger<ZarinPalGateway> _logger;

    public string Name => "ZarinPal";

    public ZarinPalGateway(ApplicationDbContext db, HttpClient httpClient, ILogger<ZarinPalGateway> logger)
    {
        _db = db;
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<PaymentRequestResult> RequestPaymentAsync(
        long amountToman, string description, string callbackUrl,
        string? mobile = null, string? email = null, CancellationToken cancellationToken = default)
    {
        var settings = await _db.SiteSettings.AsNoTracking().FirstOrDefaultAsync(cancellationToken);
        var merchantId = settings?.ZarinPalMerchantId;

        if (string.IsNullOrWhiteSpace(merchantId))
        {
            return new PaymentRequestResult(false, ErrorMessage: "درگاه پرداخت هنوز در تنظیمات سایت پیکربندی نشده است.");
        }

        var sandbox = settings!.ZarinPalSandbox;
        var baseUrl = sandbox ? SandboxBase : ProdBase;
        var startPayBase = sandbox ? SandboxStartPay : ProdStartPay;

        try
        {
            var payload = new
            {
                merchant_id = merchantId,
                amount = amountToman * 10, // تبدیل تومان به ریال برای زرین‌پال
                callback_url = callbackUrl,
                description,
                metadata = new { mobile, email }
            };

            var response = await _httpClient.PostAsJsonAsync($"{baseUrl}/request.json", payload, cancellationToken);
            var result = await response.Content.ReadFromJsonAsync<ZarinPalRequestResponse>(cancellationToken: cancellationToken);

            if (result?.Data?.Code == 100 && !string.IsNullOrEmpty(result.Data.Authority))
            {
                var authority = result.Data.Authority;
                return new PaymentRequestResult(true, Authority: authority, PaymentUrl: $"{startPayBase}/{authority}");
            }

            _logger.LogWarning("ZarinPal payment request failed. Code={Code} Response={@Result}", result?.Data?.Code, result);
            return new PaymentRequestResult(false, ErrorMessage: $"درخواست پرداخت توسط درگاه رد شد (کد: {result?.Data?.Code}).");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در برقراری ارتباط با درگاه پرداخت زرین‌پال");
            return new PaymentRequestResult(false, ErrorMessage: "خطا در اتصال به درگاه پرداخت. لطفاً چند لحظه دیگر دوباره تلاش کنید.");
        }
    }

    public async Task<PaymentVerifyResult> VerifyPaymentAsync(
        string authority, long amountToman, CancellationToken cancellationToken = default)
    {
        var settings = await _db.SiteSettings.AsNoTracking().FirstOrDefaultAsync(cancellationToken);
        var merchantId = settings?.ZarinPalMerchantId;

        if (string.IsNullOrWhiteSpace(merchantId))
        {
            return new PaymentVerifyResult(false, ErrorMessage: "درگاه پرداخت پیکربندی نشده است.");
        }

        var baseUrl = settings!.ZarinPalSandbox ? SandboxBase : ProdBase;

        try
        {
            var payload = new
            {
                merchant_id = merchantId,
                amount = amountToman * 10,
                authority
            };

            var response = await _httpClient.PostAsJsonAsync($"{baseUrl}/verify.json", payload, cancellationToken);
            var result = await response.Content.ReadFromJsonAsync<ZarinPalVerifyResponse>(cancellationToken: cancellationToken);

            // کد ۱۰۰: پرداخت با موفقیت تایید شد. کد ۱۰۱: تراکنش قبلاً تایید شده بود (بازهم موفق است).
            if (result?.Data?.Code is 100 or 101)
            {
                return new PaymentVerifyResult(true, RefId: result.Data.RefId?.ToString());
            }

            _logger.LogWarning("ZarinPal payment verify failed. Code={Code} Response={@Result}", result?.Data?.Code, result);
            return new PaymentVerifyResult(false, ErrorMessage: $"تایید پرداخت ناموفق بود (کد: {result?.Data?.Code}).");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در تایید تراکنش زرین‌پال");
            return new PaymentVerifyResult(false, ErrorMessage: "خطا در ارتباط با درگاه پرداخت برای تایید تراکنش.");
        }
    }

    // --- مدل‌های پاسخ زرین‌پال ---
    // فیلد errors در پاسخ زرین‌پال گاهی آرایه خالی [] و گاهی یک Object با جزئیات خطاست؛
    // برای جلوگیری از خطای Deserialize، به‌صورت JsonElement خام نگه‌داری می‌شود.

    private class ZarinPalRequestResponse
    {
        [JsonPropertyName("data")] public ZarinPalRequestData? Data { get; set; }
        [JsonPropertyName("errors")] public JsonElement Errors { get; set; }
    }

    private class ZarinPalRequestData
    {
        [JsonPropertyName("code")] public int Code { get; set; }
        [JsonPropertyName("authority")] public string? Authority { get; set; }
        [JsonPropertyName("message")] public string? Message { get; set; }
    }

    private class ZarinPalVerifyResponse
    {
        [JsonPropertyName("data")] public ZarinPalVerifyData? Data { get; set; }
        [JsonPropertyName("errors")] public JsonElement Errors { get; set; }
    }

    private class ZarinPalVerifyData
    {
        [JsonPropertyName("code")] public int Code { get; set; }
        [JsonPropertyName("ref_id")] public long? RefId { get; set; }
    }
}
