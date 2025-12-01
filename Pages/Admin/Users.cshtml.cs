using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using LendSecure.Data;
using LendSecure.Models;
using Microsoft.EntityFrameworkCore;

namespace LendSecure.Pages.Admin
{
    public class UsersModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public UsersModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<User> Users { get; set; } = new();
        public int TotalUsers { get; set; }
        public int BorrowersCount { get; set; }
        public int LendersCount { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            // Check if user is Admin
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "Admin")
            {
                return RedirectToPage("/Account/Login");
            }

            // Get all users with related data
            Users = await _context.Users
                .Include(u => u.KYCDocuments)
                .Include(u => u.Wallets)
                .OrderByDescending(u => u.CreatedAt)
                .ToListAsync();

            // Calculate stats
            TotalUsers = Users.Count;
            BorrowersCount = Users.Count(u => u.Role == "Borrower");
            LendersCount = Users.Count(u => u.Role == "Lender");

            return Page();
        }
    }
}