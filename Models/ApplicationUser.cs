using Microsoft.AspNetCore.Identity;

namespace SimCardShop.Models;

/// <summary>
/// کاربر سیستم — در نسخه اول فقط Admin از این استفاده می‌کند،
/// کاربران عادی نیازی به ثبت‌نام ندارند.
/// </summary>
public class ApplicationUser : IdentityUser
{
    public string? DisplayName { get; set; }
}
