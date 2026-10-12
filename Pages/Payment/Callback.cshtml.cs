using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SimCardShop.Data;
using SimCardShop.Helpers;
using SimCardShop.Models;
using SimCardShop.Services;

namespace SimCardShop.Pages.Payment;

/// <summary>
/// آدرس بازگشت کاربر از درگاه پرداخت پس از تکمیل یا انصراف از پرداخت.
/// آدرس: /payment/callback?Authority=...&amp;Status=OK|NOK
/// </summary>
public class CallbackModel : PageModel
{
    private readonly ApplicationDbContext _db;
    private readonly IPaymentGateway _gateway;

    public CallbackModel(ApplicationDbContext db, IPaymentGateway gateway)
    {
        _db = db;
        _gateway = gateway;
    }

    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public Order? Order { get; set; }

    public async Task<IActionResult> OnGetAsync(string? Authority, string? Status)
    {
        if (string.IsNullOrWhiteSpace(Authority))
        {
            return RedirectToPage("/Index");
        }

        var order = await _db.Orders
            .Include(o => o.Items).ThenInclude(i => i.SimCard)
            .Include(o => o.Customer)
            .FirstOrDefaultAsync(o => o.PaymentAuthority == Authority);

        if (order is null)
        {
            ErrorMessage = "سفارش مربوط به این تراکنش پیدا نشد.";
            return Page();
        }

        Order = order;

        // کاربر پرداخت را در درگاه لغو کرده یا ناموفق بوده است
        if (!string.Equals(Status, "OK", StringComparison.OrdinalIgnoreCase))
        {
            ErrorMessage = "پرداخت توسط شما لغو شد یا ناموفق بود.";
            return Page();
        }

        // اگر این تراکنش قبلاً با موفقیت تایید شده (مثلاً کاربر صفحه را رفرش کرده)
        if (order.IsPaid)
        {
            Success = true;
            return Page();
        }

        var verify = await _gateway.VerifyPaymentAsync(Authority, (long)order.TotalAmount);

        if (!verify.Success)
        {
            ErrorMessage = verify.ErrorMessage ?? "تایید پرداخت ناموفق بود.";
            return Page();
        }

        order.IsPaid = true;
        order.Status = OrderStatus.Paid;
        order.PaymentGateway = "ZarinPal";
        order.PaymentRefId = verify.RefId;
        order.PaidAt = DateTime.UtcNow;
        order.UpdatedAt = DateTime.UtcNow;

        if (order.Customer is not null)
        {
            CrmHelper.AddActivity(_db, order.Customer, ActivityType.Order,
                $"پرداخت آنلاین سفارش {order.OrderCode} به مبلغ {order.TotalAmount:N0} تومان با موفقیت انجام شد (کد پیگیری: {verify.RefId}).");
        }

        await _db.SaveChangesAsync();

        Success = true;
        return Page();
    }

    /// <summary>تلاش مجدد برای پرداخت همان سفارش، در صورتی که بار قبل ناموفق یا لغو شده باشد</summary>
    public async Task<IActionResult> OnPostRetryAsync(int orderId)
    {
        var order = await _db.Orders
            .Include(o => o.Items).ThenInclude(i => i.SimCard)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order is null || order.IsPaid)
        {
            return RedirectToPage("/Index");
        }

        var simNumber = order.Items.FirstOrDefault()?.SimCard?.DisplayNumber ?? order.OrderCode;
        var callbackUrl = Url.Page("/Payment/Callback", pageHandler: null, values: null, protocol: Request.Scheme)!;

        var request = await _gateway.RequestPaymentAsync(
            (long)order.TotalAmount,
            $"خرید شماره {simNumber} - سفارش {order.OrderCode}",
            callbackUrl,
            order.CustomerMobile);

        if (!request.Success || request.PaymentUrl is null)
        {
            Order = order;
            ErrorMessage = request.ErrorMessage ?? "امکان اتصال به درگاه پرداخت وجود ندارد.";
            return Page();
        }

        order.PaymentAuthority = request.Authority;
        order.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Redirect(request.PaymentUrl);
    }
}
