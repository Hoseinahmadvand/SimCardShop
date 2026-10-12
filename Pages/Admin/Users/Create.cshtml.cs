using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SimCardShop.Models;

namespace SimCardShop.Pages.Admin.Users;

public class CreateModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;

    public CreateModel(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();

        var existing = await _userManager.FindByEmailAsync(Input.Email);
        if (existing is not null)
        {
            ModelState.AddModelError("Input.Email", "کاربری با این ایمیل قبلاً ثبت شده است.");
            return Page();
        }

        var user = new ApplicationUser
        {
            UserName = Input.Email,
            Email = Input.Email,
            DisplayName = Input.DisplayName,
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(user, Input.Password);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            return Page();
        }

        await _userManager.AddToRoleAsync(user, Input.Role);

        TempData["StatusMessage"] = $"کاربر {user.Email} با نقش «{(Input.Role == "Admin" ? "مدیر" : "کارشناس فروش")}» ایجاد شد.";
        return RedirectToPage("/Admin/Users/Index");
    }

    public class InputModel
    {
        [Required(ErrorMessage = "ایمیل را وارد کنید.")]
        [EmailAddress(ErrorMessage = "فرمت ایمیل صحیح نیست.")]
        [Display(Name = "ایمیل (برای ورود استفاده می‌شود)")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "نام نمایشی را وارد کنید.")]
        [MaxLength(100)]
        [Display(Name = "نام و نام خانوادگی")]
        public string DisplayName { get; set; } = string.Empty;

        [Required(ErrorMessage = "رمز عبور را وارد کنید.")]
        [MinLength(8, ErrorMessage = "رمز عبور باید حداقل ۸ کاراکتر باشد.")]
        [DataType(DataType.Password)]
        [Display(Name = "رمز عبور اولیه")]
        public string Password { get; set; } = string.Empty;

        [Required]
        [Display(Name = "نقش")]
        public string Role { get; set; } = "Staff";
    }
}
