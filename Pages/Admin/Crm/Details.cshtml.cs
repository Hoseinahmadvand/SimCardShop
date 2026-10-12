using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SimCardShop.Data;
using SimCardShop.Helpers;
using SimCardShop.Models;

namespace SimCardShop.Pages.Admin.Crm;

public class DetailsModel : PageModel
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public DetailsModel(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public Customer Customer { get; set; } = null!;
    public List<CustomerActivity> Activities { get; set; } = new();
    public List<Order> Orders { get; set; } = new();
    public List<ApplicationUser> StaffUsers { get; set; } = new();

    [BindProperty]
    public NoteInput Note { get; set; } = new();

    [BindProperty]
    public ProfileInput Profile { get; set; } = new();

    [TempData]
    public string? StatusMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var customer = await _db.Customers
            .Include(c => c.AssignedToUser)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (customer is null) return NotFound();

        await LoadAsync(customer);
        return Page();
    }

    public async Task<IActionResult> OnPostAddNoteAsync(int id)
    {
        var customer = await _db.Customers.FindAsync(id);
        if (customer is null) return NotFound();

        if (string.IsNullOrWhiteSpace(Note.Text))
        {
            ModelState.AddModelError("Note.Text", "متن یادداشت را وارد کنید.");
            await LoadAsync(customer);
            return Page();
        }

        CrmHelper.AddActivity(_db, customer, Note.Type, Note.Text, _userManager.GetUserId(User));
        await _db.SaveChangesAsync();

        StatusMessage = "یادداشت ثبت شد.";
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostUpdateProfileAsync(int id)
    {
        var customer = await _db.Customers.FindAsync(id);
        if (customer is null) return NotFound();

        var previousStatus = customer.Status;
        var previousAssignee = customer.AssignedToUserId;

        customer.Status = Profile.Status;
        customer.AssignedToUserId = string.IsNullOrEmpty(Profile.AssignedToUserId) ? null : Profile.AssignedToUserId;
        customer.NextFollowUpAt = Profile.NextFollowUpAt;
        customer.UpdatedAt = DateTime.UtcNow;

        if (previousStatus != customer.Status || previousAssignee != customer.AssignedToUserId)
        {
            CrmHelper.AddActivity(_db, customer, ActivityType.StatusChange,
                $"وضعیت به «{DisplayLabels.CustomerStatus(customer.Status)}» تغییر کرد" +
                (previousAssignee != customer.AssignedToUserId ? " و کارشناس مسئول به‌روزرسانی شد." : "."),
                _userManager.GetUserId(User));
        }

        await _db.SaveChangesAsync();

        StatusMessage = "پروفایل مشتری به‌روزرسانی شد.";
        return RedirectToPage(new { id });
    }

    private async Task LoadAsync(Customer customer)
    {
        Customer = customer;

        Activities = await _db.CustomerActivities
            .Include(a => a.CreatedByUser)
            .Where(a => a.CustomerId == customer.Id)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();

        Orders = await _db.Orders
            .Include(o => o.Items)
            .ThenInclude(i => i.SimCard)
            .Where(o => o.CustomerId == customer.Id)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        StaffUsers = await _userManager.Users.OrderBy(u => u.Email).ToListAsync();

        Profile = new ProfileInput
        {
            Status = customer.Status,
            AssignedToUserId = customer.AssignedToUserId,
            NextFollowUpAt = customer.NextFollowUpAt
        };
    }

    public class NoteInput
    {
        public ActivityType Type { get; set; } = ActivityType.Note;

        [Display(Name = "متن یادداشت")]
        public string Text { get; set; } = string.Empty;
    }

    public class ProfileInput
    {
        public CustomerStatus Status { get; set; }
        public string? AssignedToUserId { get; set; }

        [Display(Name = "تاریخ پیگیری بعدی")]
        public DateTime? NextFollowUpAt { get; set; }
    }
}
