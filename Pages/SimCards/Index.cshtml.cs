using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SimCardShop.Data;
using SimCardShop.Models;

namespace SimCardShop.Pages.SimCards;

public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _db;

    public IndexModel(ApplicationDbContext db)
    {
        _db = db;
    }

    [BindProperty(SupportsGet = true)]
    public string? Q { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Operator { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Kind { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Randomness { get; set; }

    [BindProperty(SupportsGet = true)]
    public decimal? MinPrice { get; set; }

    [BindProperty(SupportsGet = true)]
    public decimal? MaxPrice { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Availability { get; set; } // available | sold

    [BindProperty(SupportsGet = true)]
    public string Sort { get; set; } = "newest"; // newest | cheapest | expensive

    [BindProperty(SupportsGet = true)]
    public int Page { get; set; } = 1;

    public const int PageSize = 12;

    public List<SimCard> Results { get; set; } = new();
    public int TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);

    public async Task OnGetAsync()
    {
        var query = _db.SimCards.AsQueryable();

        // جستجوی شماره: کامل، بخشی، پیش‌شماره یا چند رقم خاص
        if (!string.IsNullOrWhiteSpace(Q))
        {
            var digitsOnly = new string(Q.Where(char.IsDigit).ToArray());
            if (digitsOnly.Length > 0)
            {
                query = query.Where(s => s.Number.Contains(digitsOnly));
            }
        }

        if (!string.IsNullOrWhiteSpace(Operator) &&
            Enum.TryParse<OperatorType>(Operator, true, out var operatorValue))
        {
            query = query.Where(s => s.Operator == operatorValue);
        }
        else if (string.Equals(Operator, "hamrahaval", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(s => s.Operator == OperatorType.HamrahAval);
        }
        else if (string.Equals(Operator, "irancell", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(s => s.Operator == OperatorType.Irancell);
        }
        else if (string.Equals(Operator, "rightel", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(s => s.Operator == OperatorType.Rightel);
        }

        if (string.Equals(Kind, "permanent", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(s => s.Kind == SimCardKind.Permanent);
        }
        else if (string.Equals(Kind, "credit", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(s => s.Kind == SimCardKind.Credit);
        }

        switch (Randomness?.ToLowerInvariant())
        {
            case "rare":
                query = query.Where(s => s.RandomnessType == RandomnessType.Simple || s.RandomnessType == RandomnessType.Sequential);
                break;
            case "similar":
                query = query.Where(s => s.RandomnessType == RandomnessType.Paired || s.RandomnessType == RandomnessType.Mirror);
                break;
            case "special":
                query = query.Where(s => s.RandomnessType == RandomnessType.Special);
                break;
            case "economic":
                query = query.Where(s => s.RandomnessType == RandomnessType.Economic);
                break;
        }

        if (MinPrice.HasValue)
        {
            query = query.Where(s => s.Price >= MinPrice.Value);
        }

        if (MaxPrice.HasValue)
        {
            query = query.Where(s => s.Price <= MaxPrice.Value);
        }

        if (string.Equals(Availability, "available", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(s => s.Status == SimCardStatus.Available);
        }
        else if (string.Equals(Availability, "sold", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(s => s.Status == SimCardStatus.Sold);
        }
        else
        {
            // پیش‌فرض: شماره‌های غیرفعال در فهرست عمومی نمایش داده نشوند
            query = query.Where(s => s.Status != SimCardStatus.Disabled);
        }

        query = Sort switch
        {
            "cheapest" => query.OrderBy(s => s.Price),
            "expensive" => query.OrderByDescending(s => s.Price),
            _ => query.OrderByDescending(s => s.CreatedAt)
        };

        TotalCount = await query.CountAsync();

        if (Page < 1) Page = 1;

        Results = await query
            .Skip((Page - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync();
    }
}
