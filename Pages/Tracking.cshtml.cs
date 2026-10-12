using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SimCardShop.Data;
using SimCardShop.Models;
using SimCardShop.Services;

namespace SimCardShop.Pages;

public class TrackingModel : PageModel
{
    private readonly ApplicationDbContext _db;
    private readonly IPaymentGateway _paymentGateway;

    public TrackingModel(ApplicationDbContext db, IPaymentGateway paymentGateway)
    {
        _db = db;
        _paymentGateway = paymentGateway;
    }

    // این بخش مقادیر ارسال شده با متد GET را دریافت می‌کند
    [BindProperty(SupportsGet = true)]
    public string? SearchQuery { get; set; }

    public bool HasSearched { get; set; }
    public List<Order> Orders { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchQuery))
        {
            return Page();
        }

        HasSearched = true;
        SearchQuery = SearchQuery.Trim(); // فاصله‌های اضافی حذف می‌شود

        // جستجو بر اساس کد سفارش یا شماره موبایل در دیتابیس
        Orders = await _db.Orders
            .Include(o => o.Items).ThenInclude(i => i.SimCard)
            .Where(o => o.OrderCode == SearchQuery || o.CustomerMobile == SearchQuery)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        return Page();
    }

    /// <summary>
    /// اتصال مجدد به درگاه پرداخت برای سفارش‌هایی که پرداخت نشده‌اند
    /// </summary>
    public async Task<IActionResult> OnPostRetryPaymentAsync(int orderId)
    {
        var order = await _db.Orders
            .Include(o => o.Items).ThenInclude(i => i.SimCard)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order is null || order.IsPaid)
        {
            return RedirectToPage("/Tracking");
        }

        var simNumber = order.Items.FirstOrDefault()?.SimCard?.DisplayNumber ?? order.OrderCode;
        var callbackUrl = Url.Page("/Payment/Callback", pageHandler: null, values: null, protocol: Request.Scheme)!;

        // فراخوانی سرویس درگاه پرداخت
        var request = await _paymentGateway.RequestPaymentAsync(
            (long)order.TotalAmount,
            $"خرید شماره {simNumber} - سفارش {order.OrderCode}",
            callbackUrl,
            order.CustomerMobile);

        if (!request.Success || request.PaymentUrl is null)
        {
            // اگر درگاه مشکل داشت، با حفظ اطلاعات جستجو کاربر را در همین صفحه نگه دار
            return RedirectToPage("/Tracking", new { SearchQuery = SearchQuery });
        }

        order.PaymentAuthority = request.Authority;
        order.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Redirect(request.PaymentUrl);
    }
}