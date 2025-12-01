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

namespace LendSecure.Pages.Wallet
{
    public class TransactionsModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public TransactionsModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public IList<WalletTransaction> Transactions { get; set; }
        public string UserRole { get; set; }
        public decimal CurrentBalance { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            UserRole = HttpContext.Session.GetString("UserRole");

            // FIXED: Check authentication (allow Lender and Borrower)
            if (string.IsNullOrEmpty(userIdStr) || UserRole == "Admin")
            {
                return RedirectToPage("/Account/Login");
            }

            var userId = Guid.Parse(userIdStr);
            var wallet = await _context.Wallets
                .FirstOrDefaultAsync(w => w.UserId == userId);

            if (wallet == null)
            {
                Transactions = new List<WalletTransaction>();
                CurrentBalance = 0;
                return Page();
            }

            CurrentBalance = wallet.Balance;

            Transactions = await _context.WalletTransactions
                .Include(t => t.RelatedLoan) // Include loan details
                .Where(t => t.WalletId == wallet.WalletId)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            return Page();
        }
    }
}
