using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SimCardShop.Data;
using SimCardShop.Models;

namespace SimCardShop.Pages.Admin.Users;

public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public IndexModel(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public List<UserRow> Rows { get; set; } = new();

    [TempData]
    public string? StatusMessage { get; set; }

    public async Task OnGetAsync()
    {
        var users = await _userManager.Users.OrderBy(u => u.Email).ToListAsync();

        // مجموع فروش تکمیل‌شده به تفکیک کاربری که سفارش را نهایی کرده
        var salesByUser = await _db.Orders
            .Where(o => o.Status == OrderStatus.Completed && o.HandledByUserId != null)
            .GroupBy(o => o.HandledByUserId)
            .Select(g => new { UserId = g.Key, Count = g.Count(), Total = g.Sum(o => o.TotalAmount) })
            .ToListAsync();

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            var sales = salesByUser.FirstOrDefault(s => s.UserId == user.Id);

            Rows.Add(new UserRow
            {
                Id = user.Id,
                Email = user.Email ?? "-",
                DisplayName = user.DisplayName,
                Roles = roles.ToList(),
                IsLockedOut = user.LockoutEnd.HasValue && user.LockoutEnd > DateTimeOffset.UtcNow,
                CompletedSalesCount = sales?.Count ?? 0,
                TotalSalesAmount = sales?.Total ?? 0
            });
        }

        Rows = Rows.OrderByDescending(r => r.TotalSalesAmount).ToList();
    }

    public async Task<IActionResult> OnPostToggleLockAsync(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null) return NotFound();

        if (user.Id == _userManager.GetUserId(User))
        {
            StatusMessage = "امکان قفل کردن حساب خودتان وجود ندارد.";
            return RedirectToPage();
        }

        var isLocked = user.LockoutEnd.HasValue && user.LockoutEnd > DateTimeOffset.UtcNow;

        if (isLocked)
        {
            await _userManager.SetLockoutEndDateAsync(user, null);
            StatusMessage = $"حساب {user.Email} فعال شد.";
        }
        else
        {
            await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
            StatusMessage = $"حساب {user.Email} غیرفعال (قفل) شد.";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null) return NotFound();

        if (user.Id == _userManager.GetUserId(User))
        {
            StatusMessage = "امکان حذف حساب خودتان وجود ندارد.";
            return RedirectToPage();
        }

        var admins = await _userManager.GetUsersInRoleAsync("Admin");
        if (admins.Count <= 1 && admins.Any(a => a.Id == user.Id))
        {
            StatusMessage = "حذف این کاربر ممکن نیست، چون تنها مدیر باقی‌مانده سیستم است.";
            return RedirectToPage();
        }

        await _userManager.DeleteAsync(user);
        StatusMessage = $"کاربر {user.Email} حذف شد.";
        return RedirectToPage();
    }

    public class UserRow
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? DisplayName { get; set; }
        public List<string> Roles { get; set; } = new();
        public bool IsLockedOut { get; set; }
        public int CompletedSalesCount { get; set; }
        public decimal TotalSalesAmount { get; set; }
    }
}
