using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SimCardShop.Data;
using SimCardShop.Models;

namespace SimCardShop.Pages;

public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _db;

    public IndexModel(ApplicationDbContext db)
    {
        _db = db;
    }

    public List<SimCard> FeaturedSimCards { get; set; } = new();
    public List<SimCard> LatestSimCards { get; set; } = new();

    public int RandomCount { get; set; }
    public int SimilarCount { get; set; }
    public int SpecialCount { get; set; }
    public int EconomicCount { get; set; }
    public int HamrahAvalCount { get; set; }
    public int IrancellCount { get; set; }
    public int RightelCount { get; set; }

    public record FaqItem(string Question, string Answer);

    public List<FaqItem> Faqs { get; } = new()
    {
        new FaqItem("چطور شماره رند یا مشابه مورد نظرم را پیدا کنم؟", "کافیست از باکس جستجو در بالای صفحه، بخشی از شماره یا پیش‌شماره دلخواه را وارد کنید و از فیلترهای اپراتور، نوع شماره و قیمت برای محدود کردن نتایج استفاده کنید."),
        new FaqItem("خرید شماره چقدر طول می‌کشد؟", "پس از ثبت سفارش، کارشناسان دفتر در سریع‌ترین زمان ممکن با شما تماس می‌گیرند و مراحل نهایی خرید و تحویل شماره را هماهنگ می‌کنند."),
        new FaqItem("آیا قیمت‌ها نهایی و بدون هزینه پنهان است؟", "بله، قیمت درج‌شده برای هر شماره، قیمت نهایی و شفاف است و هیچ هزینه اضافه‌ای در فرآیند خرید از شما دریافت نمی‌شود."),
        new FaqItem("امکان خرید شماره دائمی و اعتباری هر دو وجود دارد؟", "بله، در فهرست شماره‌ها می‌توانید با فیلتر «دائمی / اعتباری» بین دو نوع خط مورد نظرتان انتخاب کنید."),
    };

    public async Task OnGetAsync()
    {
        var available = _db.SimCards.Where(s => s.Status == SimCardStatus.Available);

        FeaturedSimCards = await available
            .Where(s => s.IsFeatured)
            .OrderByDescending(s => s.CreatedAt)
            .Take(8)
            .ToListAsync();

        LatestSimCards = await available
            .OrderByDescending(s => s.CreatedAt)
            .Take(8)
            .ToListAsync();

        RandomCount = await available.CountAsync(s => s.RandomnessType == RandomnessType.Simple || s.RandomnessType == RandomnessType.Sequential);
        SimilarCount = await available.CountAsync(s => s.RandomnessType == RandomnessType.Paired || s.RandomnessType == RandomnessType.Mirror);
        SpecialCount = await available.CountAsync(s => s.RandomnessType == RandomnessType.Special);
        EconomicCount = await available.CountAsync(s => s.RandomnessType == RandomnessType.Economic);

        HamrahAvalCount = await available.CountAsync(s => s.Operator == OperatorType.HamrahAval);
        IrancellCount = await available.CountAsync(s => s.Operator == OperatorType.Irancell);
        RightelCount = await available.CountAsync(s => s.Operator == OperatorType.Rightel);
    }
}
