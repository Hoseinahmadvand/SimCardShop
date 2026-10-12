using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SimCardShop.Models;

public enum CustomerStatus
{
    Lead = 1,        // سرنخ جدید
    Contacted = 2,   // در حال پیگیری
    Customer = 3,    // مشتری (حداقل یک خرید تکمیل‌شده)
    Lost = 4         // از دست رفته / بی‌علاقه
}

public enum ActivityType
{
    Note = 1,        // یادداشت دستی
    Call = 2,        // تماس تلفنی
    Sms = 3,         // پیامک
    Order = 4,       // سفارش/درخواست خرید
    StatusChange = 5 // تغییر وضعیت یا فروشنده
}

/// <summary>
/// پروفایل مشتری/سرنخ. به‌طور خودکار از فرم خرید و درخواست تماس در صفحه هر شماره ساخته می‌شود،
/// یا می‌توان دستی از پنل مدیریت اضافه کرد.
/// </summary>
public class Customer
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required, MaxLength(15)]
    public string Mobile { get; set; } = string.Empty;

    [MaxLength(150)]
    public string? Email { get; set; }

    public CustomerStatus Status { get; set; } = CustomerStatus.Lead;

    /// <summary>کارشناس فروش مسئول پیگیری این مشتری</summary>
    public string? AssignedToUserId { get; set; }
    public ApplicationUser? AssignedToUser { get; set; }

    public DateTime? NextFollowUpAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<CustomerActivity> Activities { get; set; } = new List<CustomerActivity>();
    public ICollection<Order> Orders { get; set; } = new List<Order>();

    [NotMapped]
    public bool NeedsFollowUpToday => NextFollowUpAt.HasValue && NextFollowUpAt.Value.Date <= DateTime.UtcNow.Date;
}

/// <summary>یک رخداد در تایم‌لاین مشتری — یادداشت، تماس، سفارش یا تغییر وضعیت</summary>
public class CustomerActivity
{
    public int Id { get; set; }

    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public ActivityType Type { get; set; }

    [Required]
    public string Text { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>خالی برای رخدادهای خودکار سیستم (مثل ثبت سفارش از سایت)</summary>
    public string? CreatedByUserId { get; set; }
    public ApplicationUser? CreatedByUser { get; set; }
}
