using Microsoft.EntityFrameworkCore;
using SimCardShop.Data;
using SimCardShop.Models;

namespace SimCardShop.Helpers;

public static class CrmHelper
{
    /// <summary>
    /// مشتری را بر اساس شماره موبایل پیدا می‌کند؛ اگر وجود نداشت، یک لید جدید می‌سازد.
    /// اگر مشتری از قبل وجود داشت و نام جدیدی داده شده، نام به‌روزرسانی نمی‌شود
    /// تا اطلاعات ثبت‌شده قبلی توسط کارشناس فروش بازنویسی نشود.
    /// </summary>
    public static async Task<Customer> FindOrCreateCustomerAsync(ApplicationDbContext db, string mobile, string fullName)
    {
        var customer = await db.Customers.FirstOrDefaultAsync(c => c.Mobile == mobile);
        if (customer is not null) return customer;

        customer = new Customer
        {
            Mobile = mobile,
            FullName = string.IsNullOrWhiteSpace(fullName) ? mobile : fullName,
            Status = CustomerStatus.Lead
        };
        db.Customers.Add(customer);
        await db.SaveChangesAsync();

        return customer;
    }

    public static void AddActivity(ApplicationDbContext db, Customer customer, ActivityType type, string text, string? byUserId = null)
    {
        db.CustomerActivities.Add(new CustomerActivity
        {
            CustomerId = customer.Id,
            Type = type,
            Text = text,
            CreatedByUserId = byUserId
        });
    }
}
