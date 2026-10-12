using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SimCardShop.Data;
using SimCardShop.Models;

namespace SimCardShop.Pages.Contact;

public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _db;

    public IndexModel(ApplicationDbContext db)
    {
        _db = db;
    }

    public SiteSetting Settings { get; set; } = new();

    public async Task OnGetAsync()
    {
        Settings = await _db.SiteSettings.FirstOrDefaultAsync() ?? new SiteSetting();
    }
}
