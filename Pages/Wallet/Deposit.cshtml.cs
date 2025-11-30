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

        public async Task<IActionResult> OnGetAsync()
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr))
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
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr))
            {
                return RedirectToPage("/Account/Login");
            }

            var userId = Guid.Parse(userIdStr);
            var wallet = await _context.Wallets.FirstOrDefaultAsync(w => w.UserId == userId);

            if (wallet == null)
            {
                // Create wallet if it doesn't exist (defensive coding)
                wallet = new LendSecure.Models.Wallet
                {
                    WalletId = Guid.NewGuid(),
                    UserId = userId,
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
                UserId = userId,
                Action = "Wallet Deposit",
                Details = $"Deposited {Amount} RWF",
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                UserAgent = HttpContext.Request.Headers["User-Agent"].ToString(),
                CreatedAt = DateTime.UtcNow
            };
            _context.AuditLogs.Add(auditLog);

            await _context.SaveChangesAsync();

            SuccessMessage = $"Successfully deposited {Amount:N0} RWF!";
            CurrentBalance = wallet.Balance;
            Amount = 0; // Reset form

            return Page();
        }
    }
}
