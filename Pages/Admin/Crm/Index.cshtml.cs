using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SimCardShop.Data;
using SimCardShop.Models;

namespace SimCardShop.Pages.Admin.Crm;

public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public IndexModel(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    [BindProperty(SupportsGet = true)]
    public string? Q { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Status { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? AssignedTo { get; set; }

    [BindProperty(SupportsGet = true)]
    public bool FollowUpOnly { get; set; }

    [BindProperty(SupportsGet = true)]
    public int Page { get; set; } = 1;

    public const int PageSize = 20;

    public List<Customer> Results { get; set; } = new();
    public int TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public int FollowUpTodayCount { get; set; }

    public List<ApplicationUser> StaffUsers { get; set; } = new();

    public async Task OnGetAsync()
    {
        StaffUsers = await _userManager.Users.OrderBy(u => u.Email).ToListAsync();

        var query = _db.Customers.Include(c => c.AssignedToUser).AsQueryable();

        FollowUpTodayCount = await _db.Customers.CountAsync(c => c.NextFollowUpAt != null && c.NextFollowUpAt <= DateTime.UtcNow);

        if (!string.IsNullOrWhiteSpace(Q))
        {
            var term = Q.Trim();
            query = query.Where(c => c.FullName.Contains(term) || c.Mobile.Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(Status) && Enum.TryParse<CustomerStatus>(Status, true, out var statusValue))
        {
            query = query.Where(c => c.Status == statusValue);
        }

        if (!string.IsNullOrWhiteSpace(AssignedTo))
        {
            query = query.Where(c => c.AssignedToUserId == AssignedTo);
        }

        if (FollowUpOnly)
        {
            query = query.Where(c => c.NextFollowUpAt != null && c.NextFollowUpAt <= DateTime.UtcNow);
        }

        query = query.OrderBy(c => c.NextFollowUpAt == null)
                     .ThenBy(c => c.NextFollowUpAt)
                     .ThenByDescending(c => c.CreatedAt);

        TotalCount = await query.CountAsync();
        if (Page < 1) Page = 1;

        Results = await query
            .Skip((Page - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync();
    }
}
