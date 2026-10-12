using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SimCardShop.Data;
using SimCardShop.Models;

namespace SimCardShop.Pages.Admin.SimCards;

public class EditModel : PageModel
{
    private readonly ApplicationDbContext _db;

    public EditModel(ApplicationDbContext db)
    {
        _db = db;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string DisplayNumber { get; set; } = string.Empty;

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var sim = await _db.SimCards.FindAsync(id);
        if (sim is null) return NotFound();

        DisplayNumber = sim.DisplayNumber;

        Input = new InputModel
        {
            Id = sim.Id,
            Number = sim.Number,
            Operator = sim.Operator,
            Kind = sim.Kind,
            Usage = sim.Usage,
            RandomnessType = sim.RandomnessType,
            Price = sim.Price,
            DiscountPercent = sim.DiscountPercent,
            Status = sim.Status,
            IsFeatured = sim.IsFeatured,
            Description = sim.Description
        };

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var sim = await _db.SimCards.FindAsync(Input.Id);
        if (sim is null) return NotFound();

        DisplayNumber = sim.DisplayNumber;

        var digits = new string(Input.Number.Where(char.IsDigit).ToArray());

        if (digits.Length is < 10 or > 12)
        {
            ModelState.AddModelError("Input.Number", "شماره باید بین ۱۰ تا ۱۲ رقم باشد.");
        }
        else if (await _db.SimCards.AnyAsync(s => s.Number == digits && s.Id != Input.Id))
        {
            ModelState.AddModelError("Input.Number", "این شماره برای رکورد دیگری ثبت شده است.");
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        sim.Number = digits;
        sim.Operator = Input.Operator;
        sim.Kind = Input.Kind;
        sim.Usage = Input.Usage;
        sim.RandomnessType = Input.RandomnessType;
        sim.Price = Input.Price;
        sim.DiscountPercent = Input.DiscountPercent;
        sim.Status = Input.Status;
        sim.IsFeatured = Input.IsFeatured;
        sim.Description = Input.Description;
        sim.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        TempData["StatusMessage"] = $"شماره {sim.DisplayNumber} به‌روزرسانی شد.";
        return RedirectToPage("/Admin/SimCards/Index");
    }

    public class InputModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "شماره را وارد کنید.")]
        [Display(Name = "شماره (بدون فاصله)")]
        public string Number { get; set; } = string.Empty;

        [Display(Name = "اپراتور")]
        public OperatorType Operator { get; set; }

        [Display(Name = "نوع سیم‌کارت")]
        public SimCardKind Kind { get; set; }

        [Display(Name = "وضعیت کارکرد")]
        public UsageState Usage { get; set; }

        [Display(Name = "نوع رندی")]
        public RandomnessType RandomnessType { get; set; }

        [Required(ErrorMessage = "قیمت را وارد کنید.")]
        [Range(0, 100_000_000_000, ErrorMessage = "قیمت معتبر نیست.")]
        [Display(Name = "قیمت (تومان)")]
        public decimal Price { get; set; }

        [Range(0, 100, ErrorMessage = "درصد تخفیف باید بین ۰ تا ۱۰۰ باشد.")]
        [Display(Name = "درصد تخفیف")]
        public int DiscountPercent { get; set; }

        [Display(Name = "وضعیت موجودی")]
        public SimCardStatus Status { get; set; }

        [Display(Name = "نمایش به‌عنوان ویژه")]
        public bool IsFeatured { get; set; }

        [MaxLength(500)]
        [Display(Name = "توضیحات")]
        public string? Description { get; set; }
    }
}
