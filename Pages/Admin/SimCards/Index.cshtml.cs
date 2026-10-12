using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SimCardShop.Data;
using SimCardShop.Models;

namespace SimCardShop.Pages.Admin.SimCards;

public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _db;

    public IndexModel(ApplicationDbContext db)
    {
        _db = db;
    }

    [BindProperty(SupportsGet = true)]
    public string? Q { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Status { get; set; }

    [BindProperty(SupportsGet = true)]
    public int Page { get; set; } = 1;

    public const int PageSize = 15;

    public List<SimCard> Results { get; set; } = new();
    public int TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);

    [TempData]
    public string? StatusMessage { get; set; }

    public async Task OnGetAsync()
    {
        await LoadAsync();
    }

    public async Task<IActionResult> OnPostToggleFeaturedAsync(int id)
    {
        var sim = await _db.SimCards.FindAsync(id);
        if (sim is not null)
        {
            sim.IsFeatured = !sim.IsFeatured;
            sim.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            StatusMessage = $"وضعیت ویژه‌بودن شماره {sim.DisplayNumber} به‌روزرسانی شد.";
        }
        return RedirectToPage(new { Q, Status, Page });
    }

    public async Task<IActionResult> OnPostChangeStatusAsync(int id, SimCardStatus newStatus)
    {
        var sim = await _db.SimCards.FindAsync(id);
        if (sim is not null)
        {
            sim.Status = newStatus;
            sim.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            StatusMessage = $"وضعیت شماره {sim.DisplayNumber} به «{Helpers.DisplayLabels.SimCardStatus(newStatus)}» تغییر کرد.";
        }
        return RedirectToPage(new { Q, Status, Page });
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var sim = await _db.SimCards
            .Include(s => s.OrderItems)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (sim is not null)
        {
            if (sim.OrderItems.Any())
            {
                // به‌جای حذف فیزیکی رکوردی که در سفارش استفاده شده، غیرفعال می‌شود
                // تا یکپارچگی تاریخچه سفارش‌ها حفظ شود.
                sim.Status = SimCardStatus.Disabled;
                StatusMessage = $"شماره {sim.DisplayNumber} در سفارش‌های قبلی استفاده شده، به‌جای حذف غیرفعال شد.";
            }
            else
            {
                _db.SimCards.Remove(sim);
                StatusMessage = $"شماره {sim.DisplayNumber} حذف شد.";
            }
            await _db.SaveChangesAsync();
        }
        return RedirectToPage(new { Q, Status, Page });
    }

    private async Task LoadAsync()
    {
        var query = _db.SimCards.AsQueryable();

        if (!string.IsNullOrWhiteSpace(Q))
        {
            var digits = new string(Q.Where(char.IsDigit).ToArray());
            if (digits.Length > 0)
            {
                query = query.Where(s => s.Number.Contains(digits));
            }
        }

        if (!string.IsNullOrWhiteSpace(Status) &&
            Enum.TryParse<SimCardStatus>(Status, true, out var statusValue))
        {
            query = query.Where(s => s.Status == statusValue);
        }

        query = query.OrderByDescending(s => s.CreatedAt);

        TotalCount = await query.CountAsync();
        if (Page < 1) Page = 1;

        Results = await query
            .Skip((Page - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync();
    }
}
