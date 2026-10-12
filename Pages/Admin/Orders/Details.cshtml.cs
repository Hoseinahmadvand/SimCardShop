using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SimCardShop.Data;
using SimCardShop.Models;

namespace SimCardShop.Pages.Admin.Orders;

public class DetailsModel : PageModel
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public DetailsModel(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public Order Order { get; set; } = null!;
    public List<ApplicationUser> StaffUsers { get; set; } = new();

    [TempData]
    public string? StatusMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var order = await _db.Orders
            .Include(o => o.Items)
            .ThenInclude(i => i.SimCard)
            .Include(o => o.HandledByUser)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order is null) return NotFound();

        Order = order;
        StaffUsers = await _userManager.Users.OrderBy(u => u.Email).ToListAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostChangeStatusAsync(int id, OrderStatus newStatus)
    {
        var order = await _db.Orders
            .Include(o => o.Items)
            .ThenInclude(i => i.SimCard)
            .Include(o => o.Customer)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order is null) return NotFound();

        var previousStatus = order.Status;
        order.Status = newStatus;
        order.UpdatedAt = DateTime.UtcNow;

        if (newStatus == OrderStatus.Paid)
        {
            order.IsPaid = true;
        }

        // فروش تکمیل‌شده به‌طور خودکار به کاربری که آن را نهایی کرده نسبت داده می‌شود
        if (newStatus == OrderStatus.Completed && order.HandledByUserId is null)
        {
            order.HandledByUserId = _userManager.GetUserId(User);
        }

        // بازتاب تغییر وضعیت سفارش روی پروفایل مشتری در CRM
        if (order.Customer is not null)
        {
            if (newStatus == OrderStatus.Completed)
            {
                order.Customer.Status = Models.CustomerStatus.Customer;
            }

            Helpers.CrmHelper.AddActivity(_db, order.Customer, Models.ActivityType.StatusChange,
                $"سفارش {order.OrderCode} از «{Helpers.DisplayLabels.OrderStatus(previousStatus)}» به «{Helpers.DisplayLabels.OrderStatus(newStatus)}» تغییر کرد.",
                _userManager.GetUserId(User));
        }

        // اثر تغییر وضعیت سفارش روی وضعیت خود شماره
        foreach (var item in order.Items)
        {
            if (item.SimCard is null) continue;

            if (newStatus == OrderStatus.Completed)
            {
                item.SimCard.Status = SimCardStatus.Sold;
                item.SimCard.UpdatedAt = DateTime.UtcNow;
            }
            else if (newStatus == OrderStatus.Cancelled && item.SimCard.Status == SimCardStatus.Reserved)
            {
                // در صورت لغو سفارش، شماره‌ای که فقط برای همین سفارش رزرو شده بود آزاد می‌شود
                item.SimCard.Status = SimCardStatus.Available;
                item.SimCard.UpdatedAt = DateTime.UtcNow;
            }
        }

        await _db.SaveChangesAsync();

        TempData["StatusMessage"] = $"وضعیت سفارش {order.OrderCode} از «{Helpers.DisplayLabels.OrderStatus(previousStatus)}» به «{Helpers.DisplayLabels.OrderStatus(newStatus)}» تغییر کرد.";
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostAssignSellerAsync(int id, string? userId)
    {
        var order = await _db.Orders.FindAsync(id);
        if (order is null) return NotFound();

        order.HandledByUserId = string.IsNullOrEmpty(userId) ? null : userId;
        order.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        TempData["StatusMessage"] = "فروشنده مسئول این سفارش به‌روزرسانی شد.";
        return RedirectToPage(new { id });
    }
}
