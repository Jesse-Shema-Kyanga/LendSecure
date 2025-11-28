using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using LendSecure.Data;
using Microsoft.EntityFrameworkCore;

namespace LendSecure.Pages.Borrower
{
    public class DashboardModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DashboardModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public string UserEmail { get; set; }
        public decimal WalletBalance { get; set; }
        public int ActiveLoansCount { get; set; }
        public string KYCStatus { get; set; }
        public DateTime CreatedAt { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            // Check if user is logged in
            var userIdString = HttpContext.Session.GetString("UserId");
            var userRole = HttpContext.Session.GetString("UserRole");

            if (string.IsNullOrEmpty(userIdString) || userRole != "Borrower")
            {
                return RedirectToPage("/Account/Login");
            }

            var userId = Guid.Parse(userIdString);

            // Get user info
            var user = await _context.Users
                .Include(u => u.Wallets)
                .Include(u => u.KYCDocuments)
                .Include(u => u.LoanRequests)
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null)
            {
                return RedirectToPage("/Account/Login");
            }

            // Set properties
            UserEmail = user.Email;
            CreatedAt = user.CreatedAt;

            // Get wallet balance
            var wallet = user.Wallets.FirstOrDefault();
            WalletBalance = wallet?.Balance ?? 0;

            // Get active loans count
            ActiveLoansCount = user.LoanRequests
                .Count(l => l.Status == "Approved" || l.Status == "Funded" || l.Status == "Repaying");

            // Get KYC status
            var kycDoc = user.KYCDocuments
                .OrderByDescending(k => k.ReviewedAt)
                .FirstOrDefault();

            KYCStatus = kycDoc?.Status ?? "Not Submitted";

            return Page();
        }
    }
}