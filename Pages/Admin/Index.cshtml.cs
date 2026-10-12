using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SimCardShop.Data;
using SimCardShop.Models;

namespace SimCardShop.Pages.Admin;

public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _db;

    public IndexModel(ApplicationDbContext db)
    {
        _db = db;
    }

    public int TotalSimCards { get; set; }
    public int AvailableSimCards { get; set; }
    public int SoldSimCards { get; set; }
    public int ReservedSimCards { get; set; }

    public int TotalOrders { get; set; }
    public int NewOrders { get; set; }
    public decimal TotalSales { get; set; }

    public int SentSmsCount { get; set; }
    public int FollowUpTodayCount { get; set; }

    public List<Order> RecentOrders { get; set; } = new();
    public List<SellerStat> TopSellers { get; set; } = new();

    public class SellerStat
    {
        public string Name { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal Total { get; set; }
    }

    public async Task OnGetAsync()
    {
        TotalSimCards = await _db.SimCards.CountAsync();
        AvailableSimCards = await _db.SimCards.CountAsync(s => s.Status == SimCardStatus.Available);
        SoldSimCards = await _db.SimCards.CountAsync(s => s.Status == SimCardStatus.Sold);
        ReservedSimCards = await _db.SimCards.CountAsync(s => s.Status == SimCardStatus.Reserved);

        TotalOrders = await _db.Orders.CountAsync();
        NewOrders = await _db.Orders.CountAsync(o => o.Status == OrderStatus.New);

        TotalSales = await _db.Orders
            .Where(o => o.Status == OrderStatus.Completed || o.Status == OrderStatus.Paid)
            .SumAsync(o => (decimal?)o.TotalAmount) ?? 0;

        SentSmsCount = await _db.SmsRecipients.CountAsync(r => r.Status == SmsRecipientStatus.Sent);
        FollowUpTodayCount = await _db.Customers.CountAsync(c => c.NextFollowUpAt != null && c.NextFollowUpAt <= DateTime.UtcNow);

        RecentOrders = await _db.Orders
            .Include(o => o.Items)
            .ThenInclude(i => i.SimCard)
            .OrderByDescending(o => o.CreatedAt)
            .Take(5)
            .ToListAsync();

        var salesByUser = await _db.Orders
            .Where(o => o.Status == OrderStatus.Completed && o.HandledByUserId != null)
            .Include(o => o.HandledByUser)
            .GroupBy(o => o.HandledByUser!.Email)
            .Select(g => new SellerStat { Name = g.Key ?? "-", Count = g.Count(), Total = g.Sum(o => o.TotalAmount) })
            .OrderByDescending(s => s.Total)
            .Take(5)
            .ToListAsync();

        TopSellers = salesByUser;
    }
}
