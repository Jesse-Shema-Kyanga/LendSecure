using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using LendSecure.Models;
using LendSecure.Data;

namespace LendSecure.Pages.Loan
{
    public class MyFundingsModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public MyFundingsModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public IList<LoanFunding> Fundings { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr))
            {
                return RedirectToPage("/Account/Login");
            }

            var userId = Guid.Parse(userIdStr);

            Fundings = await _context.LoanFundings
                .Include(f => f.Loan)
                .ThenInclude(l => l.Borrower)
                .ThenInclude(b => b.Profile)
                .Where(f => f.LenderId == userId)
                .OrderByDescending(f => f.FundedAt)
                .ToListAsync();

            return Page();
        }
    }
}
