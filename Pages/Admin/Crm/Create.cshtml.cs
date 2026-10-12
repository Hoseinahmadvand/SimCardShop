using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SimCardShop.Data;
using SimCardShop.Helpers;
using SimCardShop.Models;

namespace SimCardShop.Pages.Admin.Crm;

public class CreateModel : PageModel
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public CreateModel(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public List<ApplicationUser> StaffUsers { get; set; } = new();

    public async Task OnGetAsync()
    {
        StaffUsers = await _userManager.Users.OrderBy(u => u.Email).ToListAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        StaffUsers = await _userManager.Users.OrderBy(u => u.Email).ToListAsync();

        var digits = new string(Input.Mobile.Where(char.IsDigit).ToArray());
        if (digits.Length != 11 || !digits.StartsWith("09"))
        {
            ModelState.AddModelError("Input.Mobile", "شماره موبایل معتبر نیست، مثلاً 09123456789.");
        }
        else if (await _db.Customers.AnyAsync(c => c.Mobile == digits))
        {
            ModelState.AddModelError("Input.Mobile", "مشتری‌ای با این شماره موبایل قبلاً ثبت شده است.");
        }

        if (!ModelState.IsValid) return Page();

        var customer = new Customer
        {
            FullName = Input.FullName,
            Mobile = digits,
            Email = Input.Email,
            AssignedToUserId = string.IsNullOrEmpty(Input.AssignedToUserId) ? null : Input.AssignedToUserId
        };

        _db.Customers.Add(customer);
        await _db.SaveChangesAsync();

        if (!string.IsNullOrWhiteSpace(Input.InitialNote))
        {
            CrmHelper.AddActivity(_db, customer, ActivityType.Note, Input.InitialNote, _userManager.GetUserId(User));
            await _db.SaveChangesAsync();
        }

        TempData["StatusMessage"] = $"مشتری «{customer.FullName}» اضافه شد.";
        return RedirectToPage("/Admin/Crm/Details", new { id = customer.Id });
    }

    public class InputModel
    {
        [Required(ErrorMessage = "نام و نام خانوادگی را وارد کنید.")]
        [MaxLength(100)]
        [Display(Name = "نام و نام خانوادگی")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "شماره موبایل را وارد کنید.")]
        [Display(Name = "شماره موبایل")]
        public string Mobile { get; set; } = string.Empty;

        [EmailAddress(ErrorMessage = "فرمت ایمیل صحیح نیست.")]
        [Display(Name = "ایمیل (اختیاری)")]
        public string? Email { get; set; }

        [Display(Name = "کارشناس مسئول (اختیاری)")]
        public string? AssignedToUserId { get; set; }

        [Display(Name = "یادداشت اولیه (اختیاری)")]
        public string? InitialNote { get; set; }
    }
}
