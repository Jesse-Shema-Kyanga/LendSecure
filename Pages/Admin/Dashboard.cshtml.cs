using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using LendSecure.Data;
using LendSecure.Models;
using Microsoft.EntityFrameworkCore;

namespace LendSecure.Pages.Admin
{
    public class DashboardModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DashboardModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public string UserEmail { get; set; }
        public DateTime CreatedAt { get; set; }
        public int TotalUsers { get; set; }
        public int PendingKYCCount { get; set; }
        public int PendingLoansCount { get; set; }
        public int ActiveLoansCount { get; set; }
        public List<KYCDocument> RecentKYCDocs { get; set; } = new();
        public List<LoanRequest> RecentLoanRequests { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            // Check if user is logged in as Admin
            var userIdString = HttpContext.Session.GetString("UserId");
            var userRole = HttpContext.Session.GetString("UserRole");

            if (string.IsNullOrEmpty(userIdString) || userRole != "Admin")
            {
                return RedirectToPage("/Account/Login");
            }

            var userId = Guid.Parse(userIdString);

            // Get admin user info
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null)
            {
                return RedirectToPage("/Account/Login");
            }

            UserEmail = user.Email;
            CreatedAt = user.CreatedAt;

            // Get platform statistics
            TotalUsers = await _context.Users.CountAsync();

            PendingKYCCount = await _context.KYCDocuments
                .Where(k => k.Status == "Pending")
                .CountAsync();

            PendingLoansCount = await _context.LoanRequests
                .Where(l => l.Status == "Pending")
                .CountAsync();

            ActiveLoansCount = await _context.LoanRequests
                .Where(l => l.Status == "Approved" || l.Status == "Funded" || l.Status == "Repaying")
                .CountAsync();

            // Get recent KYC documents
            RecentKYCDocs = await _context.KYCDocuments
                .Include(k => k.User)
                .Where(k => k.Status == "Pending")
                .OrderByDescending(k => k.DocId)
                .Take(5)
                .ToListAsync();

            // Get recent loan requests
            RecentLoanRequests = await _context.LoanRequests
                .Include(l => l.Borrower)
                .Where(l => l.Status == "Pending")
                .OrderByDescending(l => l.CreatedAt)
                .Take(5)
                .ToListAsync();

            return Page();
        }
    }
}