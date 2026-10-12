using SimCardShop.Models;

namespace SimCardShop.Helpers;

public static class SmsHelper
{
    /// <summary>جایگزینی {Number}، {Price} و {Link} در متن قالب با اطلاعات شماره</summary>
    public static string ApplyPlaceholders(string template, SimCard simCard, string baseUrl)
    {
        var link = $"{baseUrl.TrimEnd('/')}/i/{simCard.Number}";

        return template
            .Replace("{Number}", simCard.DisplayNumber)
            .Replace("{Price}", simCard.FinalPrice.ToString("N0") + " تومان")
            .Replace("{Link}", link);
    }

    /// <summary>
    /// تخمین تعداد پیامک بر اساس طول متن.
    /// متن فارسی از استاندارد UCS-2 استفاده می‌کند: هر پیامک تکی حداکثر ۷۰ کاراکتر
    /// و در پیامک‌های زنجیره‌ای حداکثر ۶۷ کاراکتر برای هر بخش.
    /// </summary>
    public static int EstimateSegmentCount(string message)
    {
        var length = message.Length;
        if (length == 0) return 0;
        if (length <= 70) return 1;
        return (int)Math.Ceiling(length / 67.0);
    }

    /// <summary>خواندن شماره موبایل معتبر از متن آزاد (خط به خط، یا فایل CSV ساده)</summary>
    public static List<string> ExtractMobiles(string? rawText)
    {
        if (string.IsNullOrWhiteSpace(rawText)) return new List<string>();

        var result = new List<string>();
        var lines = rawText.Split(new[] { '\r', '\n', ',', ';' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var rawLine in lines)
        {
            var digits = new string(rawLine.Where(char.IsDigit).ToArray());

            // پشتیبانی از فرمت‌های 989123456789 و +989123456789
            if (digits.StartsWith("98") && digits.Length == 12)
            {
                digits = "0" + digits[2..];
            }

            if (digits.Length == 11 && digits.StartsWith("09"))
            {
                result.Add(digits);
            }
        }

        return result;
    }
}
