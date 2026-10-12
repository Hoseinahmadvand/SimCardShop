using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using SimCardShop.Data;
using SimCardShop.Helpers;
using SimCardShop.Models;
using SimCardShop.Services;

namespace SimCardShop.Pages;

public class SimCardDetailsModel : PageModel
{
    private readonly ApplicationDbContext _db;
    private readonly IMemoryCache _cache;
    private readonly IPaymentGateway _paymentGateway;

    public SimCardDetailsModel(ApplicationDbContext db, IMemoryCache cache, IPaymentGateway paymentGateway)
    {
        _db = db;
        _cache = cache;
        _paymentGateway = paymentGateway;
    }

    private bool IsRateLimited(string action, int maxAttempts, TimeSpan window)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var key = $"ratelimit:{action}:{ip}";

        var count = _cache.GetOrCreate(key, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = window;
            return 0;
        });

        if (count >= maxAttempts) return true;

        _cache.Set(key, count + 1, window);
        return false;
    }

    public SimCard? SimCard { get; set; }
    public List<SimCard> SimilarSimCards { get; set; } = new();
    public string? OfficePhone { get; set; }
    public string? OfficeWhatsAppLink { get; set; }
    public bool PaymentEnabled { get; set; }

    [BindProperty]
    public BuyInput Buy { get; set; } = new();

    public bool OrderSubmitted { get; set; }
    public string? OrderCode { get; set; }

    public async Task<IActionResult> OnGetAsync(string number)
    {
        SimCard = await _db.SimCards.FirstOrDefaultAsync(s => s.Number == number);

        if (SimCard is null || SimCard.Status == SimCardStatus.Disabled)
        {
            return NotFound();
        }

        // ثبت بازدید ساده
        SimCard.ViewCount++;
        _db.VisitLogs.Add(new VisitLog
        {
            SimCardId = SimCard.Id,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
            Referrer = Request.Headers["Referer"].ToString()
        });
        await _db.SaveChangesAsync();

        await LoadSimilarAndSettingsAsync(SimCard);

        return Page();
    }

    private async Task LoadSimilarAndSettingsAsync(SimCard simCard)
    {
        var settings = await _db.SiteSettings.AsNoTracking().FirstOrDefaultAsync();
        OfficePhone = settings?.Phone ?? settings?.Mobile;
        PaymentEnabled = settings?.PaymentEnabled == true && !string.IsNullOrWhiteSpace(settings.ZarinPalMerchantId);

        if (!string.IsNullOrWhiteSpace(settings?.Mobile))
        {
            var digits = new string(settings.Mobile.Where(char.IsDigit).ToArray());
            if (digits.StartsWith("0")) digits = "98" + digits[1..];

            var text = Uri.EscapeDataString($"سلام، درباره شماره {simCard.DisplayNumber} سوال داشتم.");
            OfficeWhatsAppLink = $"https://wa.me/{digits}?text={text}";
        }

        SimilarSimCards = await _db.SimCards
            .Where(s => s.Id != simCard.Id && s.Status == SimCardStatus.Available)
            .Where(s => s.Operator == simCard.Operator || s.RandomnessType == simCard.RandomnessType)
            .OrderByDescending(s => s.CreatedAt)
            .Take(4)
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostBuyAsync(string number)
    {
        // ۱. پاکسازی کامل خطاهای بخش‌های دیگر از ModelState
        foreach (var key in ModelState.Keys.Where(k => k.StartsWith("Callback") || k == "Callback.Mobile").ToList())
        {
            ModelState.Remove(key);
        }

        if (string.IsNullOrWhiteSpace(number))
        {
            return BadRequest("شماره سیم‌کارت ارسال نشده است.");
        }

        SimCard = await _db.SimCards.FirstOrDefaultAsync(s => s.Number == number);

        if (SimCard is null || SimCard.Status == SimCardStatus.Disabled)
        {
            return NotFound();
        }

        // بارگذاری تنظیمات و موارد مشابه
        await LoadSimilarAndSettingsAsync(SimCard);

        // بررسی اینکه آیا درگاه پرداخت در تنظیمات سایت فعال است یا خیر
        if (!PaymentEnabled)
        {
            ModelState.AddModelError(string.Empty, "درگاه پرداخت آنلاین در حال حاضر فعال نیست یا تنظیمات آن (مثل مرچنت کد زرین‌پال) کامل نشده است. لطفاً از طریق پنل مدیریت تنظیمات را بررسی کنید.");
            return Page();
        }

        if (IsRateLimited("buy", maxAttempts: 5, TimeSpan.FromMinutes(10)))
        {
            ModelState.AddModelError(string.Empty, "تعداد درخواست‌های شما زیاد بوده، لطفاً کمی بعد دوباره تلاش کنید.");
            return Page();
        }

        // بررسی موجود بودن شماره
        if (SimCard.Status != SimCardStatus.Available)
        {
            ModelState.AddModelError(string.Empty, "متأسفانه این شماره دیگر برای فروش در دسترس نیست.");
            return Page();
        }

        // جلوگیری از ثبت سفارش تکراری در ۱۰ دقیقه گذشته
        var recentDuplicate = await _db.Orders
            .Where(o => o.CustomerMobile == Buy.Mobile && o.CreatedAt > DateTime.UtcNow.AddMinutes(-10))
            .AnyAsync(o => o.Items.Any(i => i.SimCardId == SimCard.Id));

        if (recentDuplicate)
        {
            ModelState.AddModelError(string.Empty, "سفارش شما برای این شماره قبلاً ثبت شده است. کارشناسان دفتر به‌زودی تماس می‌گیرند.");
            return Page();
        }

        // افزایش تعداد کلیک خرید
        SimCard.BuyClickCount++;

        // ایجاد سفارش جدید
        var order = new Order
        {
            OrderCode = "ORD-" + DateTime.UtcNow.Ticks.ToString()[^6..],
            CustomerName = Buy.FullName,
            CustomerMobile = Buy.Mobile,
            Notes = Buy.Notes,
            TotalAmount = SimCard.FinalPrice,
            Status = OrderStatus.New
        };

        order.Items.Add(new OrderItem
        {
            SimCardId = SimCard.Id,
            PriceAtPurchase = SimCard.FinalPrice
        });

        // رزرو موقت شماره
        SimCard.Status = SimCardStatus.Reserved;
        SimCard.UpdatedAt = DateTime.UtcNow;

        var customer = await CrmHelper.FindOrCreateCustomerAsync(_db, Buy.Mobile, Buy.FullName);
        order.CustomerId = customer.Id;

        CrmHelper.AddActivity(_db, customer, ActivityType.Order,
            $"درخواست خرید شماره {SimCard.DisplayNumber} به مبلغ {SimCard.FinalPrice:N0} تومان ثبت شد.");

        _db.Orders.Add(order);
        await _db.SaveChangesAsync();

        // اتصال به درگاه پرداخت زرین‌پال
        var callbackUrl = Url.Page("/Payment/Callback", pageHandler: null, values: null, protocol: Request.Scheme);

        var paymentRequest = await _paymentGateway.RequestPaymentAsync(
            (long)order.TotalAmount,
            $"خرید شماره {SimCard.DisplayNumber} - سفارش {order.OrderCode}",
            callbackUrl!,
            order.CustomerMobile);

        if (paymentRequest.Success && paymentRequest.PaymentUrl is not null)
        {
            order.PaymentGateway = "ZarinPal";
            order.PaymentAuthority = paymentRequest.Authority;
            await _db.SaveChangesAsync();

            return Redirect(paymentRequest.PaymentUrl);
        }
        else
        {
            // اگر درگاه خطا داد یا ارتباط برقرار نشد، وضعیت رزرو شماره را برگردانید و خطا نمایش دهید
            SimCard.Status = SimCardStatus.Available;
            await _db.SaveChangesAsync();

            ModelState.AddModelError(string.Empty, "خطا در ارتباط با درگاه پرداخت: " + (paymentRequest.ErrorMessage ?? "خطای ناشناخته در اتصال به زرین‌پال"));
            return Page();
        }
    }
    [BindProperty]
    public CallbackInput Callback { get; set; } = new();

    public bool CallbackSubmitted { get; set; }

    public async Task<IActionResult> OnPostRequestCallAsync(string number)
    {
        // پاکسازی خطاهای فرم خرید هنگام ارسال درخواست تماس
        foreach (var key in ModelState.Keys.Where(k => k.StartsWith("Buy")))
        {
            ModelState.Remove(key);
        }

        SimCard = await _db.SimCards.FirstOrDefaultAsync(s => s.Number == number);
        if (SimCard is null || SimCard.Status == SimCardStatus.Disabled)
        {
            return NotFound();
        }

        await LoadSimilarAndSettingsAsync(SimCard);

        if (string.IsNullOrWhiteSpace(Callback.Mobile) || Callback.Mobile.Length != 11)
        {
            ModelState.AddModelError("Callback.Mobile", "شماره موبایل معتبر نیست.");
            return Page();
        }

        if (IsRateLimited("callback", maxAttempts: 5, TimeSpan.FromMinutes(10)))
        {
            ModelState.AddModelError(string.Empty, "تعداد درخواست‌های شما زیاد بوده، لطفاً کمی بعد دوباره تلاش کنید.");
            return Page();
        }

        var customer = await CrmHelper.FindOrCreateCustomerAsync(_db, Callback.Mobile, Callback.Mobile);
        CrmHelper.AddActivity(_db, customer, ActivityType.Call,
            $"درخواست تماس درباره شماره {SimCard.DisplayNumber} از طریق سایت ثبت شد.");

        var order = new Order
        {
            OrderCode = "REQ-" + DateTime.UtcNow.Ticks.ToString()[^6..],
            CustomerName = "درخواست تماس",
            CustomerMobile = Callback.Mobile,
            Notes = $"درخواست تماس درباره شماره {SimCard.DisplayNumber}",
            TotalAmount = 0,
            Status = OrderStatus.New,
            CustomerId = customer.Id
        };
        order.Items.Add(new OrderItem { SimCardId = SimCard.Id, PriceAtPurchase = 0 });

        _db.Orders.Add(order);
        await _db.SaveChangesAsync();

        CallbackSubmitted = true;
        return Page();
    }

    public class CallbackInput
    {
        [Required]
        [RegularExpression(@"^09\d{9}$", ErrorMessage = "شماره موبایل معتبر نیست، مثلاً 09123456789")]
        public string Mobile { get; set; } = string.Empty;
    }

    public class BuyInput
    {
        [Required(ErrorMessage = "نام و نام خانوادگی را وارد کنید.")]
        [MaxLength(100)]
        [Display(Name = "نام و نام خانوادگی")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "شماره موبایل را وارد کنید.")]
        [RegularExpression(@"^09\d{9}$", ErrorMessage = "شماره موبایل معتبر نیست، مثلاً 09123456789")]
        [Display(Name = "شماره موبایل")]
        public string Mobile { get; set; } = string.Empty;

        [MaxLength(500)]
        [Display(Name = "توضیحات (اختیاری)")]
        public string? Notes { get; set; }
    }
}