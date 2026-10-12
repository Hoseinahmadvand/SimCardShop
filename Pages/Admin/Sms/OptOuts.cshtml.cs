using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SimCardShop.Data;
using SimCardShop.Helpers;
using SimCardShop.Models;

namespace SimCardShop.Pages.Admin.Sms;

public class OptOutsModel : PageModel
{
    private readonly ApplicationDbContext _db;

    public OptOutsModel(ApplicationDbContext db)
    {
        _db = db;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public List<SmsOptOut> Items { get; set; } = new();

    [TempData]
    public string? StatusMessage { get; set; }

    public async Task OnGetAsync()
    {
        Items = await _db.SmsOptOuts.OrderByDescending(o => o.CreatedAt).ToListAsync();
    }

    public async Task<IActionResult> OnPostAddAsync()
    {
        var mobiles = SmsHelper.ExtractMobiles(Input.Mobiles);

        if (mobiles.Count == 0)
        {
            ModelState.AddModelError("Input.Mobiles", "حداقل یک شماره موبایل معتبر وارد کنید.");
            Items = await _db.SmsOptOuts.OrderByDescending(o => o.CreatedAt).ToListAsync();
            return Page();
        }

        var existing = await _db.SmsOptOuts.Select(o => o.Mobile).ToListAsync();
        var newOnes = mobiles.Where(m => !existing.Contains(m)).Distinct().ToList();

        foreach (var mobile in newOnes)
        {
            _db.SmsOptOuts.Add(new SmsOptOut { Mobile = mobile, Note = Input.Note });
        }
        await _db.SaveChangesAsync();

        StatusMessage = $"{newOnes.Count} شماره به لیست عدم ارسال اضافه شد.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostRemoveAsync(int id)
    {
        var item = await _db.SmsOptOuts.FindAsync(id);
        if (item is not null)
        {
            _db.SmsOptOuts.Remove(item);
            await _db.SaveChangesAsync();
            StatusMessage = $"شماره {item.Mobile} از لیست عدم ارسال حذف شد.";
        }
        return RedirectToPage();
    }

    public class InputModel
    {
        [Display(Name = "شماره موبایل (هر خط یک شماره)")]
        public string? Mobiles { get; set; }

        [MaxLength(200)]
        [Display(Name = "یادداشت (اختیاری)")]
        public string? Note { get; set; }
    }
}
