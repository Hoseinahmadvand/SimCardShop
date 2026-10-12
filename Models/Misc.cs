using System.ComponentModel.DataAnnotations;

namespace SimCardShop.Models;

/// <summary>خدمات دفتر پیشخوان، مثل تعویض سیم‌کارت، انتقال مالکیت و ...</summary>
public class Service
{
    public int Id { get; set; }

    [Required, MaxLength(150)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>نام آیکون ساده (از مجموعه آیکون‌های سبک استفاده‌شده در سایت)</summary>
    [MaxLength(50)]
    public string? Icon { get; set; }

    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;
}

/// <summary>تنظیمات کلی سایت به صورت تک‌رکورد (Singleton row)</summary>
public class SiteSetting
{
    public int Id { get; set; } 

    [MaxLength(150)]
    public string OfficeName { get; set; } = "دفتر خدمات پیشخوان";

    [MaxLength(300)]
    public string? LogoUrl { get; set; }

    [MaxLength(20)]
    public string? Phone { get; set; }

    [MaxLength(15)]
    public string? Mobile { get; set; }

    [MaxLength(300)]
    public string? Address { get; set; }

    [MaxLength(300)]
    public string? WhatsAppUrl { get; set; }

    [MaxLength(300)]
    public string? TelegramUrl { get; set; }

    [MaxLength(300)]
    public string? InstagramUrl { get; set; }

    [MaxLength(500)]
    public string? MapUrl { get; set; }

    // تنظیمات اتصال پنل پیامک — بعداً به پنل واقعی وصل می‌شود
    [MaxLength(300)]
    public string? SmsApiUrl { get; set; }

    [MaxLength(300)]
    public string? SmsApiKey { get; set; }

    [MaxLength(20)]
    public string? SmsSenderNumber { get; set; }

    // تنظیمات اتصال درگاه پرداخت (زرین‌پال) — تا زمانی که PaymentEnabled فعال نباشد
    // یا Merchant ID خالی باشد، سفارش‌ها مانند قبل به‌صورت آفلاین (پیگیری تلفنی) ثبت می‌شوند.
    public bool PaymentEnabled { get; set; } = false;

    [MaxLength(80)]
    public string? ZarinPalMerchantId { get; set; }

    /// <summary>حالت آزمایشی درگاه (Sandbox) — برای تست بدون تراکنش واقعی</summary>
    public bool ZarinPalSandbox { get; set; } = true;
}

/// <summary>ثبت ساده بازدید هر شماره، بدون سیستم Analytics پیچیده</summary>
public class VisitLog
{
    public int Id { get; set; }

    public int SimCardId { get; set; }
    public SimCard? SimCard { get; set; }

    public DateTime VisitedAt { get; set; } = DateTime.UtcNow;

    [MaxLength(45)]
    public string? IpAddress { get; set; }

    [MaxLength(300)]
    public string? Referrer { get; set; }
}

/// <summary>قالب آماده پیامک، با پشتیبانی از {Number}، {Price} و {Link}</summary>
public class SmsTemplate
{
    public int Id { get; set; }

    [Required, MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public string Text { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// لیست سراسری شماره‌هایی که درخواست عدم دریافت پیامک تبلیغاتی داده‌اند.
/// در تمام کمپین‌های آینده رعایت می‌شود.
/// </summary>
public class SmsOptOut
{
    public int Id { get; set; }

    [Required, MaxLength(15)]
    public string Mobile { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [MaxLength(200)]
    public string? Note { get; set; }
}
