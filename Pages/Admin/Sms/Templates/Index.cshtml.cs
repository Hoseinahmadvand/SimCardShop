using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SimCardShop.Data;
using SimCardShop.Models;

namespace SimCardShop.Pages.Admin.Sms.Templates;

public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _db;

    public IndexModel(ApplicationDbContext db)
    {
        _db = db;
    }

    public List<SmsTemplate> Items { get; set; } = new();

    [TempData]
    public string? StatusMessage { get; set; }

    public async Task OnGetAsync()
    {
        Items = await _db.SmsTemplates.OrderByDescending(t => t.CreatedAt).ToListAsync();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var template = await _db.SmsTemplates.FindAsync(id);
        if (template is not null)
        {
            _db.SmsTemplates.Remove(template);
            await _db.SaveChangesAsync();
            StatusMessage = $"قالب «{template.Name}» حذف شد.";
        }
        return RedirectToPage();
    }
}
