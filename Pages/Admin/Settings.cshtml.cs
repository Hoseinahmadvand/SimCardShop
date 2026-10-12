using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SimCardShop.Data;
using SimCardShop.Models;

namespace SimCardShop.Pages.Admin;

public class SettingsModel : PageModel
{
    private readonly ApplicationDbContext _db;

    public SettingsModel(ApplicationDbContext db)
    {
        _db = db;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    [TempData]
    public string? StatusMessage { get; set; }

    public async Task OnGetAsync()
    {
        var settings = await _db.SiteSettings.FirstOrDefaultAsync() ?? new SiteSetting();

        Input = new InputModel
        {
            OfficeName = settings.OfficeName,
            LogoUrl = settings.LogoUrl,
            Phone = settings.Phone,
            Mobile = settings.Mobile,
            Address = settings.Address,
            WhatsAppUrl = settings.WhatsAppUrl,
            TelegramUrl = settings.TelegramUrl,
            InstagramUrl = settings.InstagramUrl,
            MapUrl = settings.MapUrl,
            SmsApiUrl = settings.SmsApiUrl,
            SmsApiKey = settings.SmsApiKey,
            SmsSenderNumber = settings.SmsSenderNumber,
            PaymentEnabled = settings.PaymentEnabled,
            ZarinPalMerchantId = settings.ZarinPalMerchantId,
            ZarinPalSandbox = settings.ZarinPalSandbox
        };
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();

        var settings = await _db.SiteSettings.FirstOrDefaultAsync();
        if (settings is null)
        {
            settings = new SiteSetting();
            _db.SiteSettings.Add(settings);
        }

        settings.OfficeName = Input.OfficeName;
        settings.LogoUrl = Input.LogoUrl;
        settings.Phone = Input.Phone;
        settings.Mobile = Input.Mobile;
        settings.Address = Input.Address;
        settings.WhatsAppUrl = Input.WhatsAppUrl;
        settings.TelegramUrl = Input.TelegramUrl;
        settings.InstagramUrl = Input.InstagramUrl;
        settings.MapUrl = Input.MapUrl;
        settings.SmsApiUrl = Input.SmsApiUrl;
        settings.SmsApiKey = Input.SmsApiKey;
        settings.SmsSenderNumber = Input.SmsSenderNumber;
        settings.PaymentEnabled = Input.PaymentEnabled;
        settings.ZarinPalMerchantId = Input.ZarinPalMerchantId;
        settings.ZarinPalSandbox = Input.ZarinPalSandbox;

        await _db.SaveChangesAsync();

        StatusMessage = "تنظیمات با موفقیت ذخیره شد.";
        return RedirectToPage();
    }

    public class InputModel
    {
        [Required(ErrorMessage = "نام دفتر را وارد کنید.")]
        [MaxLength(150)]
        [Display(Name = "نام دفتر")]
        public string OfficeName { get; set; } = string.Empty;

        [MaxLength(300)]
        [Display(Name = "آدرس لوگو")]
        public string? LogoUrl { get; set; }

        [MaxLength(20)]
        [Display(Name = "تلفن ثابت")]
        public string? Phone { get; set; }

        [MaxLength(15)]
        [Display(Name = "موبایل")]
        public string? Mobile { get; set; }

        [MaxLength(300)]
        [Display(Name = "آدرس")]
        public string? Address { get; set; }

        [MaxLength(300)]
        [Display(Name = "لینک واتس‌اپ")]
        public string? WhatsAppUrl { get; set; }

        [MaxLength(300)]
        [Display(Name = "لینک تلگرام")]
        public string? TelegramUrl { get; set; }

        [MaxLength(300)]
        [Display(Name = "لینک اینستاگرام")]
        public string? InstagramUrl { get; set; }

        [MaxLength(500)]
        [Display(Name = "لینک نقشه")]
        public string? MapUrl { get; set; }

        [MaxLength(300)]
        [Display(Name = "آدرس API پنل پیامک")]
        public string? SmsApiUrl { get; set; }

        [MaxLength(300)]
        [Display(Name = "کلید API پنل پیامک")]
        public string? SmsApiKey { get; set; }

        [MaxLength(20)]
        [Display(Name = "شماره فرستنده پیامک")]
        public string? SmsSenderNumber { get; set; }

        [Display(Name = "فعال‌سازی پرداخت آنلاین")]
        public bool PaymentEnabled { get; set; }

        [MaxLength(80)]
        [Display(Name = "کد پذیرنده زرین‌پال (Merchant ID)")]
        public string? ZarinPalMerchantId { get; set; }

        [Display(Name = "حالت آزمایشی (Sandbox)")]
        public bool ZarinPalSandbox { get; set; } = true;
    }
}
