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

        public async Task OnGetAsync()
        {
            Loans = await _context.LoanRequests
                .Include(l => l.Borrower)
                .ThenInclude(b => b.Profile)
                .Include(l => l.Fundings)
                .Where(l => l.Status == "Approved")
                .ToListAsync();
        }
    }
}
