using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using LendSecure.Models;
using LendSecure.Data;

namespace LendSecure.Pages.Wallet
{
    public class DepositModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DepositModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        [Required]
        [Range(100, 1000000, ErrorMessage = "Amount must be between 100 and 1,000,000")]
        public decimal Amount { get; set; }

        public decimal CurrentBalance { get; set; }
        public string SuccessMessage { get; set; }
        public string ErrorMessage { get; set; }
        public string UserRole { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            UserRole = HttpContext.Session.GetString("UserRole");

            // FIXED: Check authentication (allow Lender and Borrower, not Admin)
            if (string.IsNullOrEmpty(userIdStr) || UserRole == "Admin")
            {
                return RedirectToPage("/Account/Login");
            }

            var userId = Guid.Parse(userIdStr);
            var wallet = await _context.Wallets.FirstOrDefaultAsync(w => w.UserId == userId);
            if (wallet != null)
            {
                CurrentBalance = wallet.Balance;
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            UserRole = HttpContext.Session.GetString("UserRole");

            // FIXED: Check authentication
            if (string.IsNullOrEmpty(userIdStr) || UserRole == "Admin")
            {
                return RedirectToPage("/Account/Login");
            }

            if (!ModelState.IsValid)
            {
                var userId = Guid.Parse(userIdStr);
                var walletCheck = await _context.Wallets.FirstOrDefaultAsync(w => w.UserId == userId);
                if (walletCheck != null)
                {
                    CurrentBalance = walletCheck.Balance;
                }
                return Page();
            }

            var currentUserId = Guid.Parse(userIdStr);
            var wallet = await _context.Wallets.FirstOrDefaultAsync(w => w.UserId == currentUserId);

            if (wallet == null)
            {
                // Create wallet if it doesn't exist
                wallet = new LendSecure.Models.Wallet
                {
                    WalletId = Guid.NewGuid(),
                    UserId = currentUserId,
                    Balance = 0,
                    Currency = "RWF",
                    UpdatedAt = DateTime.UtcNow
                };
                _context.Wallets.Add(wallet);
            }

            // Update Balance
            wallet.Balance += Amount;
            wallet.UpdatedAt = DateTime.UtcNow;

            // Create Transaction Record
            var txn = new WalletTransaction
            {
                TxnId = Guid.NewGuid(),
                WalletId = wallet.WalletId,
                TxnType = "Deposit",
                Amount = Amount,
                Currency = "RWF",
                CreatedAt = DateTime.UtcNow
            };
            _context.WalletTransactions.Add(txn);

            // Audit Log
            var auditLog = new AuditLog
            {
                LogId = Guid.NewGuid(),
                UserId = currentUserId,
                Action = "Wallet Deposit",
                Details = $"Deposited {Amount:N0} RWF (Demo)",
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                UserAgent = HttpContext.Request.Headers["User-Agent"].ToString(),
                CreatedAt = DateTime.UtcNow
            };
            _context.AuditLogs.Add(auditLog);

            await _context.SaveChangesAsync();

            SuccessMessage = $"Successfully deposited {Amount:N0} RWF to your wallet!";
            CurrentBalance = wallet.Balance;

            // Clear form
            ModelState.Clear();
            Amount = 0;

            return Page();
        }
    }
}
