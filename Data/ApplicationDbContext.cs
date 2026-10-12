using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SimCardShop.Models;

namespace SimCardShop.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<SimCard> SimCards => Set<SimCard>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<SmsCampaign> SmsCampaigns => Set<SmsCampaign>();
    public DbSet<SmsRecipient> SmsRecipients => Set<SmsRecipient>();
    public DbSet<Service> Services => Set<Service>();
    public DbSet<SiteSetting> SiteSettings => Set<SiteSetting>();
    public DbSet<VisitLog> VisitLogs => Set<VisitLog>();
    public DbSet<SmsTemplate> SmsTemplates => Set<SmsTemplate>();
    public DbSet<SmsOptOut> SmsOptOuts => Set<SmsOptOut>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<CustomerActivity> CustomerActivities => Set<CustomerActivity>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<SimCard>(entity =>
        {
            entity.HasIndex(s => s.Number).IsUnique();
            entity.HasIndex(s => s.Status);
            entity.HasIndex(s => s.Operator);
        });

        builder.Entity<Order>(entity =>
        {
            entity.HasIndex(o => o.OrderCode).IsUnique();
            entity.HasIndex(o => o.PaymentAuthority);

            entity.HasOne(o => o.HandledByUser)
                  .WithMany()
                  .HasForeignKey(o => o.HandledByUserId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(o => o.Customer)
                  .WithMany(c => c.Orders)
                  .HasForeignKey(o => o.CustomerId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<OrderItem>(entity =>
        {
            entity.HasOne(oi => oi.Order)
                  .WithMany(o => o.Items)
                  .HasForeignKey(oi => oi.OrderId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(oi => oi.SimCard)
                  .WithMany(s => s.OrderItems)
                  .HasForeignKey(oi => oi.SimCardId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<SmsRecipient>(entity =>
        {
            entity.HasOne(r => r.SmsCampaign)
                  .WithMany(c => c.Recipients)
                  .HasForeignKey(r => r.SmsCampaignId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<VisitLog>(entity =>
        {
            entity.HasOne(v => v.SimCard)
                  .WithMany()
                  .HasForeignKey(v => v.SimCardId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<SmsOptOut>(entity =>
        {
            entity.HasIndex(o => o.Mobile).IsUnique();
        });

        builder.Entity<Customer>(entity =>
        {
            entity.HasIndex(c => c.Mobile).IsUnique();

            entity.HasOne(c => c.AssignedToUser)
                  .WithMany()
                  .HasForeignKey(c => c.AssignedToUserId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<CustomerActivity>(entity =>
        {
            entity.HasOne(a => a.Customer)
                  .WithMany(c => c.Activities)
                  .HasForeignKey(a => a.CustomerId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(a => a.CreatedByUser)
                  .WithMany()
                  .HasForeignKey(a => a.CreatedByUserId)
                  .OnDelete(DeleteBehavior.SetNull);
        });
    }
}
