using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using SimCardShop.Data;
using SimCardShop.Models;
using SimCardShop.Services;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// --- Database ---
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("رشته اتصال 'DefaultConnection' در appsettings.json تنظیم نشده است.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// --- Identity (فقط برای ادمین استفاده می‌شود) ---
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequiredLength = 8;
        options.SignIn.RequireConfirmedAccount = false;

        options.Lockout.AllowedForNewUsers = true;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Admin/Login";
    options.AccessDeniedPath = "/Admin/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
});

builder.Services.AddAuthorization(options =>
{
    // پنل مدیریت به‌طور کلی برای هر دو نقش Admin و Staff باز است
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin", "Staff"));

    // مدیریت کاربران و تنظیمات حساس فقط برای نقش Admin
    options.AddPolicy("SuperAdminOnly", policy => policy.RequireRole("Admin"));
});

// --- Razor Pages ---
builder.Services.AddRazorPages(options =>
{
    // کل پوشه پنل مدیریت نیازمند نقش Admin یا Staff است، به‌جز صفحه ورود.
    options.Conventions.AuthorizeFolder("/Admin", "AdminOnly");
    options.Conventions.AllowAnonymousToPage("/Admin/Login");

    // مدیریت کاربران و تنظیمات سایت فقط برای نقش Admin (نه Staff)
    options.Conventions.AuthorizeFolder("/Admin/Users", "SuperAdminOnly");
    options.Conventions.AuthorizePage("/Admin/Settings", "SuperAdminOnly");
});

// --- سرویس پیامک ---
// در حالت فعلی، در صورت خالی بودن تنظیمات پنل پیامک، به‌صورت Mock عمل می‌کند.
//builder.Services.AddHttpClient<ISmsService, SmsService>();
builder.Services.AddScoped<ISmsService, SmsService>();
// --- درگاه پرداخت ---
// در حالت فعلی، در صورت خالی بودن Merchant ID در تنظیمات سایت، درخواست پرداخت آنلاین
// رد می‌شود و صفحه خرید به‌صورت خودکار به جریان قبلی (ثبت سفارش و پیگیری تلفنی) برمی‌گردد.
builder.Services.AddHttpClient<IPaymentGateway, ZarinPalGateway>();

// --- کش حافظه‌ای، برای محدودسازی نرخ درخواست فرم‌های عمومی حساس ---
builder.Services.AddMemoryCache();

// --- محدودسازی نرخ درخواست برای ثبت کلیک (جلوگیری از سوءاستفاده/اسپم) ---
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddFixedWindowLimiter("ClickTracking", limiterOptions =>
    {
        limiterOptions.PermitLimit = 30;
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.QueueLimit = 0;
    });
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    await next();
});

app.UseStaticFiles();

app.UseRouting();

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

// نقشه سایت پویا برای موتورهای جستجو
app.MapGet("/sitemap.xml", async (ApplicationDbContext db, IConfiguration config) =>
{
    var baseUrl = (config["SiteBaseUrl"] ?? "https://example.ir").TrimEnd('/');
    var urls = new List<string> { "", "/simcards", "/services", "/about", "/contact" };

    var numbers = await db.SimCards
        .Where(s => s.Status != SimCardStatus.Disabled)
        .Select(s => s.Number)
        .ToListAsync();

    var sb = new System.Text.StringBuilder();
    sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
    sb.Append("<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">");

    foreach (var path in urls)
    {
        sb.Append($"<url><loc>{baseUrl}{path}</loc></url>");
    }
    foreach (var number in numbers)
    {
        sb.Append($"<url><loc>{baseUrl}/i/{number}</loc></url>");
    }

    sb.Append("</urlset>");

    return Results.Text(sb.ToString(), "application/xml");
});

// ثبت سبک کلیک روی «تماس» و «واتس‌اپ» در صفحه هر شماره — خارج از فیلتر Antiforgery صفحات
// چون این درخواست به‌صورت fetch/beacon و بدون توکن ارسال می‌شود. فقط شمارنده را افزایش می‌دهد.
app.MapPost("/api/simcards/track", async (HttpContext http, ApplicationDbContext db) =>
{
    var form = await http.Request.ReadFormAsync();
    var number = form["number"].ToString();
    var type = form["type"].ToString();

    var sim = await db.SimCards.FirstOrDefaultAsync(s => s.Number == number);
    if (sim is null) return Results.NotFound();

    switch (type)
    {
        case "call":
            sim.CallClickCount++;
            break;
        case "whatsapp":
            sim.WhatsAppClickCount++;
            break;
        default:
            return Results.BadRequest();
    }

    await db.SaveChangesAsync();
    return Results.Ok(new { ok = true });
}).RequireRateLimiting("ClickTracking");

// اجرای Seed داده‌های نمونه در محیط توسعه
using (var scope = app.Services.CreateScope())
{
    await SeedData.InitializeAsync(scope.ServiceProvider);
}

app.Run();
