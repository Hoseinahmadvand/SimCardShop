using SimCardShop.Models;

namespace SimCardShop.Helpers;

public static class DisplayLabels
{
    public static string Operator(OperatorType value) => value switch
    {
        OperatorType.HamrahAval => "همراه اول",
        OperatorType.Irancell => "ایرانسل",
        OperatorType.Rightel => "رایتل",
        _ => value.ToString()
    };

    public static string Kind(SimCardKind value) => value switch
    {
        SimCardKind.Permanent => "دائمی",
        SimCardKind.Credit => "اعتباری",
        _ => value.ToString()
    };

    public static string Usage(UsageState value) => value switch
    {
        UsageState.Fresh => "صفر",
        UsageState.Used => "کارکرده",
        _ => value.ToString()
    };

    public static string Randomness(RandomnessType value) => value switch
    {
        RandomnessType.Simple => "رند ساده",
        RandomnessType.Paired => "رند جفتی",
        RandomnessType.Sequential => "رند ترتیبی",
        RandomnessType.Mirror => "رند آینه‌ای",
        RandomnessType.Special => "رند ویژه",
        RandomnessType.Economic => "اقتصادی",
        _ => value.ToString()
    };

    public static string SimCardStatus(SimCardStatus value) => value switch
    {
        Models.SimCardStatus.Available => "موجود",
        Models.SimCardStatus.Reserved => "رزرو شده",
        Models.SimCardStatus.Sold => "فروخته شده",
        Models.SimCardStatus.Disabled => "غیرفعال",
        _ => value.ToString()
    };

    public static string OrderStatus(OrderStatus value) => value switch
    {
        Models.OrderStatus.New => "جدید",
        Models.OrderStatus.Paid => "پرداخت شده",
        Models.OrderStatus.InProgress => "در حال انجام",
        Models.OrderStatus.Completed => "تکمیل شده",
        Models.OrderStatus.Cancelled => "لغو شده",
        _ => value.ToString()
    };

    public static string SmsCampaignStatus(SmsCampaignStatus value) => value switch
    {
        Models.SmsCampaignStatus.Draft => "پیش‌نویس",
        Models.SmsCampaignStatus.Sending => "در حال ارسال",
        Models.SmsCampaignStatus.Completed => "تکمیل شده",
        Models.SmsCampaignStatus.Cancelled => "لغو شده",
        _ => value.ToString()
    };

    public static string CustomerStatus(CustomerStatus value) => value switch
    {
        Models.CustomerStatus.Lead => "سرنخ جدید",
        Models.CustomerStatus.Contacted => "در حال پیگیری",
        Models.CustomerStatus.Customer => "مشتری",
        Models.CustomerStatus.Lost => "از دست رفته",
        _ => value.ToString()
    };

    public static string ActivityType(ActivityType value) => value switch
    {
        Models.ActivityType.Note => "یادداشت",
        Models.ActivityType.Call => "تماس",
        Models.ActivityType.Sms => "پیامک",
        Models.ActivityType.Order => "سفارش",
        Models.ActivityType.StatusChange => "تغییر وضعیت",
        _ => value.ToString()
    };
}
