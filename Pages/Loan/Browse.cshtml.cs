using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using LendSecure.Models;
using LendSecure.Data;

namespace LendSecure.Pages.Loan
{
    public class BrowseModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public BrowseModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public IList<LoanRequest> Loans { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            // FIXED: Check if user is Lender
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "Lender")
            {
                return RedirectToPage("/Account/Login");
            }

            // Get all approved loans
            var allLoans = await _context.LoanRequests
                .Include(l => l.Borrower)
                .ThenInclude(b => b.Profile)
                .Include(l => l.Fundings)
                .Where(l => l.Status == "Approved")
                .ToListAsync();

            // FIXED: Filter out fully funded loans
            Loans = allLoans
                .Where(l => l.Fundings.Sum(f => f.Amount) < l.AmountRequested)
                .ToList();

            return Page();
        }
    }
}
