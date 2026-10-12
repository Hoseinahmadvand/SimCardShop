using System.ComponentModel.DataAnnotations;

namespace SimCardShop.Models;

public class SmsCampaign
{
    public int Id { get; set; }

    [MaxLength(150)]
    public string Title { get; set; } = string.Empty;

    /// <summary>در صورت تبلیغ یک شماره خاص</summary>
    public int? SimCardId { get; set; }
    public SimCard? SimCard { get; set; }

    [Required]
    public string MessageText { get; set; } = string.Empty;

    public SmsCampaignStatus Status { get; set; } = SmsCampaignStatus.Draft;

    public int TotalRecipients { get; set; }
    public int SentCount { get; set; }
    public int FailedCount { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? SentAt { get; set; }

    public ICollection<SmsRecipient> Recipients { get; set; } = new List<SmsRecipient>();
}

public class SmsRecipient
{
    public int Id { get; set; }

    public int SmsCampaignId { get; set; }
    public SmsCampaign? SmsCampaign { get; set; }

    [Required, MaxLength(15)]
    public string Mobile { get; set; } = string.Empty;

    public SmsRecipientStatus Status { get; set; } = SmsRecipientStatus.Pending;

    public string? ErrorMessage { get; set; }

    public DateTime? SentAt { get; set; }

    /// <summary>مشترک از دریافت پیامک تبلیغاتی انصراف داده — لیست حذف سراسری</summary>
    public bool OptedOut { get; set; }
}
