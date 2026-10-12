using IPE.SmsIrClient;
using Microsoft.EntityFrameworkCore;
using SimCardShop.Data;

namespace SimCardShop.Services;

public class SmsService : ISmsService
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<SmsService> _logger;

    public SmsService(
        ApplicationDbContext db,
        ILogger<SmsService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<SmsSendResult> SendAsync(
        string mobile,
        string message,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(mobile))
            return new SmsSendResult(false, "شماره موبایل وارد نشده است.");

        if (string.IsNullOrWhiteSpace(message))
            return new SmsSendResult(false, "متن پیامک خالی است.");

        var settings = await _db.SiteSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);

        var apiKey = settings?.SmsApiKey;
        var sender = settings?.SmsSenderNumber;

        if (string.IsNullOrWhiteSpace(apiKey))
            return new SmsSendResult(false, "API Key مربوط به SMS.ir تنظیم نشده است.");

        if (string.IsNullOrWhiteSpace(sender))
            return new SmsSendResult(false, "شماره ارسال SMS.ir تنظیم نشده است.");

        try
        {
            var smsIr = new SmsIr(apiKey);

            var response = await smsIr.BulkSendAsync(
                long.Parse(sender),
                message,
                new[]
                {
                    NormalizeMobile(mobile)
                });

            if (response.Data == null)
            {
                _logger.LogError(
                    "SMS.ir ارسال پیامک را ناموفق اعلام کرد. Mobile: {Mobile}",
                    mobile);

                return new SmsSendResult(
                    false,
                    "ارسال پیامک توسط SMS.ir ناموفق بود.");
            }

            _logger.LogInformation(
                "SMS با موفقیت توسط SMS.ir ارسال شد. Mobile: {Mobile}",
                mobile);

            return new SmsSendResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "خطا در ارسال SMS.ir به {Mobile}",
                mobile);

            return new SmsSendResult(
                false,
                "خطا در ارتباط با پنل SMS.ir.");
        }
    }

    private static string NormalizeMobile(string mobile)
    {
        mobile = mobile
            .Trim()
            .Replace(" ", "")
            .Replace("-", "");

        if (mobile.StartsWith("+98"))
            mobile = "0" + mobile[3..];

        if (mobile.StartsWith("98") && mobile.Length == 12)
            mobile = "0" + mobile[2..];

        return mobile;
    }
}