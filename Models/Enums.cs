namespace SimCardShop.Models;

/// <summary>اپراتور خط</summary>
public enum OperatorType
{
    HamrahAval = 1,   // همراه اول
    Irancell = 2,     // ایرانسل
    Rightel = 3       // رایتل
}

/// <summary>نوع سیم‌کارت</summary>
public enum SimCardKind
{
    Permanent = 1,  // دائمی
    Credit = 2      // اعتباری
}

/// <summary>وضعیت کارکرد خط</summary>
public enum UsageState
{
    Fresh = 1,   // صفر
    Used = 2     // کارکرده
}

/// <summary>نوع رندی شماره</summary>
public enum RandomnessType
{
    Simple = 1,       // ساده
    Paired = 2,       // جفتی
    Sequential = 3,   // ترتیبی
    Mirror = 4,       // آینه‌ای
    Special = 5,      // خاص/ویژه
    Economic = 6      // اقتصادی
}

/// <summary>وضعیت موجودی شماره</summary>
public enum SimCardStatus
{
    Available = 1,   // موجود
    Reserved = 2,    // رزرو شده
    Sold = 3,        // فروخته شده
    Disabled = 4     // غیرفعال
}

/// <summary>وضعیت سفارش</summary>
public enum OrderStatus
{
    New = 1,          // جدید
    Paid = 2,         // پرداخت شده
    InProgress = 3,   // در حال انجام
    Completed = 4,    // تکمیل شده
    Cancelled = 5     // لغو شده
}

/// <summary>وضعیت ارسال کمپین پیامکی</summary>
public enum SmsCampaignStatus
{
    Draft = 1,
    Sending = 2,
    Completed = 3,
    Cancelled = 4
}

/// <summary>وضعیت ارسال به هر گیرنده</summary>
public enum SmsRecipientStatus
{
    Pending = 1,
    Sent = 2,
    Failed = 3
}
