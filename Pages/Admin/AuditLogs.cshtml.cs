using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using LendSecure.Data;
using LendSecure.Models;
using Microsoft.EntityFrameworkCore;

namespace LendSecure.Pages.Admin
{
    public class AuditLogsModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public AuditLogsModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<AuditLog> Logs { get; set; } = new();
        public int TotalLogs { get; set; }
        public int TodayLogs { get; set; }
        public int UniqueUsers { get; set; }
        public int LastHourLogs { get; set; }
        public string ActionFilter { get; set; }
        public string DateFilter { get; set; }

        public async Task<IActionResult> OnGetAsync(string actionFilter = "", string dateFilter = "week")
        {
            // Check if user is Admin
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "Admin")
            {
                return RedirectToPage("/Account/Login");
            }

            ActionFilter = actionFilter;
            DateFilter = dateFilter;

            // Get total stats
            TotalLogs = await _context.AuditLogs.CountAsync();
            TodayLogs = await _context.AuditLogs
                .Where(l => l.CreatedAt.Date == DateTime.UtcNow.Date)
                .CountAsync();
            UniqueUsers = await _context.AuditLogs
                .Select(l => l.UserId)
                .Distinct()
                .CountAsync();
            LastHourLogs = await _context.AuditLogs
                .Where(l => l.CreatedAt >= DateTime.UtcNow.AddHours(-1))
                .CountAsync();

            // Build query with filters
            var query = _context.AuditLogs
                .Include(l => l.User)
                .AsQueryable();

            // Date filter
            if (dateFilter == "today")
            {
                query = query.Where(l => l.CreatedAt.Date == DateTime.UtcNow.Date);
            }
            else if (dateFilter == "week")
            {
                query = query.Where(l => l.CreatedAt >= DateTime.UtcNow.AddDays(-7));
            }
            else if (dateFilter == "month")
            {
                query = query.Where(l => l.CreatedAt >= DateTime.UtcNow.AddDays(-30));
            }

            // Action filter
            if (!string.IsNullOrEmpty(actionFilter))
            {
                query = query.Where(l => l.Action.Contains(actionFilter));
            }

            Logs = await query
                .OrderByDescending(l => l.CreatedAt)
                .Take(100) // Limit to last 100 for performance
                .ToListAsync();

            return Page();
        }
    }
}