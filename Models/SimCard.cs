using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SimCardShop.Models;

public class SimCard
{
    public int Id { get; set; }

    /// <summary>شماره به صورت خالص و بدون فاصله، مثلاً 09200936136 — برای جستجو و URL</summary>
    [Required, MaxLength(15)]
    public string Number { get; set; } = string.Empty;

    public OperatorType Operator { get; set; }

    public SimCardKind Kind { get; set; }

    public UsageState Usage { get; set; }

    public RandomnessType RandomnessType { get; set; }

    [Column(TypeName = "decimal(18,0)")]
    public decimal Price { get; set; }

    /// <summary>درصد تخفیف، صفر یعنی بدون تخفیف</summary>
    public int DiscountPercent { get; set; }

    public SimCardStatus Status { get; set; } = SimCardStatus.Available;

    /// <summary>نمایش در بخش‌های ویژه صفحه اصلی</summary>
    public bool IsFeatured { get; set; }

    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    // آمار ساده لینک، بدون نیاز به سیستم Analytics جداگانه
    public int ViewCount { get; set; }
    public int BuyClickCount { get; set; }
    public int CallClickCount { get; set; }
    public int WhatsAppClickCount { get; set; }

    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    [NotMapped]
    public decimal FinalPrice => DiscountPercent > 0
        ? Math.Round(Price * (100 - DiscountPercent) / 100m, 0)
        : Price;

    [NotMapped]
    public bool IsPurchasable => Status == SimCardStatus.Available;

    /// <summary>نمایش شماره با فاصله‌گذاری خوانا، مثلاً 0920 093 6136</summary>
    [NotMapped]
    public string DisplayNumber
    {
        get
        {
            if (string.IsNullOrEmpty(Number) || Number.Length < 10) return Number;
            return $"{Number[..4]} {Number[4..7]} {Number[7..]}";
        }
    }
}
