using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SimCardShop.Data;
using SimCardShop.Models;

namespace SimCardShop.Pages.Admin.Sms.Templates;

public class CreateModel : PageModel
{
    private readonly ApplicationDbContext _db;

    public CreateModel(ApplicationDbContext db)
    {
        _db = db;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();

        _db.SmsTemplates.Add(new SmsTemplate { Name = Input.Name, Text = Input.Text });
        await _db.SaveChangesAsync();

        TempData["StatusMessage"] = "قالب جدید ثبت شد.";
        return RedirectToPage("/Admin/Sms/Templates/Index");
    }

    public class InputModel
    {
        [Required(ErrorMessage = "نام قالب را وارد کنید.")]
        [MaxLength(150)]
        [Display(Name = "نام قالب")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "متن قالب را وارد کنید.")]
        [Display(Name = "متن قالب")]
        public string Text { get; set; } =
            "فروش ویژه شماره {Number}\nقیمت: {Price}\nمشاهده:\n{Link}\n\nلغو11";
    }
}
