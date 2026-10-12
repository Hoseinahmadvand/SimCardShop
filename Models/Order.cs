using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SimCardShop.Models;

public class Order
{
    public int Id { get; set; }

    /// <summary>کد سفارش قابل نمایش به مشتری، مثلاً ORD-100234</summary>
    [Required, MaxLength(20)]
    public string OrderCode { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string CustomerName { get; set; } = string.Empty;

    [Required, MaxLength(15)]
    public string CustomerMobile { get; set; } = string.Empty;

    public string? Notes { get; set; }

    [Column(TypeName = "decimal(18,0)")]
    public decimal TotalAmount { get; set; }

    public OrderStatus Status { get; set; } = OrderStatus.New;

    public bool IsPaid { get; set; }

    /// <summary>نام درگاه پرداخت استفاده‌شده، مثلاً ZarinPal — خالی یعنی سفارش آفلاین/تلفنی</summary>
    [MaxLength(30)]
    public string? PaymentGateway { get; set; }

    /// <summary>کد Authority دریافتی از درگاه هنگام ایجاد تراکنش پرداخت</summary>
    [MaxLength(64)]
    public string? PaymentAuthority { get; set; }

    /// <summary>کد پیگیری (Ref ID) که درگاه پس از تایید موفق پرداخت برمی‌گرداند</summary>
    [MaxLength(32)]
    public string? PaymentRefId { get; set; }

    public DateTime? PaidAt { get; set; }

    /// <summary>پروفایل مشتری در CRM — به‌طور خودکار بر اساس شماره موبایل پیدا/ساخته می‌شود</summary>
    public int? CustomerId { get; set; }
    public Customer? Customer { get; set; }

    /// <summary>کاربر پنل مدیریت که این سفارش را نهایی/تکمیل کرده — مبنای گزارش فروش هر کاربر</summary>
    public string? HandledByUserId { get; set; }
    public ApplicationUser? HandledByUser { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
}

public class OrderItem
{
    public int Id { get; set; }

    public int OrderId { get; set; }
    public Order? Order { get; set; }

    public int SimCardId { get; set; }
    public SimCard? SimCard { get; set; }

    [Column(TypeName = "decimal(18,0)")]
    public decimal PriceAtPurchase { get; set; }
}
