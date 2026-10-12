using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SimCardShop.Data;

namespace SimCardShop.Pages.Admin.Sms.Templates;

public class EditModel : PageModel
{
    private readonly ApplicationDbContext _db;

    public EditModel(ApplicationDbContext db)
    {
        _db = db;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var template = await _db.SmsTemplates.FindAsync(id);
        if (template is null) return NotFound();

        Input = new InputModel { Id = template.Id, Name = template.Name, Text = template.Text };
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();

        var template = await _db.SmsTemplates.FindAsync(Input.Id);
        if (template is null) return NotFound();

        template.Name = Input.Name;
        template.Text = Input.Text;
        await _db.SaveChangesAsync();

        TempData["StatusMessage"] = "قالب به‌روزرسانی شد.";
        return RedirectToPage("/Admin/Sms/Templates/Index");
    }

    public class InputModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "نام قالب را وارد کنید.")]
        [MaxLength(150)]
        [Display(Name = "نام قالب")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "متن قالب را وارد کنید.")]
        [Display(Name = "متن قالب")]
        public string Text { get; set; } = string.Empty;
    }
}
