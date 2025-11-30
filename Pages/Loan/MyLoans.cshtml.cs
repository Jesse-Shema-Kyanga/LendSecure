using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using LendSecure.Data;
using LendSecure.Models;
using Microsoft.EntityFrameworkCore;

namespace LendSecure.Pages.Loan
{
    public class MyLoansModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public MyLoansModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<LoanRequest> MyLoans { get; set; } = new();
        public string UserEmail { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var userIdString = HttpContext.Session.GetString("UserId");
            var userRole = HttpContext.Session.GetString("UserRole");

            if (string.IsNullOrEmpty(userIdString) || userRole != "Borrower")
            {
                return RedirectToPage("/Account/Login");
            }

            var userId = Guid.Parse(userIdString);

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null)
            {
                return RedirectToPage("/Account/Login");
            }

            UserEmail = user.Email;

            MyLoans = await _context.LoanRequests
                .Include(l => l.Fundings)
                .Include(l => l.Repayments)
                .Where(l => l.BorrowerId == userId)
                .OrderByDescending(l => l.CreatedAt)
                .ToListAsync();

            return Page();
        }
    }
}

