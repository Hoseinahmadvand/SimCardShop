using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SimCardShop.Data;
using SimCardShop.Models;

namespace SimCardShop.Pages.Admin.Sms;

public class HistoryModel : PageModel
{
    private readonly ApplicationDbContext _db;

    public HistoryModel(ApplicationDbContext db)
    {
        _db = db;
    }

    [BindProperty(SupportsGet = true)]
    public int Page { get; set; } = 1;

    public const int PageSize = 10;

    public List<SmsCampaign> Campaigns { get; set; } = new();
    public int TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);

    public async Task OnGetAsync()
    {
        var query = _db.SmsCampaigns
            .Include(c => c.SimCard)
            .OrderByDescending(c => c.CreatedAt);

        TotalCount = await query.CountAsync();
        if (Page < 1) Page = 1;

        Campaigns = await query
            .Skip((Page - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync();
    }
}
