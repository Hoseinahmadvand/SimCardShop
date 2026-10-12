using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SimCardShop.Data;
using SimCardShop.Models;

namespace SimCardShop.Pages.Admin.SimCards;

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
        var digits = new string(Input.Number.Where(char.IsDigit).ToArray());

        if (digits.Length is < 10 or > 12)
        {
            ModelState.AddModelError("Input.Number", "شماره باید بین ۱۰ تا ۱۲ رقم باشد.");
        }
        else if (await _db.SimCards.AnyAsync(s => s.Number == digits))
        {
            ModelState.AddModelError("Input.Number", "این شماره قبلاً در سیستم ثبت شده است.");
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var sim = new SimCard
        {
            Number = digits,
            Operator = Input.Operator,
            Kind = Input.Kind,
            Usage = Input.Usage,
            RandomnessType = Input.RandomnessType,
            Price = Input.Price,
            DiscountPercent = Input.DiscountPercent,
            Status = Input.Status,
            IsFeatured = Input.IsFeatured,
            Description = Input.Description
        };

        _db.SimCards.Add(sim);
        await _db.SaveChangesAsync();

        TempData["StatusMessage"] = $"شماره {sim.DisplayNumber} با موفقیت ثبت شد.";
        return RedirectToPage("/Admin/SimCards/Index");
    }

    public class InputModel
    {
        [Required(ErrorMessage = "شماره را وارد کنید.")]
        [Display(Name = "شماره (بدون فاصله)")]
        public string Number { get; set; } = string.Empty;

        [Display(Name = "اپراتور")]
        public OperatorType Operator { get; set; } = OperatorType.HamrahAval;

        [Display(Name = "نوع سیم‌کارت")]
        public SimCardKind Kind { get; set; } = SimCardKind.Permanent;

        [Display(Name = "وضعیت کارکرد")]
        public UsageState Usage { get; set; } = UsageState.Fresh;

        [Display(Name = "نوع رندی")]
        public RandomnessType RandomnessType { get; set; } = RandomnessType.Simple;

        [Required(ErrorMessage = "قیمت را وارد کنید.")]
        [Range(0, 100_000_000_000, ErrorMessage = "قیمت معتبر نیست.")]
        [Display(Name = "قیمت (تومان)")]
        public decimal Price { get; set; }

        [Range(0, 100, ErrorMessage = "درصد تخفیف باید بین ۰ تا ۱۰۰ باشد.")]
        [Display(Name = "درصد تخفیف")]
        public int DiscountPercent { get; set; }

        [Display(Name = "وضعیت موجودی")]
        public SimCardStatus Status { get; set; } = SimCardStatus.Available;

        [Display(Name = "نمایش به‌عنوان ویژه")]
        public bool IsFeatured { get; set; }

        [MaxLength(500)]
        [Display(Name = "توضیحات")]
        public string? Description { get; set; }
    }
}
