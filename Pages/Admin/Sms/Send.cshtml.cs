using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SimCardShop.Data;
using SimCardShop.Helpers;
using SimCardShop.Models;
using SimCardShop.Services;

namespace SimCardShop.Pages.Admin.Sms;

public class SendModel : PageModel
{
    private readonly ApplicationDbContext _db;
    private readonly ISmsService _smsService;
    private readonly IConfiguration _configuration;

    public SendModel(ApplicationDbContext db, ISmsService smsService, IConfiguration configuration)
    {
        _db = db;
        _smsService = smsService;
        _configuration = configuration;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public int? SimCardId { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? PrefillMobile { get; set; }

    public List<SimCard> AvailableSimCards { get; set; } = new();
    public List<SmsTemplate> Templates { get; set; } = new();

    // نتیجه پیش‌نمایش، پس از اعتبارسنجی
    public bool PreviewReady { get; set; }
    public List<string> PreviewRecipients { get; set; } = new();
    public string PreviewMessage { get; set; } = string.Empty;
    public int PreviewSegmentCount { get; set; }
    public int SkippedOptedOutCount { get; set; }
    public int SkippedInvalidCount { get; set; }

    // نتیجه نهایی پس از ارسال
    public bool SendCompleted { get; set; }
    public int SentCount { get; set; }
    public int FailedCount { get; set; }

    public async Task OnGetAsync()
    {
        await LoadLookupsAsync();
        if (SimCardId.HasValue)
        {
            Input.SimCardId = SimCardId;
        }
        if (!string.IsNullOrWhiteSpace(PrefillMobile))
        {
            Input.RecipientsText = PrefillMobile;
        }
    }

    public async Task<IActionResult> OnPostPreviewAsync()
    {
        await LoadLookupsAsync();

        var simCard = Input.SimCardId.HasValue
            ? await _db.SimCards.FindAsync(Input.SimCardId.Value)
            : null;

        if (string.IsNullOrWhiteSpace(Input.MessageTemplate))
        {
            ModelState.AddModelError("Input.MessageTemplate", "متن پیامک را وارد کنید.");
            return Page();
        }

        var csvMobiles = new List<string>();
        if (Input.CsvFile is { Length: > 0 })
        {
            using var reader = new StreamReader(Input.CsvFile.OpenReadStream());
            csvMobiles = SmsHelper.ExtractMobiles(await reader.ReadToEndAsync());
        }

        var manualMobiles = SmsHelper.ExtractMobiles(Input.RecipientsText);
        var allCandidates = manualMobiles.Concat(csvMobiles).Distinct().ToList();

        var rawTokenCount = (Input.RecipientsText ?? string.Empty)
            .Split(new[] { '\r', '\n', ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
            .Count();
        SkippedInvalidCount = Math.Max(0, rawTokenCount - manualMobiles.Count);

        if (allCandidates.Count == 0)
        {
            ModelState.AddModelError("Input.RecipientsText", "حداقل یک شماره موبایل معتبر وارد کنید.");
            return Page();
        }

        var optedOut = await _db.SmsOptOuts.Select(o => o.Mobile).ToListAsync();
        var validRecipients = allCandidates.Where(m => !optedOut.Contains(m)).ToList();

        SkippedOptedOutCount = allCandidates.Count(m => optedOut.Contains(m));

        var finalMessage = simCard is not null
            ? SmsHelper.ApplyPlaceholders(Input.MessageTemplate, simCard, _configuration["SiteBaseUrl"] ?? "https://example.ir")
            : Input.MessageTemplate;

        PreviewReady = true;
        PreviewRecipients = validRecipients;
        PreviewMessage = finalMessage;
        PreviewSegmentCount = SmsHelper.EstimateSegmentCount(finalMessage);

        // برای مرحله تأیید در فرم نگه‌داری می‌شود
        Input.ConfirmedRecipients = string.Join("\n", validRecipients);
        Input.ConfirmedMessage = finalMessage;

        return Page();
    }

    public async Task<IActionResult> OnPostSendAsync()
    {
        await LoadLookupsAsync();

        var recipients = (Input.ConfirmedRecipients ?? string.Empty)
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct()
            .ToList();

        if (recipients.Count == 0 || string.IsNullOrWhiteSpace(Input.ConfirmedMessage))
        {
            ModelState.AddModelError(string.Empty, "اطلاعات ارسال نامعتبر است، لطفاً دوباره پیش‌نمایش را انجام دهید.");
            return Page();
        }

        var campaign = new SmsCampaign
        {
            Title = Input.SimCardId.HasValue
                ? $"تبلیغ شماره #{Input.SimCardId}"
                : "کمپین پیامکی عمومی",
            SimCardId = Input.SimCardId,
            MessageText = Input.ConfirmedMessage,
            Status = SmsCampaignStatus.Sending,
            TotalRecipients = recipients.Count
        };

        foreach (var mobile in recipients)
        {
            campaign.Recipients.Add(new SmsRecipient { Mobile = mobile, Status = SmsRecipientStatus.Pending });
        }

        _db.SmsCampaigns.Add(campaign);
        await _db.SaveChangesAsync();

        var sentCount = 0;
        var failedCount = 0;

        var matchedCustomers = await _db.Customers
            .Where(c => recipients.Contains(c.Mobile))
            .ToListAsync();

        foreach (var recipient in campaign.Recipients)
        {
            var result = await _smsService.SendAsync(recipient.Mobile, campaign.MessageText);
            if (result.Success)
            {
                recipient.Status = SmsRecipientStatus.Sent;
                recipient.SentAt = DateTime.UtcNow;
                sentCount++;
            }
            else
            {
                recipient.Status = SmsRecipientStatus.Failed;
                recipient.ErrorMessage = result.ErrorMessage;
                failedCount++;
            }

            var matchedCustomer = matchedCustomers.FirstOrDefault(c => c.Mobile == recipient.Mobile);
            if (matchedCustomer is not null)
            {
                CrmHelper.AddActivity(_db, matchedCustomer, ActivityType.Sms,
                    result.Success ? "پیامک تبلیغاتی ارسال شد." : "ارسال پیامک تبلیغاتی ناموفق بود.");
            }
        }

        campaign.SentCount = sentCount;
        campaign.FailedCount = failedCount;
        campaign.Status = SmsCampaignStatus.Completed;
        campaign.SentAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        SendCompleted = true;
        SentCount = sentCount;
        FailedCount = failedCount;

        Input = new InputModel();
        return Page();
    }

    private async Task LoadLookupsAsync()
    {
        AvailableSimCards = await _db.SimCards
            .Where(s => s.Status != SimCardStatus.Disabled)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();

        Templates = await _db.SmsTemplates.OrderByDescending(t => t.CreatedAt).ToListAsync();
    }

    public class InputModel
    {
        [Display(Name = "شماره تبلیغ‌شونده (اختیاری)")]
        public int? SimCardId { get; set; }

        [Display(Name = "متن پیامک")]
        public string MessageTemplate { get; set; } =
            "❤️ فروش مشابه خط شما: {Number}\n\nمشاهده و خرید:\n{Link}\n\nلغو11";

        [Display(Name = "شماره موبایل گیرندگان (هر خط یک شماره)")]
        public string? RecipientsText { get; set; }

        [Display(Name = "یا آپلود فایل CSV / متنی")]
        public IFormFile? CsvFile { get; set; }

        // فیلدهای مخفی برای انتقال نتیجه پیش‌نمایش به مرحله ارسال نهایی
        public string? ConfirmedRecipients { get; set; }
        public string? ConfirmedMessage { get; set; }
    }
}
