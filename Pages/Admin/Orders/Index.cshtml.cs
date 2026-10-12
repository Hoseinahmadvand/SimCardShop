using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SimCardShop.Data;
using SimCardShop.Models;

namespace SimCardShop.Pages.Admin.Orders;

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

    public List<Order> Results { get; set; } = new();
    public int TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);

    [TempData]
    public string? StatusMessage { get; set; }

    public async Task OnGetAsync()
    {
        var query = _db.Orders
            .Include(o => o.Items)
            .ThenInclude(i => i.SimCard)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(Q))
        {
            var term = Q.Trim();
            query = query.Where(o =>
                o.OrderCode.Contains(term) ||
                o.CustomerName.Contains(term) ||
                o.CustomerMobile.Contains(term) ||
                o.Items.Any(i => i.SimCard != null && i.SimCard.Number.Contains(term)));
        }

        if (!string.IsNullOrWhiteSpace(Status) &&
            Enum.TryParse<OrderStatus>(Status, true, out var statusValue))
        {
            query = query.Where(o => o.Status == statusValue);
        }

        query = query.OrderByDescending(o => o.CreatedAt);

        TotalCount = await query.CountAsync();
        if (Page < 1) Page = 1;

        Results = await query
            .Skip((Page - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync();
    }
}
