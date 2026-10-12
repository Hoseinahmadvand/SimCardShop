using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SimCardShop.Data;
using SimCardShop.Models;

namespace SimCardShop.Pages.Admin.Statistics;

public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _db;

    public IndexModel(ApplicationDbContext db)
    {
        _db = db;
    }

    [BindProperty(SupportsGet = true)]
    public string Sort { get; set; } = "views";

    public int TotalViews { get; set; }
    public int TotalBuyClicks { get; set; }
    public int TotalCallClicks { get; set; }
    public int TotalWhatsAppClicks { get; set; }

    public List<SimCard> TopSimCards { get; set; } = new();

    public async Task OnGetAsync()
    {
        var simCards = _db.SimCards.AsQueryable();

        TotalViews = await simCards.SumAsync(s => s.ViewCount);
        TotalBuyClicks = await simCards.SumAsync(s => s.BuyClickCount);
        TotalCallClicks = await simCards.SumAsync(s => s.CallClickCount);
        TotalWhatsAppClicks = await simCards.SumAsync(s => s.WhatsAppClickCount);

        simCards = Sort switch
        {
            "buyClicks" => simCards.OrderByDescending(s => s.BuyClickCount),
            "callClicks" => simCards.OrderByDescending(s => s.CallClickCount),
            _ => simCards.OrderByDescending(s => s.ViewCount)
        };

        TopSimCards = await simCards.Take(25).ToListAsync();
    }
}
