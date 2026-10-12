using Microsoft.AspNetCore.Identity;
using SimCardShop.Models;

namespace SimCardShop.Data;

public static class SeedData
{
    public static async Task InitializeAsync(IServiceProvider services)
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
       // await context.Database.EnsureCreatedAsync();

        await SeedAdminAsync(services);
        SeedSimCards(context);
        SeedServices(context);
        SeedSiteSettings(context);
        SeedSmsTemplates(context);
        SeedCustomers(context);

        await context.SaveChangesAsync();
    }

    private static async Task SeedAdminAsync(IServiceProvider services)
    {
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

        const string adminRole = "Admin";
        if (!await roleManager.RoleExistsAsync(adminRole))
        {
            await roleManager.CreateAsync(new IdentityRole(adminRole));
        }

        const string staffRole = "Staff";
        if (!await roleManager.RoleExistsAsync(staffRole))
        {
            await roleManager.CreateAsync(new IdentityRole(staffRole));
        }

        const string adminEmail = "admin@example.ir";
        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        if (adminUser is null)
        {
            adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                DisplayName = "مدیر سیستم",
                EmailConfirmed = true
            };

            // نکته امنیتی: این رمز عبور فقط برای Demo است.
            // پیش از انتشار روی سرور واقعی حتماً آن را تغییر دهید.
            var result = await userManager.CreateAsync(adminUser, "Admin@12345");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, adminRole);
            }
        }
    }

    private static void SeedSimCards(ApplicationDbContext context)
    {
        if (context.SimCards.Any()) return;

        var samples = new List<SimCard>
        {
            new()
            {
                Number = "09121234567",
                Operator = OperatorType.HamrahAval,
                Kind = SimCardKind.Permanent,
                Usage = UsageState.Used,
                RandomnessType = RandomnessType.Sequential,
                Price = 85_000_000,
                DiscountPercent = 5,
                Status = SimCardStatus.Available,
                IsFeatured = true,
                Description = "شماره ترتیبی همراه اول، مناسب کسب‌وکار."
            },
            new()
            {
                Number = "09121112233",
                Operator = OperatorType.HamrahAval,
                Kind = SimCardKind.Permanent,
                Usage = UsageState.Fresh,
                RandomnessType = RandomnessType.Paired,
                Price = 62_000_000,
                DiscountPercent = 0,
                Status = SimCardStatus.Available,
                IsFeatured = false,
                Description = "رند جفتی، صفر و کاملاً تمیز."
            },
            new()
            {
                Number = "09200936136",
                Operator = OperatorType.Irancell,
                Kind = SimCardKind.Permanent,
                Usage = UsageState.Fresh,
                RandomnessType = RandomnessType.Special,
                Price = 12_500_000,
                DiscountPercent = 10,
                Status = SimCardStatus.Available,
                IsFeatured = true,
                Description = "شماره ویژه ایرانسل با ترکیب ارقام کمیاب."
            },
            new()
            {
                Number = "09351234567",
                Operator = OperatorType.Irancell,
                Kind = SimCardKind.Credit,
                Usage = UsageState.Fresh,
                RandomnessType = RandomnessType.Sequential,
                Price = 45_000_000,
                DiscountPercent = 0,
                Status = SimCardStatus.Reserved,
                IsFeatured = false,
                Description = "شماره اعتباری با ترتیب رقومی بالا."
            },
            new()
            {
                Number = "09901234567",
                Operator = OperatorType.Rightel,
                Kind = SimCardKind.Permanent,
                Usage = UsageState.Fresh,
                RandomnessType = RandomnessType.Economic,
                Price = 3_200_000,
                DiscountPercent = 0,
                Status = SimCardStatus.Available,
                IsFeatured = false,
                Description = "گزینه اقتصادی رایتل برای استفاده روزمره."
            },
            new()
            {
                Number = "09121001001",
                Operator = OperatorType.HamrahAval,
                Kind = SimCardKind.Permanent,
                Usage = UsageState.Used,
                RandomnessType = RandomnessType.Mirror,
                Price = 210_000_000,
                DiscountPercent = 8,
                Status = SimCardStatus.Available,
                IsFeatured = true,
                Description = "شماره آینه‌ای کلکسیونی، بسیار کمیاب."
            }
        };

        context.SimCards.AddRange(samples);
    }

    private static void SeedServices(ApplicationDbContext context)
    {
        if (context.Services.Any()) return;

        context.Services.AddRange(
            new Service { Title = "خدمات سیم‌کارت", Description = "صدور، فعال‌سازی و پشتیبانی سیم‌کارت‌های تمامی اپراتورها.", Icon = "sim", SortOrder = 1 },
            new Service { Title = "تعویض سیم‌کارت", Description = "تعویض سیم‌کارت آسیب‌دیده یا مفقودشده در کمترین زمان.", Icon = "swap", SortOrder = 2 },
            new Service { Title = "انتقال مالکیت", Description = "انتقال قانونی مالکیت خط به نام خریدار جدید.", Icon = "transfer", SortOrder = 3 },
            new Service { Title = "خدمات اپراتورها", Description = "پرداخت قبض، شارژ و سایر خدمات اپراتوری.", Icon = "operator", SortOrder = 4 },
            new Service { Title = "خدمات دولت الکترونیک", Description = "ارائه خدمات دولت الکترونیک از طریق دفتر پیشخوان.", Icon = "gov", SortOrder = 5 },
            new Service { Title = "سایر خدمات دفتر", Description = "سایر خدمات اداری و مشاوره‌ای دفتر پیشخوان.", Icon = "more", SortOrder = 6 }
        );
    }

    private static void SeedSiteSettings(ApplicationDbContext context)
    {
        if (context.SiteSettings.Any()) return;

        context.SiteSettings.Add(new SiteSetting
        {
         
            OfficeName = "دفتر خدمات پیشخوان شماره ۱۲۳",
            Phone = "021-12345678",
            Mobile = "09120000000",
            Address = "تهران، خیابان نمونه، پلاک ۱"
        });
    }

    private static void SeedSmsTemplates(ApplicationDbContext context)
    {
        if (context.SmsTemplates.Any()) return;

        context.SmsTemplates.AddRange(
            new SmsTemplate
            {
                Name = "تبلیغ شماره تکی",
                Text = "❤️ فروش مشابه خط شما: {Number}\n\nمشاهده و خرید:\n{Link}\n\nلغو11"
            },
            new SmsTemplate
            {
                Name = "فروش ویژه با قیمت",
                Text = "فروش ویژه شماره {Number}\nقیمت: {Price}\nمشاهده:\n{Link}\n\nلغو11"
            }
        );
    }

    private static void SeedCustomers(ApplicationDbContext context)
    {
        if (context.Customers.Any()) return;

        var customer1 = new Customer
        {
            FullName = "علی رضایی",
            Mobile = "09121110000",
            Status = CustomerStatus.Contacted,
            NextFollowUpAt = DateTime.UtcNow.AddDays(1)
        };
        var customer2 = new Customer
        {
            FullName = "سارا احمدی",
            Mobile = "09351110000",
            Status = CustomerStatus.Lead
        };

        context.Customers.AddRange(customer1, customer2);

        context.CustomerActivities.AddRange(
            new CustomerActivity
            {
                Customer = customer1,
                Type = ActivityType.Call,
                Text = "تماس گرفته شد، علاقه‌مند به شماره‌های رند همراه اول است. فردا پیگیری شود."
            },
            new CustomerActivity
            {
                Customer = customer2,
                Type = ActivityType.Note,
                Text = "از طریق اینستاگرام درخواست قیمت شماره رند داده است."
            }
        );
    }
}
